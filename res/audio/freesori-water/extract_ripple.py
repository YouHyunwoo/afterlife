"""보관 원본에서 짧은 물 호버음을 재현한다 (Python + NumPy)."""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
source = HERE / 'tfile.wav'
with wave.open(str(source), 'rb') as wav:
    rate, channels, width = wav.getframerate(), wav.getnchannels(), wav.getsampwidth()
    assert width == 2, '16-bit PCM 원본 필요'
    samples = np.frombuffer(wav.readframes(wav.getnframes()), dtype='<i2').reshape(-1, channels) / 32768.0

# 첫 100ms의 강한 어택을 제거하고 남은 물소리만 사용한다.
start, end = 2.30, 2.60
clip = samples[round(start * rate):round(end * rate)].copy()
clip -= clip.mean(axis=0)
fade_in, fade_out = round(.03 * rate), round(.14 * rate)
clip[:fade_in] *= np.linspace(0, 1, fade_in)[:, None]
clip[-fade_out:] *= np.linspace(1, 0, fade_out)[:, None]
# 기존 편집본과 같은 게인을 유지해 잘라낸 뒤 잔여 피크가 커지지 않게 한다.
reference = samples[round(2.20 * rate):round(2.60 * rate)].copy()
reference -= reference.mean(axis=0)
reference[:round(.005 * rate)] *= np.linspace(0, 1, round(.005 * rate))[:, None]
reference[-fade_out:] *= np.linspace(1, 0, fade_out)[:, None]
gain = 10 ** (-6 / 20) / np.max(np.abs(reference))
clip *= gain
pcm = np.rint(clip * 32767).astype('<i2')
output = HERE / 'freesori_water_ripple.wav'
with wave.open(str(output), 'wb') as wav:
    wav.setparams((channels, 2, rate, 0, 'NONE', 'not compressed'))
    wav.writeframes(pcm.tobytes())
target = ROOT / 'impls/unity/Afterlife/Assets/Modules/WorldAudio/Resources/WorldAudio' / output.name
target.write_bytes(output.read_bytes())
hop = round(rate * .01)
report = {
    'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
    'source_seconds': len(samples) / rate,
    'sample_rate': rate, 'channels': channels,
    'selected_seconds': [start, end], 'duration_seconds': len(clip) / rate,
    'fade_in_seconds': .03, 'fade_out_seconds': .14,
    'gain': float(gain), 'peak_dbfs': float(20 * np.log10(np.max(np.abs(clip)))),
    'rms_dbfs': float(20 * np.log10(np.sqrt(np.mean(clip ** 2)))),
    'selection_basis': '사용자 피드백에 따라 첫 100ms 어택 제거, 30ms 페이드 인. 기존 게인 유지. Unity 재생 검증 미실시.',
    'rms_10ms': [float(np.sqrt(np.mean(samples[i:i+hop] ** 2))) for i in range(0, len(samples), hop)],
    'output_sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
}
(HERE / 'analysis.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
assert pcm[0].max() == 0 and pcm[-1].max() == 0
assert np.max(np.abs(pcm.astype(np.int32))) < 32767
assert target.read_bytes() == output.read_bytes()
print(f'{output.name}: {len(clip)/rate:.2f}s, peak {report["peak_dbfs"]:.2f} dBFS, import copy verified')

# 강한 어택 뒤의 서로 다른 물결 구간. 단일 클립은 기존 참조 호환용으로 유지한다.
variant_dir = HERE / 'variants'
import_dir = target.parent / 'WaterRipples'
variant_dir.mkdir(exist_ok=True)
import_dir.mkdir(exist_ok=True)
variants = []
for index, (begin, finish) in enumerate([(1.12, 1.60), (2.30, 2.68), (2.84, 3.30), (4.64, 5.00), (6.86, 7.38)], 1):
    segment = samples[round(begin * rate):round(finish * rate)].copy()
    segment -= segment.mean(axis=0)
    # 잔여 구간의 좁고 큰 피크를 부드럽게 눌러 타격감이 다시 강조되지 않게 한다.
    segment = .07 * np.tanh(segment / .07)
    attack, release = round(.045 * rate), round(.18 * rate)
    segment[:attack] *= np.sin(np.linspace(0, np.pi / 2, attack))[:, None] ** 2
    segment[-release:] *= np.cos(np.linspace(0, np.pi / 2, release))[:, None] ** 2
    # 구간마다 체감 레벨을 맞추되 작은 물소리를 과하게 증폭하지 않는다.
    level = min(1.0, 10 ** (-32 / 20) / np.sqrt(np.mean(segment ** 2)),
                10 ** (-16 / 20) / np.max(np.abs(segment)))
    segment *= level
    data = np.rint(segment * 32767).astype('<i2')
    data[0] = data[-1] = 0
    path = variant_dir / f'freesori_water_ripple_{index:02}.wav'
    with wave.open(str(path), 'wb') as wav:
        wav.setparams((channels, 2, rate, 0, 'NONE', 'not compressed'))
        wav.writeframes(data.tobytes())
    imported = import_dir / path.name
    imported.write_bytes(path.read_bytes())
    assert imported.read_bytes() == path.read_bytes()
    assert np.max(np.abs(data.astype(np.int32))) < 32767
    variants.append({'file': path.name, 'source_seconds': [begin, finish],
                     'duration_seconds': len(segment) / rate, 'gain': float(level),
                     'peak_dbfs': float(20 * np.log10(np.max(np.abs(segment)))),
                     'rms_dbfs': float(20 * np.log10(np.sqrt(np.mean(segment ** 2)))),
                     'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
(HERE / 'variants-analysis.json').write_text(json.dumps({
    'source_sha256': report['source_sha256'], 'fade_in_seconds': .045, 'fade_out_seconds': .18,
    'transient_softening': '0.07 * tanh(sample / 0.07)',
    'selection_basis': '강한 어택 뒤 구간, 파형 기준 선정. Unity 청취 확인 필요.',
    'variants': variants}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(f'{len(variants)} variants exported and import copies verified')

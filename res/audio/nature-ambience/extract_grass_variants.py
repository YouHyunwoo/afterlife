"""기존 실녹음 편집본에서 부드러운 풀 스침 4개를 발췌한다 (numpy)."""
from pathlib import Path
import hashlib
import json
import shutil
import wave
import numpy as np

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SOURCE = HERE / 'grass.wav'
OUTPUT = HERE / 'grass-variants'
TARGET = ROOT / 'impls/unity/Afterlife/Assets/Devlog/Youtube/Modules/AmbientAudio/Resources/DevlogAmbience/GrassRustles'


def write_wav(path, data, rate):
    with wave.open(str(path), 'wb') as wav:
        wav.setparams((2, 2, rate, 0, 'NONE', 'not compressed'))
        wav.writeframes(np.rint(data * 32767).astype('<i2').tobytes())


def main():
    with wave.open(str(SOURCE), 'rb') as wav:
        assert wav.getsampwidth() == 2 and wav.getnchannels() == 2
        rate = wav.getframerate()
        samples = np.frombuffer(wav.readframes(wav.getnframes()), dtype='<i2').reshape(-1, 2) / 32768.
    OUTPUT.mkdir(exist_ok=True)
    TARGET.mkdir(parents=True, exist_ok=True)
    records, preview = [], []
    # 기존 WAV의 0초는 원 MP3의 1.3초다. 교차 연결 이전 구간만 사용한다.
    # 큰 피크 뒤의 퍼지는 구간을 골라 강한 접촉 어택을 피한다.
    for i, (start, end) in enumerate([(.10, .65), (1.55, 2.15), (4.15, 4.85), (6.45, 7.15)], 1):
        clip = samples[round(start * rate):round(end * rate)].copy()
        clip -= clip.mean(axis=0)
        # 남은 좁은 피크를 부드럽게 눌러 바스락 어택보다 여운을 강조한다.
        threshold = 3 * np.sqrt(np.mean(clip ** 2))
        clip = threshold * np.tanh(clip / threshold)
        clip[:round(.035 * rate)] *= np.linspace(0, 1, round(.035 * rate))[:, None]
        clip[-round(.14 * rate):] *= np.linspace(1, 0, round(.14 * rate))[:, None]
        gain = min(10 ** (-25 / 20) / np.sqrt(np.mean(clip ** 2)),
                   10 ** (-13 / 20) / np.max(np.abs(clip)))
        clip *= gain
        path = OUTPUT / f'grass_rustle_{i:02}.wav'
        write_wav(path, clip, rate)
        shutil.copyfile(path, TARGET / path.name)
        preview.extend([clip, np.zeros((round(.65 * rate), 2))])
        records.append(dict(file=path.name, source_seconds=[start, end],
            original_mp3_seconds=[round(start + 1.3, 2), round(end + 1.3, 2)],
            duration_seconds=len(clip) / rate, gain=float(gain),
            rms_dbfs=float(20 * np.log10(np.sqrt(np.mean(clip ** 2)))),
            peak_dbfs=float(20 * np.log10(np.max(np.abs(clip)))),
            sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    write_wav(HERE / 'grass_variants_preview.wav', np.concatenate(preview), rate)
    report = dict(source='grass.wav', source_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        sample_rate=rate, channels=2, fade_in_seconds=.035, fade_out_seconds=.14,
        transient_softening='tanh at 3x segment RMS', listening_verified=False, clips=records)
    (HERE / 'grass-variants-analysis.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(records, indent=2))


if __name__ == '__main__':
    main()

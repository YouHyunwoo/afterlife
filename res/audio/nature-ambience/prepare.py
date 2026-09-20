"""CC0 실녹음 HQ MP3를 환경음으로 편집한다. numpy/scipy/soundfile 필요."""
from pathlib import Path
import hashlib
import json
import shutil
import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfiltfilt, resample_poly
from math import gcd

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
TARGET = ROOT / 'impls/unity/Afterlife/Assets/Devlog/Modules/AmbientAudio/Resources/DevlogAmbience'
RATE = 44100


def prepare(kind, source, start, end, highpass, rms_db, crossfade):
    samples, rate = sf.read(HERE / source, always_2d=True)
    samples = samples[round(start * rate):round(end * rate)]
    divisor = gcd(rate, RATE)
    samples = resample_poly(samples, RATE // divisor, rate // divisor)
    samples -= samples.mean(axis=0)
    samples = sosfiltfilt(butter(2, highpass, 'highpass', fs=RATE, output='sos'), samples, axis=0)
    if kind == 'grass':
        # 풀 녹음의 짧고 날카로운 피크 때문에 배경 전체가 지나치게 작아지지 않게 완화한다.
        threshold = 4 * np.sqrt(np.mean(samples ** 2))
        samples = threshold * np.tanh(samples / threshold)
    if crossfade:
        # 끝과 시작을 겹쳐 붙여 loop 경계의 파형/레벨 불연속을 줄인다.
        count = round(crossfade * RATE)
        blend = np.linspace(0, 1, count)[:, None]
        seam = samples[-count:] * (1 - blend) + samples[:count] * blend
        samples = np.concatenate([samples[count:-count], seam])
    else:
        for length, beginning in [(.08, True), (.25, False)]:
            count = round(length * RATE)
            if beginning:
                samples[:count] *= np.linspace(0, 1, count)[:, None]
            else:
                samples[-count:] *= np.linspace(1, 0, count)[:, None]
    gain = min(10 ** (rms_db / 20) / np.sqrt(np.mean(samples ** 2)),
               10 ** (-10 / 20) / np.max(np.abs(samples)))
    samples *= gain
    output = HERE / (kind + '.wav')
    sf.write(output, samples, RATE, subtype='PCM_16')
    TARGET.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(output, TARGET / output.name)
    return dict(file=output.name, source=source, selection_seconds=[start, end],
                duration_seconds=len(samples) / RATE, highpass_hz=highpass,
                crossfade_seconds=crossfade, gain=float(gain),
                transient_softening='tanh at 4x RMS' if kind == 'grass' else 'none',
                rms_dbfs=float(20 * np.log10(np.sqrt(np.mean(samples ** 2)))),
                peak_dbfs=float(20 * np.log10(np.max(np.abs(samples)))),
                sha256=hashlib.sha256(output.read_bytes()).hexdigest())


if __name__ == '__main__':
    results = [prepare('wind', 'wind_181250_hq.mp3', 12, 52, 90, -22, 2),
               prepare('grass', 'grass_525268_hq.mp3', .3, 10, 240, -26, 1),
               prepare('bird', 'bird_182507_hq.mp3', 0, 4.727, 600, -25, 0)]
    (HERE / 'analysis.json').write_text(json.dumps(dict(sample_rate=RATE, channels=2,
        listening_verified=False, outputs=results), indent=2) + '\n', encoding='utf-8')
    print(json.dumps(results, indent=2))

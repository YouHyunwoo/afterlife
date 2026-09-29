"""기존 로컬 Supertonic 모델을 편집기 전용 자산 폴더로 한 번 복사한다."""
import argparse
import hashlib
import json
import shutil
import tempfile
from pathlib import Path
from runtime_paths import ROOT, DATA_DIR

SOURCE = ROOT / '.tooling/day01-tts/assets'
DESTINATION = DATA_DIR / 'tts_assets'
REQUIRED = ('LICENSE', 'onnx/tts.json', 'onnx/unicode_indexer.json',
            'onnx/duration_predictor.onnx', 'onnx/text_encoder.onnx',
            'onnx/vector_estimator.onnx', 'onnx/vocoder.onnx',
            *(f'voice_styles/M{i}.json' for i in range(1, 6)))


def check(folder):
    missing=[name for name in REQUIRED if not (folder/name).is_file()]
    if missing:raise FileNotFoundError('Supertonic 모델 파일이 없습니다: '+', '.join(missing))


def verify_copy(folder, manifest):
    if not manifest.is_file():return
    items=json.loads(manifest.read_text(encoding='utf-8')).get('files',[])
    for item in items:
        path=folder/item['path']
        if not path.is_file() or path.stat().st_size!=item['bytes']:
            raise ValueError('모델 파일 크기가 다릅니다: '+item['path'])
        digest=hashlib.sha256()
        with path.open('rb') as stream:
            for chunk in iter(lambda:stream.read(1024*1024),b''):digest.update(chunk)
        if digest.hexdigest()!=item['sha256']:
            raise ValueError('모델 파일 해시가 다릅니다: '+item['path'])


def prepare(source=SOURCE,destination=DESTINATION):
    source=Path(source).resolve();destination=Path(destination).resolve()
    if destination.exists():
        check(destination)
        return destination
    check(source)
    destination.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='tts-prepare-',dir=destination.parent) as work:
        staging=Path(work)/'assets'
        shutil.copytree(source,staging)
        verify_copy(staging, ROOT/'devlog/youtube/day-01/source/narration/provenance.json')
        staging.rename(destination)
    return destination


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--source',type=Path,default=SOURCE)
    parser.add_argument('--destination',type=Path,default=DESTINATION)
    args=parser.parse_args()
    print(prepare(args.source,args.destination))

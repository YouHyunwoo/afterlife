"""res 파일 목록/해시 갱신. Unity 에셋/메타데이터는 수정하지 않는다."""
from pathlib import Path
import hashlib, json
ROOT=Path(__file__).resolve().parents[2]
def main():
    files=[]
    for p in sorted((ROOT/'res').rglob('*')):
        if not p.is_file() or p == ROOT/'res/manifest.json':continue
        if 'graphify-out' in p.parts and ('cache' in p.parts or p.name.startswith('.')):continue
        if any(part in {'__pycache__', '.tooling', '.venv', 'venv'} for part in p.parts):continue
        if p.suffix.lower() in {'.pyc', '.pyo', '.blend1', '.blend2'}:continue
        if p.name.lower() in {'desktop.ini', 'thumbs.db', '.ds_store'}:continue
        rel=p.relative_to(ROOT).as_posix()
        group='기존 프로젝트 리소스; 공개 이용조건은 출처 기록 확인'
        if '/devlog-day01-narration/' in rel or '/day-01/narration/' in rel:
            group='프로젝트 대본 / Supertonic 3 M1 로컬 AI 나레이션; OpenRAIL-M 모델·MIT 추론 코드, docs/audio-credits.md'
        elif '/gongu-tree-fall/' in rel:group='한국저작권위원회 / 공유마당 13220749 / CC BY / docs/audio-credits.md'
        elif '/freesori-water/' in rel:group='프리소리 tistory.com/17 / 사용자 무료 이용 확인에 따라 적용 / 라이선스 원문 미확인 / docs/audio-credits.md'
        elif '/vegetation/' in rel:group='사용자 제공 PNG; 외부 출처/라이선스 미확인'
        elif '/stone-analysis/' in rel:group='iwanPlays / Freesound 567250, 567251; 분석 폴더 출처 기록 확인'
        elif '/tree-fall-analysis/' in rel:group='ecfike / Freesound 139952; 이전 후보, 현재 미사용'
        elif '/concepts/abandoned-altar-v2/' in rel:group='프로젝트용 내장 ImageGen 생성 시안 / prompt.txt'
        elif '/previews/abandoned-altar-v2/' in rel or rel.endswith('/AbandonedAltarV2.fbx'):group='프로젝트 자체 제작 Blender 제단 / tools/art/build_abandoned_altar_v2.py'
        files.append({'path':rel,'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'provenance':group})
    (ROOT/'res/manifest.json').write_text(json.dumps({'files':files},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(f'Resource manifest: {len(files)} files')
if __name__=='__main__':main()

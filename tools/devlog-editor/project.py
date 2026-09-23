"""영상 편집 프로젝트 모델. 모든 시간은 초, 파일 경로는 저장 JSON 기준 상대 경로."""
import copy
import json
import math
import os
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT = ROOT / 'devlog/youtube/day-01/edit-project/day01.devfilm.json'

def resolve(project_file, value):
    p = Path(value)
    return p.resolve() if p.is_absolute() else (Path(project_file).parent / p).resolve()

def relative(project_file, path):
    return os.path.relpath(Path(path).resolve(), Path(project_file).parent).replace('\\', '/')

def timed_shots(project):
    start = 0.0
    for shot in project['shots']:
        start = shot.get('timelineStart', start)
        yield start, shot
        start += shot['duration']

def duration(project):
    if project.get('schemaVersion') == 2:
        from timeline import duration as track_duration
        return track_duration(project)
    return max(start+s['duration'] for start,s in timed_shots(project))

def validate(project, project_file, files=False):
    if project.get('schemaVersion') == 2:
        from timeline import validate as validate_tracks
        return validate_tracks(project, project_file, files)
    if project.get('schemaVersion') != 1:
        raise ValueError('지원하지 않는 프로젝트 버전입니다.')
    shots = project.get('shots', [])
    if not shots or len(shots) > 200:
        raise ValueError('샷은 1~200개여야 합니다.')
    def number(value, name, low=0, high=86400):
        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or not low <= value <= high:
            raise ValueError(f'{name}: {low}~{high} 범위의 숫자가 필요합니다.')
    number(project['video']['offset'], '영상 시작 오프셋')
    number(project['video']['volume'], '녹화본 볼륨', 0, 1)
    number(project['narrationVolume'], '나레이션 볼륨', 0, 2)
    number(project['music']['volume'], '음악 볼륨', 0, 1)
    number(project['music']['creditsMultiplier'], '크레딧 음악 배율', 0, 4)
    paths = [project['video']['path'], project['music']['path']]
    ids = set()
    for shot in shots:
        if shot['id'] in ids:
            raise ValueError('샷 ID가 중복됩니다.')
        ids.add(shot['id'])
        number(shot['duration'], '샷 길이', .04, 3600)
        number(shot['sourceIn'], '원본 샷 시작')
        if 'timelineStart' in shot:
            number(shot['timelineStart'], '타임라인 시작')
        for cue in shot['cues']:
            number(cue['start'], '문장 시작')
            number(cue['duration'], '자막 길이', .01)
            number(cue.get('volume', 1), '문장 볼륨', 0, 2)
            if cue['start'] + cue['duration'] > shot['duration'] + .02:
                raise ValueError(f"{shot['title']}: 자막이 샷 끝을 넘습니다.")
            paths.append(cue['path'])
    end = 0
    for start, shot in sorted(timed_shots(project), key=lambda pair: pair[0]):
        if start < end - .000001:
            raise ValueError('영상 샷이 겹칩니다. 시작 시간이나 길이를 조절하세요.')
        end = start + shot['duration']
    if duration(project) > 3600:
        raise ValueError('현재 편집기는 1시간 이하 프로젝트를 지원합니다.')
    if project['export']['height'] not in (720, 1080) or project['export']['fps'] not in (24, 30, 60):
        raise ValueError('출력 해상도 또는 프레임 설정이 올바르지 않습니다.')
    if files:
        for path in paths:
            if not resolve(project_file, path).is_file():
                raise ValueError(f'파일을 찾을 수 없습니다: {path}')

def load(path):
    p = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    validate(p, path)
    return p

def save(path, project):
    validate(project, path)
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + '.tmp')
    temp.write_text(json.dumps(project, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    temp.replace(path)

def rebase(project, old, new):
    p = copy.deepcopy(project)
    for media in (p['video'], p['music']):
        if media.get('path'):
            media['path'] = relative(new, resolve(old, media['path']))
    if p.get('schemaVersion') == 2:
        from timeline import clips
        for asset in p.get('assets',[]):
            asset['path'] = relative(new, resolve(old, asset['path']))
        for track, clip in clips(p, enabled=False):
            if track['kind'] != 'subtitle':
                clip['path'] = relative(new, resolve(old, clip['path']))
        return p
    for shot in p['shots']:
        for cue in shot['cues']:
            cue['path'] = relative(new, resolve(old, cue['path']))
    return p


def portable_project(project, project_file):
    """브라우저가 저장 위치를 숨겨도 다시 열 수 있도록 매체 경로를 절대 경로로 기록한다."""
    p = copy.deepcopy(project)
    for media in (p['video'], p['music']):
        if media.get('path'):
            media['path'] = str(resolve(project_file, media['path']))
    if p.get('schemaVersion') == 2:
        from timeline import clips
        for asset in p.get('assets', []):
            if asset.get('path'):
                asset['path'] = str(resolve(project_file, asset['path']))
        for track, clip in clips(p, enabled=False):
            if track['kind'] != 'subtitle' and clip.get('path'):
                clip['path'] = str(resolve(project_file, clip['path']))
    else:
        for shot in p['shots']:
            for cue in shot['cues']:
                if cue.get('path'):
                    cue['path'] = str(resolve(project_file, cue['path']))
    return p


def has_relative_media(project):
    paths = [project['video'].get('path'), project['music'].get('path')]
    if project.get('schemaVersion') == 2:
        from timeline import clips
        paths.extend(asset.get('path') for asset in project.get('assets', []))
        paths.extend(clip.get('path') for track, clip in clips(project, enabled=False)
                     if track['kind'] != 'subtitle')
    else:
        paths.extend(cue.get('path') for shot in project['shots'] for cue in shot['cues'])
    return any(path and not Path(path).is_absolute() for path in paths)


def blank_project(name):
    name = name.strip()
    if not name or len(name) > 120 or re.search(r'[<>:"/\\|?*\x00-\x1f]', name) or name.endswith(('.', ' ')):
        raise ValueError('프로젝트 이름은 1~120자로 입력하고 파일 이름에 사용할 수 없는 문자는 제외해주세요.')
    if name.split('.')[0].upper() in {'CON', 'PRN', 'AUX', 'NUL', *(f'COM{i}' for i in range(1, 10)), *(f'LPT{i}' for i in range(1, 10))}:
        raise ValueError('Windows 예약 이름은 프로젝트 이름으로 사용할 수 없습니다.')
    return dict(schemaVersion=2, title=name, video=dict(path='', offset=0, volume=0, padEnd=False),
        narrationVolume=1, music=dict(path='', volume=.25, creditsMultiplier=1, fadeIn=0, fadeOut=0, creditsRamp=0),
        export=dict(height=1080, fps=30, subtitles=False), tracks=[], assets=[], minimumDuration=1)


def create_project(folder, name, root=ROOT):
    project = blank_project(name)
    if not str(folder).strip():
        raise ValueError('프로젝트 저장 폴더를 선택해주세요.')
    folder = Path(folder).resolve()
    if not folder.is_dir():
        raise ValueError('프로젝트를 저장할 폴더를 찾을 수 없습니다.')
    if not folder.is_relative_to(root.resolve()):
        raise ValueError('프로젝트 폴더는 Afterlife 작업 폴더 안에서 선택해주세요.')
    path = folder / (project['title'] + '.devfilm.json')
    if path.exists():
        raise FileExistsError('같은 이름의 프로젝트 파일이 이미 있습니다. 다른 이름을 선택하거나 기존 파일을 열어주세요.')
    save(path, project)
    return path, project


def browse_projects(folder, root=ROOT):
    root = root.resolve()
    current = Path(folder).resolve() if folder else root
    if not current.is_relative_to(root) or not current.is_dir():
        raise ValueError('Afterlife 작업 폴더 안의 기존 폴더를 선택해주세요.')
    entries = sorted(current.iterdir(), key=lambda item: item.name.casefold())
    directories = []
    files = []
    for entry in entries:
        resolved = entry.resolve()
        if not resolved.is_relative_to(root):
            continue
        if entry.is_dir():
            directories.append(dict(name=entry.name, path=str(resolved)))
        elif entry.is_file() and entry.name.lower().endswith('.json'):
            files.append(dict(name=entry.name, path=str(resolved)))
    return dict(root=str(root), path=str(current), parent=str(current.parent) if current != root else None,
                directories=directories, files=files)

def from_day01(path=DEFAULT):
    import re
    manifest = json.loads((ROOT / 'devlog/youtube/day-01/media/audio/narration/manifest.json').read_text(encoding='utf-8'))
    film = (ROOT / 'impls/unity/Afterlife/Assets/Devlog/Youtube/Day01/Data/Day01Film.asset').read_text(encoding='utf-8')
    def scalar(name, fallback):
        found = re.search(r'^  ' + name + r': ([\d.]+)', film, re.M)
        return float(found[1]) if found else fallback
    shots = []
    for s in manifest['shots']:
        shots.append(dict(id=s['id'], title=s['title'], duration=s['duration'], sourceIn=s['start'], credits=s.get('credits', False),
            notes=s['visual'], cues=[dict(start=c['start'], duration=c['duration'], text=c['text'], volume=1,
                path=relative(path, ROOT / 'devlog/youtube/day-01/media/audio/narration' / c['file'])) for c in s['cues']]))
    p = dict(schemaVersion=1, title='Day01 · 세계의 모습과 손맛',
        video=dict(path=relative(path, ROOT / 'devlog/youtube/day-01/media/video/recordings/Devlog_Day01_004.mp4'), offset=5.033333, volume=0, padEnd=True),
        narrationVolume=scalar('NarrationVolume', .85),
        music=dict(path=relative(path, ROOT / 'devlog/youtube/day-01/media/audio/music/Childhood-Day01.wav'), volume=scalar('MusicVolume', .1),
            creditsMultiplier=scalar('CreditsMusicMultiplier', 1.5), fadeIn=2, fadeOut=3, creditsRamp=2),
        export=dict(height=1080, fps=30, subtitles=False), shots=shots,
        credits=[dict(role='나레이션', name='Supertonic 3', detail='AI 생성 음성'), dict(role='배경음악', name='Childhood — Scott Buckley', detail='CC BY 4.0 · www.scottbuckley.com.au')],
        notes='녹화본의 화면·크레딧을 그대로 사용합니다. 자막이 이미 녹화돼 있으면 자막 덧씌우기는 꺼두세요. 원본 오디오는 기본 음소거입니다.')
    save(path, p)
    return p

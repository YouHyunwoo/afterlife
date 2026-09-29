"""프로젝트별 영구 미디어 위치. 렌더 캐시는 이 위치와 분리한다."""
import re
import uuid
from pathlib import Path
from project import ROOT

BROWSER_PROJECTS = ROOT / 'devlog/youtube/editor-projects'
WRITE_ROOTS = {ROOT.resolve()}


def grant_folder(folder):
    """데스크톱 파일 선택창에서 사용자가 고른 위치만 추가한다."""
    WRITE_ROOTS.add(Path(folder).resolve())


def writable(path):
    path = Path(path).resolve()
    if not any(path.is_relative_to(root) for root in WRITE_ROOTS):
        raise ValueError('파일 선택창에서 프로젝트 또는 출력 폴더를 먼저 선택해주세요.')
    return path


def media_root(project_file):
    path = Path(project_file).resolve()
    name = path.name[:-13] if path.name.lower().endswith('.devfilm.json') else path.stem
    return path.parent / (name + '.media')


def import_folder(project_file, kind):
    if kind not in ('video', 'audio'):
        raise ValueError('지원하지 않는 미디어 종류입니다.')
    return media_root(project_file) / kind / 'imports'


def export_folder(project_file):
    return media_root(project_file) / 'video' / 'exports'


def export_path(project_file, stamp):
    return export_folder(project_file) / (media_root(project_file).name[:-6] + '-' + stamp + '.mp4')


def browser_project_path(project):
    # 파일명/제목 변경에도 동일한 보관 위치를 사용한다. 외부 경로는 받지 않는다.
    key = project.get('browserStorageId')
    if key is None:
        key = uuid.uuid4().hex
    if not isinstance(key, str) or not re.fullmatch(r'[0-9a-f]{32}', key):
        raise ValueError('브라우저 프로젝트 보관 ID가 올바르지 않습니다.')
    project['browserStorageId'] = key
    return BROWSER_PROJECTS / key / 'project.devfilm.json'

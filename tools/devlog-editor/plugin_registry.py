"""앱에 설치된 신뢰 가능한 플러그인. manifest API v1, 일반 프로젝트 결과만 반환."""
import copy
import importlib.util
import json
import re
from pathlib import Path
from runtime_paths import APP_DIR, DATA_DIR
from project import validate

PLUGIN_DIR=APP_DIR/'plugins'
SETTINGS=DATA_DIR/'plugins.json'


def manifests():
    result=[]
    settings=json.loads(SETTINGS.read_text(encoding='utf-8')) if SETTINGS.exists() else {}
    for path in sorted(PLUGIN_DIR.glob('*/manifest.json')):
        item=json.loads(path.read_text(encoding='utf-8'))
        if item.get('apiVersion')!=1 or item.get('id')!=path.parent.name:
            continue
        if not re.fullmatch(r'[a-z][a-z0-9-]*',item['id']):
            continue
        result.append({**item,'enabled':bool(settings.get(item['id'],True))})
    return result


def get_plugin(key):
    for item in manifests():
        if item['id']==key:return item
    raise ValueError('플러그인을 찾을 수 없습니다.')


def set_enabled(key, enabled):
    get_plugin(key)
    if not isinstance(enabled,bool):raise ValueError('활성 상태가 올바르지 않습니다.')
    settings=json.loads(SETTINGS.read_text(encoding='utf-8')) if SETTINGS.exists() else {}
    settings[key]=enabled
    SETTINGS.parent.mkdir(parents=True,exist_ok=True)
    temp=SETTINGS.with_suffix('.tmp');temp.write_text(json.dumps(settings),encoding='utf-8');temp.replace(SETTINGS)


def resource(key, name):
    get_plugin(key)
    folder=(PLUGIN_DIR/key).resolve();path=(folder/name).resolve()
    if not path.is_relative_to(folder) or path.suffix not in ('.js','.css') or not path.is_file():
        raise ValueError('플러그인 리소스를 찾을 수 없습니다.')
    return path


def invoke(key, action, project, project_file, options):
    manifest=get_plugin(key)
    if not manifest['enabled']:raise ValueError('플러그인이 꺼져 있습니다.')
    if action not in manifest['actions']:raise ValueError('지원하지 않는 플러그인 작업입니다.')
    validate(project,project_file)
    module_path=PLUGIN_DIR/key/'plugin.py'
    spec=importlib.util.spec_from_file_location('editor_plugin_'+key.replace('-','_'),module_path)
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    result=module.run(action,copy.deepcopy(project),Path(project_file),options)
    validate(result['project'],project_file,files=True)
    return result

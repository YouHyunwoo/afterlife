import json
from pathlib import Path

RESOURCES = Path(__file__).resolve().parents[2] / 'impls/unity/Afterlife/Assets/Resources'

# 1. en.json의 key 순서 추출
with open(RESOURCES / 'Localization/en.json', encoding='utf-8') as f:
    en_data = json.load(f)
en_order = [item['key'] for item in en_data['items']]

# 2. strings.json 파싱 및 정렬
with open(RESOURCES / 'strings.json', encoding='utf-8') as f:
    strings_data = json.load(f)

for lang in strings_data['languages']:
    # id->entry 매핑
    id_map = {entry['id']: entry for entry in lang['strings'] if 'id' in entry}
    # en.json 순서대로 정렬
    sorted_entries = [id_map[id] for id in en_order if id in id_map]
    # en.json에 없는 id는 뒤에 추가
    extra_entries = [entry for entry in lang['strings'] if entry.get('id') not in en_order]
    lang['strings'] = sorted_entries + extra_entries

# 3. 저장
with open(RESOURCES / 'strings-generated.json', 'w', encoding='utf-8') as f:
    json.dump(strings_data, f, ensure_ascii=False, indent=4)

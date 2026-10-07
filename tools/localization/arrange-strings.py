"""기준 언어의 key 순서로 문자열을 정렬한다. 입력은 보존한다."""
import argparse
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
RESOURCES = ROOT / "impls/unity/Afterlife/Assets/Resources"

def arrange_strings(order, data):
    keys = [item["key"] for item in order["items"]]
    known = set(keys)
    for language in data["languages"]:
        entries = {entry["id"]: entry for entry in language["strings"] if "id" in entry}
        language["strings"] = [entries[key] for key in keys if key in entries] + [entry for entry in language["strings"] if entry.get("id") not in known]
    return data

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--order", type=Path, required=True, help="items[].key를 가진 기준 언어 JSON")
    parser.add_argument("--input", type=Path, default=RESOURCES / "strings.json")
    parser.add_argument("--output", type=Path, default=RESOURCES / "strings-generated.json")
    args = parser.parse_args()
    for source in [args.order, args.input]:
        if not source.is_file(): parser.error("입력 파일 없음: " + str(source))
    if args.output.resolve() in {args.order.resolve(), args.input.resolve()}: parser.error("출력은 입력과 다른 파일이어야 합니다.")
    order = json.loads(args.order.read_text(encoding="utf-8-sig"))
    data = json.loads(args.input.read_text(encoding="utf-8-sig"))
    args.output.write_text(json.dumps(arrange_strings(order, data), ensure_ascii=False, indent=4) + chr(10), encoding="utf-8")
    print("문자열 출력:", args.output)

if __name__ == "__main__": main()

# 기존 문자열 정리

`Assets/Resources/strings.json`을 별도 기준 언어 JSON의 `items[].key` 순서로 정렬한다. 기준에 없는 항목은 기존 순서로 뒤에 유지하며 입력은 덮어쓰지 않는다.

```powershell
python tools/localization/arrange-strings.py --order <기준-언어.json> --input <문자열.json> --output <정렬결과.json>
```

기존 기본값 `Localization/en.json`은 저장소에 없으므로 `--order`를 명시한다. 파일 누락과 입력 덮어쓰기는 오류로 중단한다. 생성 결과를 실제 게임에 적용하는 절차는 별도다.

검증: 기준 순서, 기준 밖 항목 보존, 입력 파일 SHA-256 보존을 확인한다.

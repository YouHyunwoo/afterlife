using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;

public class StringEditor : EditorWindow
{
    private Afterlife.Data.StringData stringData = new();
    private string jsonFilePath = "";
    private int selectedLanguageIndex = 0;

    // ReorderableList 관련 필드
    private ReorderableList reorderableStringList;
    private List<string> cachedStringIds = new List<string>();
    private ReorderableList reorderableLanguageList;
    // 자동 저장 타이머 관련
    private double lastEditTime = -1;
    private bool hasPendingChanges = false;
    // 문자열 목록 스크롤 위치
    private Vector2 scrollPos = Vector2.zero;

    [MenuItem("Tools/Strings/String Editor")]
    public static void ShowWindow()
    {
        GetWindow<StringEditor>("String Editor");
    }

    private const string EditorPrefsKey_FilePath = "StringEditor_LastJsonFilePath";
    private const string EditorPrefsKey_LangIndex = "StringEditor_LastLangIndex";

    private void OnEnable()
    {
        // EditorPrefs에서 마지막 파일 경로/언어 인덱스 복원
        if (EditorPrefs.HasKey(EditorPrefsKey_FilePath))
        {
            jsonFilePath = EditorPrefs.GetString(EditorPrefsKey_FilePath, "");
            if (!string.IsNullOrEmpty(jsonFilePath) && File.Exists(jsonFilePath))
            {
                stringData = Afterlife.Model.StringJsonDataFileLoader.LoadByPath(jsonFilePath);
            }
        }
        if (EditorPrefs.HasKey(EditorPrefsKey_LangIndex))
        {
            selectedLanguageIndex = EditorPrefs.GetInt(EditorPrefsKey_LangIndex, 0);
        }
        SetupReorderableList();
        SetupLanguageReorderableList();
        EditorApplication.update += AutoSaveUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= AutoSaveUpdate;
        // 창이 닫힐 때 남은 변경사항 저장
        if (hasPendingChanges)
        {
            SaveJsonImmediate();
        }
        // EditorPrefs에 마지막 파일 경로/언어 인덱스 저장
        EditorPrefs.SetString(EditorPrefsKey_FilePath, jsonFilePath ?? "");
        EditorPrefs.SetInt(EditorPrefsKey_LangIndex, selectedLanguageIndex);
        reorderableLanguageList = null;
    }

    private void SetupLanguageReorderableList()
    {
        reorderableLanguageList = new ReorderableList(stringData.languages, typeof(Afterlife.Data.LanguageEntry), true, true, true, true);
        reorderableLanguageList.drawHeaderCallback = (rect) => {
            EditorGUI.LabelField(rect, "언어 목록");
        };
        reorderableLanguageList.drawElementCallback = (rect, index, isActive, isFocused) => {
            if (index < 0 || index >= stringData.languages.Count) return;
            var lang = stringData.languages[index];
            GUIStyle style = new GUIStyle(EditorStyles.label);
            if (index == selectedLanguageIndex)
            {
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(0.2f, 0.5f, 1f, 1f);
            }
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width, rect.height), lang.languageCode, style))
            {
                if (selectedLanguageIndex != index)
                {
                    selectedLanguageIndex = index;
                    SetupReorderableList();
                }
            }
        };
        reorderableLanguageList.onAddCallback = (list) => {
            LanguageCodeInputPopup.Show((code) => {
                if (!string.IsNullOrEmpty(code) && !stringData.languages.Exists(l => l.languageCode == code))
                {
                    stringData.languages.Add(new Afterlife.Data.LanguageEntry { languageCode = code });
                    selectedLanguageIndex = stringData.languages.Count - 1;
                    SetupReorderableList();
                    SetupLanguageReorderableList();
                    SaveJsonImmediate();
                }
            });
        };
        reorderableLanguageList.onRemoveCallback = (list) => {
            int idx = reorderableLanguageList.index;
            if (idx >= 0 && idx < stringData.languages.Count)
            {
                string langCode = stringData.languages[idx].languageCode;
                if (EditorUtility.DisplayDialog("언어 삭제", $"{langCode} 언어를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    stringData.languages.RemoveAt(idx);
                    if (selectedLanguageIndex >= stringData.languages.Count)
                        selectedLanguageIndex = Mathf.Max(0, stringData.languages.Count - 1);
                    SetupReorderableList();
                    SetupLanguageReorderableList();
                    SaveJsonImmediate();
                }
            }
        };
        reorderableLanguageList.onReorderCallback = (list) => {
            // 순서 변경 시 selectedLanguageIndex 동기화
            if (selectedLanguageIndex >= 0 && selectedLanguageIndex < stringData.languages.Count)
            {
                // 현재 선택된 언어의 코드
                string curCode = stringData.languages[selectedLanguageIndex].languageCode;
                for (int i = 0; i < stringData.languages.Count; i++)
                {
                    if (stringData.languages[i].languageCode == curCode)
                    {
                        selectedLanguageIndex = i;
                        break;
                    }
                }
            }
            SetupReorderableList();
            SaveJsonImmediate();
        };
    
    }

    private void AutoSaveUpdate()
    {
        if (hasPendingChanges && lastEditTime > 0 && (EditorApplication.timeSinceStartup - lastEditTime) > 1.0)
        {
            SaveJsonImmediate();
        }
    }

    private void SetupReorderableList()
    {
        // 모든 문자열 ID 목록 추출 (모든 언어에서 공통)
        HashSet<string> allStringIds = new HashSet<string>();
        foreach (var lang in stringData.languages)
            foreach (var entry in lang.strings)
                allStringIds.Add(entry.id);
        cachedStringIds = new List<string>(allStringIds);
        // 기존 순서 유지(첫 번째 언어 기준)
        if (stringData.languages.Count > 0)
        {
            List<string> ordered = new List<string>();
            foreach (var entry in stringData.languages[0].strings)
                if (allStringIds.Contains(entry.id))
                    ordered.Add(entry.id);
            foreach (var id in cachedStringIds)
                if (!ordered.Contains(id))
                    ordered.Add(id);
            cachedStringIds = ordered;
        }

        reorderableStringList = new ReorderableList(cachedStringIds, typeof(string), true, true, true, true);
        reorderableStringList.drawHeaderCallback = (rect) => {
            var selectedLanguage = stringData.languages.Count > 0 && selectedLanguageIndex < stringData.languages.Count ? stringData.languages[selectedLanguageIndex] : null;
            int count = cachedStringIds != null ? cachedStringIds.Count : 0;
            string langCode = selectedLanguage != null ? selectedLanguage.languageCode : "-";
            EditorGUI.LabelField(rect, $"문자열 목록 ({langCode}) - {count}개");
        };
        reorderableStringList.drawElementCallback = (rect, index, isActive, isFocused) => {
            if (index < 0 || index >= cachedStringIds.Count) return;
            string id = cachedStringIds[index];
            var selectedLanguage = stringData.languages.Count > 0 && selectedLanguageIndex < stringData.languages.Count ? stringData.languages[selectedLanguageIndex] : null;
            if (selectedLanguage == null) return;
            var entry = selectedLanguage.strings.Find(s => s.id == id);
            if (entry == null)
            {
                entry = new Afterlife.Data.StringEntry { id = id, text = "" };
                selectedLanguage.strings.Add(entry);
            }
            float idWidth = 150f;
            float textWidth = rect.width - idWidth;
            Rect idRect = new Rect(rect.x, rect.y + 2, idWidth, EditorGUIUtility.singleLineHeight);
            Rect textRect = new Rect(rect.x + idWidth, rect.y + 2, textWidth, EditorGUIUtility.singleLineHeight);
            // id 인라인 수정
            string newId = EditorGUI.TextField(idRect, id);
            if (newId != id && !string.IsNullOrEmpty(newId) && !cachedStringIds.Contains(newId))
            {
                // 모든 언어에서 id 동기화
                for (int l = 0; l < stringData.languages.Count; l++)
                {
                    var e = stringData.languages[l].strings.Find(s => s.id == id);
                    if (e != null) e.id = newId;
                }
                cachedStringIds[index] = newId;
                MarkDirty();
            }
            // text 인라인 수정 (현재 언어만)
            // \n → 실제 줄바꿈으로 변환하여 표시
            string displayText = entry.text?.Replace("\\n", "\n");
            string newText = EditorGUI.TextField(textRect, displayText);
            // 실제 줄바꿈 → \n으로 변환하여 저장
            string saveText = newText?.Replace("\r\n", "\n").Replace("\n", "\\n");
            if (saveText != entry.text)
            {
                entry.text = saveText;
                MarkDirty();
            }
        };
        // onAddCallback: 한 줄 추가 (id 자동 생성)
        reorderableStringList.onAddCallback = (list) => {
            string newId = "";
            cachedStringIds.Add(newId);
            foreach (var lang in stringData.languages)
            {
                lang.strings.Add(new Afterlife.Data.StringEntry {
                    id = newId,
                    text = ""
                });
            }
            reorderableStringList.list = cachedStringIds;
            reorderableStringList.index = cachedStringIds.Count - 1;
            MarkDirty();
        };
        reorderableStringList.onRemoveCallback = (list) => {
            int idx = reorderableStringList.index;
            if (idx >= 0 && idx < cachedStringIds.Count)
            {
                string id = cachedStringIds[idx];
                foreach (var lang in stringData.languages)
                    lang.strings.RemoveAll(s => s.id == id);
                cachedStringIds.RemoveAt(idx);
                reorderableStringList.list = cachedStringIds;
                MarkDirty();
            }
        };
        reorderableStringList.onReorderCallback = (list) => {
            // 모든 언어의 문자열 순서 동기화
            foreach (var lang in stringData.languages)
            {
                List<Afterlife.Data.StringEntry> newList = new List<Afterlife.Data.StringEntry>();
                foreach (string id in cachedStringIds)
                {
                    var entry = lang.strings.Find(s => s.id == id);
                    if (entry != null)
                        newList.Add(entry);
                }
                lang.strings = newList;
            }
            MarkDirty();
        };
    }

    private void OnGUI()
    {
        jsonFilePath = EditorGUILayout.TextField("JSON 파일 경로", jsonFilePath, GUILayout.ExpandWidth(true));
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("불러오기"))
        {
            if (!string.IsNullOrEmpty(jsonFilePath) && File.Exists(jsonFilePath))
            {
                stringData = Afterlife.Model.StringJsonDataFileLoader.LoadByPath(jsonFilePath);
                selectedLanguageIndex = 0;
            }
            else
            {
                stringData = new Afterlife.Data.StringData();
            }
        }
        if (GUILayout.Button("찾기"))
        {
            string path = EditorUtility.OpenFilePanel("JSON 파일 선택", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                jsonFilePath = path;
                stringData = Afterlife.Model.StringJsonDataFileLoader.LoadByPath(jsonFilePath);
                selectedLanguageIndex = 0;
            }
            else
            {
                stringData = new Afterlife.Data.StringData();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (string.IsNullOrEmpty(jsonFilePath))
        {
            EditorGUILayout.HelpBox("JSON 파일 경로를 입력하거나 찾으세요.", MessageType.Info);
            return;
        }

        // 언어 목록 ReorderableList 표시
        GUILayout.Space(10);
        if (reorderableLanguageList == null || reorderableLanguageList.list != stringData.languages)
            SetupLanguageReorderableList();
        reorderableLanguageList.DoLayoutList();
        if (stringData.languages.Count == 0)
        {
            EditorGUILayout.HelpBox("언어를 추가하세요.", MessageType.Warning);
            return;
        }

        // 모든 문자열 ID 목록 추출 (모든 언어에서 공통)
        HashSet<string> allStringIds = new HashSet<string>();
        foreach (var lang in stringData.languages)
            foreach (var entry in lang.strings)
                allStringIds.Add(entry.id);
        List<string> sortedStringIds = new List<string>(allStringIds);
        sortedStringIds.Sort();

        // 문자열 목록 (공통 ID, 해당 언어의 text만 수정)
        GUILayout.Space(10);

        // 스크롤뷰 적용: 문자열 목록이 많을 때 스크롤바가 나타나도록 함
        int listHeight = Mathf.Min(24 * (reorderableStringList.count + 1), 400); // 한 줄 24px, 최대 400px
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(listHeight));
        reorderableStringList.DoLayoutList();
        EditorGUILayout.EndScrollView();

        // 저장 버튼
        if (GUILayout.Button("저장"))
        {
            SaveJsonImmediate();
        }

        // 저장 상태 레이블 표시
        GUILayout.Space(8);
        GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
        statusStyle.alignment = TextAnchor.MiddleRight;
        statusStyle.fontStyle = FontStyle.Italic;
        string statusText = hasPendingChanges ? "저장 필요 (자동 저장 대기 중)" : "저장 완료";
        Color prevColor = GUI.color;
        GUI.color = hasPendingChanges ? new Color(1f,0.6f,0.2f,1f) : new Color(0.2f,0.7f,0.2f,1f);
        GUILayout.Label(statusText, statusStyle, GUILayout.ExpandWidth(true));
        GUI.color = prevColor;
    }

    // 자동 저장 타이머 리셋 및 변경 플래그
    private void MarkDirty()
    {
        lastEditTime = EditorApplication.timeSinceStartup;
        hasPendingChanges = true;
    }

    // 즉시 저장
    private void SaveJsonImmediate()
    {
        if (!string.IsNullOrEmpty(jsonFilePath))
        {
            Afterlife.Model.StringJsonDataFileSaver.SaveByPath(jsonFilePath, stringData);
            AssetDatabase.Refresh();
        }
        hasPendingChanges = false;
        lastEditTime = -1;
    }
}

using UnityEditor;
using UnityEngine;
using System;

public class LanguageCodeInputPopup : EditorWindow
{
    private string input = "";
    private static Action<string> onConfirm;

    public static void Show(Action<string> onConfirmCallback)
    {
        onConfirm = onConfirmCallback;
        var window = ScriptableObject.CreateInstance<LanguageCodeInputPopup>();
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 90);
        window.titleContent = new GUIContent("언어 코드 입력");
        window.ShowUtility();
    }

    private void OnGUI()
    {
        GUILayout.Label("추가할 언어 코드를 입력하세요 (예: ko, en, ja 등)");
        GUI.SetNextControlName("LangCodeField");
        input = EditorGUILayout.TextField(input);
        GUI.FocusControl("LangCodeField");
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("확인"))
        {
            onConfirm?.Invoke(input.Trim());
            Close();
        }
        if (GUILayout.Button("취소"))
        {
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }
}

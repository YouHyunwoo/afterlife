using System.IO;
using UnityEngine;

namespace Afterlife.Model
{
    public static class StringJsonDataFileLoader
    {
        public static Data.StringData LoadByPath(string path)
        {
            if (!File.Exists(path)) { Debug.LogError($"Strings JSON not found: {path}"); return new Data.StringData(); }

            string jsonText = File.ReadAllText(path);
            return JsonUtility.FromJson<Data.StringData>(jsonText);
        }

        public static Data.StringData LoadFromResources(string resourcePath)
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
            if (jsonAsset == null)
            {
                Debug.LogError($"Strings JSON not found in Resources: {resourcePath}");
                return new Data.StringData();
            }

            return JsonUtility.FromJson<Data.StringData>(jsonAsset.text);
        }
    }

    public static class StringJsonDataFileSaver
    {
        public static void SaveByPath(string path, Data.StringData data)
        {
            string jsonText = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, jsonText);
        }
    }
}
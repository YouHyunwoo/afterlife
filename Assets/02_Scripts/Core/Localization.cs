using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Core
{
    public enum Language
    {
        English,
        Korean,
    }

    [Serializable]
    public class LocalizationItem
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class LocalizationCollection
    {
        public LocalizationItem[] items;
    }

    public static class Localization
    {
        public static Language CurrentLanguage;
        public static string CurrentLocale => CurrentLanguage.ToString().ToLower();

        static readonly Dictionary<string, string> localizedText = new();

        public static event Action OnLanguageChangedEvent;

        public static void Load()
        {
            string locale = PlayerPrefs.GetString("Locale", "en");
            LoadByLocale(locale);
        }

        public static void LoadByLocale(string locale)
        {
            localizedText.Clear();

            var languageData = Resources.Load<TextAsset>($"Localization/{locale}");
            if (languageData == null)
            {
                Debug.LogError($"Localization JSON not found: {locale}");
                return;
            }

            var data = JsonUtility.FromJson<LocalizationCollection>(languageData.text);
            foreach (var item in data.items)
            {
                localizedText[item.key] = item.value;
            }

            CurrentLanguage = ParseLocale(locale);
            PlayerPrefs.SetString("Locale", locale);
            OnLanguageChangedEvent?.Invoke();

            Debug.Log($"로케일 '{locale}'이(가) 성공적으로 로드되었습니다.");
        }

        static Language ParseLocale(string locale)
        {
            return locale.ToLower() switch
            {
                "en" => Language.English,
                "ko" => Language.Korean,
                _ => throw new ArgumentException($"지원하지 않는 언어 코드: {locale}"),
            };
        }

        public static string Get(string key)
        {
            if (localizedText.TryGetValue(key, out var val))
            {
                return val;
            }

            return $"<Missing:{key}>";
        }

        public static void SetLanguage(Language lang)
        {
            string code = lang switch
            {
                Language.Korean  => "ko",
                Language.English => "en",
                _                => throw new ArgumentException($"지원하지 않는 언어: {lang}"),
            };
            LoadByLocale(code);
        }
    }
}
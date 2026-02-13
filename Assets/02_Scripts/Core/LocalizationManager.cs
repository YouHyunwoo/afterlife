using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Core
{
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

    public class LocalizationManager : ManagerBase
    {
        static LocalizationManager instance;

        public string[] Locales;

        string currentLocale;
        readonly Dictionary<string, string> localizedText = new();
        const string LocaleKey = "Locale";

        public event Action OnLanguageChangedEvent;

        public override void SetUp()
        {
            instance = this;

            string locale = PlayerPrefs.GetString(LocaleKey, Locales[0].ToLower());
            SetLocaleInternal(locale);
        }

        public override void TearDown()
        {
            OnLanguageChangedEvent = null;

            localizedText.Clear();
        }

        void SetLocaleInternal(string locale)
        {
            currentLocale = locale;

            localizedText.Clear();

            var stringData = Model.StringJsonDataFileLoader.LoadFromResources($"strings");
            foreach (var languageEntry in stringData.languages)
            {
                if (languageEntry.languageCode != locale) { continue; }

                foreach (var item in languageEntry.strings)
                {
                    localizedText[item.id] = item.text;
                }

                break;
            }

            PlayerPrefs.SetString(LocaleKey, locale);

            OnLanguageChangedEvent?.Invoke();
        }

        public void SetLocale(string locale)
        {
            SetLocaleInternal(locale);
        }

        public void RotateLocale()
        {
            int currentIndex = Array.IndexOf(Locales, currentLocale);
            int nextIndex = (currentIndex + 1) % Locales.Length;
            string nextLocale = Locales[nextIndex].ToLower();
            SetLocaleInternal(nextLocale);
        }

        public static string Get(string key)
        {
            var hasValue = instance.localizedText.TryGetValue(key, out var value);
            return hasValue ? value : $"<Missing:{key}>";
        }

        public string this[string key] => Get(key);
    }
}
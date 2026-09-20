using System;
using System.Collections.Generic;

namespace Afterlife.Data
{
    [Serializable]
    public class StringData
    {
        public List<LanguageEntry> languages = new();
    }

    [Serializable]
    public class LanguageEntry
    {
        public string languageCode;
        public List<StringEntry> strings = new();
    }

    [Serializable]
    public class StringEntry
    {
        public string id;
        public string text;
    }
}
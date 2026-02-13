using System.Collections.Generic;

namespace Afterlife.Data.YAML
{
    [System.Serializable]
    public class Scenario
    {
        public string name;
        public string description;
        public Scene[] scenes;
    }

    [System.Serializable]
    public class Scene
    {
        public string name;
        public string description;
        public List<Dictionary<string, string>> sequences;
    }
}
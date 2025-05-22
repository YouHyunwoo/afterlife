using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Afterlife.Model
{
    public class YamlParser
    {
        public static T Parse<T>(string yamlPath)
        {
            string path = Path.Combine(Application.streamingAssetsPath, yamlPath);
            string yamlData = File.ReadAllText(path);

            var deserializer = new DeserializerBuilder().Build();
            var result = deserializer.Deserialize<T>(yamlData);

            return result;
        }
    }
}
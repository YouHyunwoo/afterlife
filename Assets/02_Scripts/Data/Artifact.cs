using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "ArtifactData", menuName = "Afterlife/Data/Artifact")]
    public class Artifact : ScriptableObject
    {
        public string Id;
        public Sprite Icon;
        public UpgradeParameter[] Parameters;
    }
}

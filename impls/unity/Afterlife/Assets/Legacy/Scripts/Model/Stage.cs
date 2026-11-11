namespace Afterlife.Model
{
    [System.Serializable]
    public class Stage
    {
        public Data.Stage Data;
        public Map Map;

        public float ElapsedTime;
        public int Days;
        public bool IsDayTime;

        public int MaxEnvironmentObjectCount;
        public float EnvironmentObjectGenerationProbability;
    }
}
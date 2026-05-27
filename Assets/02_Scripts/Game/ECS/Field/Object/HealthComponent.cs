namespace Afterlife.Dev.Game
{
    public struct HealthComponent
    {
        public float Current;
        public float Max;

        public bool IsAlive => Current > 0f;
        public float Ratio => Current / Max;
    }
}
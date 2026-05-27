namespace Afterlife.Dev.Game
{
    public struct WanderComponent
    {
        public float Chance; // 배회 확률
        public float Interval; // 결정 주기
        public float DecisionTime; // 다음 판단 시간
        public float DisabledTime; // 배회 비활성화 시간
    }
}
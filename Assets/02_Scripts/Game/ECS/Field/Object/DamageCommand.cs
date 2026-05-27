namespace Afterlife.Dev.Game
{
    public struct DamageCommand
    {
        public float Amount;
        public object Attacker; 
        public bool IsPending; // 처리 대기 중인가?
    }
}
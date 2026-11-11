namespace Afterlife.Dev.Field
{
    public interface IDamageable
    {
        void TakeDamage(float damage, Object attacker);
        bool IsAlive { get; }
    }
}

namespace Afterlife.Dev.Field
{
    public interface IDamageable
    {
        void TakeDamage(float damage, ObjectVisible attacker);
        bool IsAlive { get; }
    }
}

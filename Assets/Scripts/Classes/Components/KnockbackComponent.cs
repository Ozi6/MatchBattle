using UnityEngine;

public class KnockbackComponent : IKnockback
{
    private readonly float force;
    private readonly float duration;

    public KnockbackComponent(float force, float duration)
    {
        this.force = force;
        this.duration = duration;
    }

    public void ApplyKnockback(Enemy enemy, Vector2 direction, float forceMultiplier, float duration)
    {
        enemy.ApplyKnockback(direction, force * forceMultiplier, this.duration);
    }
}
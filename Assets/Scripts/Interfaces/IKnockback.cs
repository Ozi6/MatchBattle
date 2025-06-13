using UnityEngine;

public interface IKnockback
{
    void ApplyKnockback(Enemy enemy, Vector2 direction, float force, float duration);
}

using UnityEngine;

public interface IExplosive
{
    void Explode(Vector3 position, float damage, float radius, IDebuffable debuffable, IKnockback knockback);
}

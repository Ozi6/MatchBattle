using UnityEngine;

public class ExplosionComponent : MonoBehaviour, IExplosive
{
    private float radius;
    private GameObject explosionEffectPrefab;
    private readonly string enemyTag = "Enemy";
    private Projectile projectile;

    public ExplosionComponent Initialize(float radius, GameObject explosionEffectPrefab, Projectile projectile)
    {
        this.radius = radius;
        this.explosionEffectPrefab = explosionEffectPrefab;
        this.projectile = projectile;
        return this;
    }

    public void Explode(Vector3 position, float damage, float radius, IDebuffable debuffable, IKnockback knockback)
    {
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, position, Quaternion.identity);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, this.radius);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag(enemyTag))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsDead())
                {
                    float distance = Vector2.Distance(position, enemy.transform.position);
                    float damageMultiplier = 1f - (distance / this.radius);
                    damageMultiplier = Mathf.Clamp01(damageMultiplier);

                    float areaDamage = damage * damageMultiplier;
                    enemy.TakeDamage(areaDamage);

                    debuffable?.ApplyDebuff(enemy);

                    if (knockback != null)
                    {
                        Vector2 knockbackDirection = (enemy.transform.position - position).normalized;
                        knockback.ApplyKnockback(enemy, knockbackDirection, damageMultiplier, 1);
                    }
                }
            }
        }
        if (projectile != null)
            projectile.DestroyProjectile();
    }
}
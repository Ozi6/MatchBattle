using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D projectileCollider;
    [SerializeField] private string enemyTag = "Enemy";

    private ProjectileData data;
    private IKnockback knockbackComponent;
    private IExplosive explosionComponent;
    private IArcTrajectory arcTrajectoryComponent;
    private IDebuffable debuffComponent;
    private IHoming homingComponent;
    private GameObject explosionEffectPrefab;
    private Vector2 direction = Vector2.right;
    private Vector3 targetPosition;
    private bool isInitialized = false;
    private float explosionTimer = 0f;
    private bool hasExploded = false;
    private float currentLifetime;
    private int pierceCount = 0;
    private bool hasHitTarget = false;
    private GameObject[] inFlightEffectInstances;
    private GameObject[] onContactEffectPrefabs;

    public Action<Projectile> OnProjectileDestroyed;
    public Action<Projectile, Enemy> OnProjectileHitEnemy;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (projectileCollider == null)
            projectileCollider = GetComponent<Collider2D>();

        if (rb != null)
            rb.gravityScale = 0f;
    }

    public void Initialize(Vector2 dir, ProjectileData projectileData, Enemy target = null, GameObject[] inFlightEffects = null, GameObject[] onContactEffects = null)
    {
        data = projectileData.Clone();
        direction = Vector2.right;
        isInitialized = true;
        hasExploded = false;
        currentLifetime = data.lifetime;
        pierceCount = 0;
        hasHitTarget = false;
        onContactEffectPrefabs = onContactEffects;

        if (inFlightEffects != null && inFlightEffects.Length > 0)
        {
            inFlightEffectInstances = new GameObject[inFlightEffects.Length];
            for (int i = 0; i < inFlightEffects.Length; i++)
            {
                if (inFlightEffects[i] != null)
                    inFlightEffectInstances[i] = Instantiate(inFlightEffects[i], transform.position, Quaternion.identity, transform);
            }
        }

        if (data.hasKnockback)
            knockbackComponent = new KnockbackComponent(data.knockbackForce, data.knockbackDuration);

        if (data.hasAreaEffect || data.explodesOnContact)
            explosionComponent = gameObject.AddComponent<ExplosionComponent>().Initialize(data.areaRadius, explosionEffectPrefab, this);

        if (data.usesArc)
            arcTrajectoryComponent = gameObject.AddComponent<ArcTrajectoryComponent>().Initialize(transform, rb);

        if (data.debuffDuration > 0f)
            debuffComponent = new DebuffComponent(data.debuffType, data.debuffDuration, data.debuffIntensity);

        if (data.isHoming)
            homingComponent = gameObject.AddComponent<HomingComponent>().Initialize(data.homingStrength, data.homingRange, transform);

        if (data.explosionTimer > 0f)
            explosionTimer = data.explosionTimer;

        if (data.usesArc && targetPosition != Vector3.zero)
            arcTrajectoryComponent?.SetupArc(transform.position, targetPosition, data.arcHeight, data.speed);
        else if (rb != null)
            rb.linearVelocity = direction * data.speed;

        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
            trailRenderer.Clear();
        }
        transform.rotation = Quaternion.identity;
    }

    public void SetTargetPosition(Vector3 target)
    {
        targetPosition = target;
        if (isInitialized && data.usesArc)
            arcTrajectoryComponent?.SetupArc(transform.position, targetPosition, data.arcHeight, data.speed);
    }

    void Update()
    {
        if (!isInitialized)
            return;

        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            DestroyProjectile();
            return;
        }

        if (data.explosionTimer > 0f && !hasExploded)
        {
            explosionTimer -= Time.deltaTime;
            if (explosionTimer <= 0f)
            {
                TriggerOnContactEffects(transform.position);
                explosionComponent?.Explode(transform.position, data.damage, data.areaRadius, debuffComponent, knockbackComponent);
                hasExploded = true;
                DestroyProjectile();
                return;
            }
        }

        if (arcTrajectoryComponent != null)
        {
            arcTrajectoryComponent.UpdateArcMovement();
            if (arcTrajectoryComponent.IsArcComplete())
            {
                if (data.explodesOnContact)
                {
                    TriggerOnContactEffects(transform.position);
                    explosionComponent?.Explode(transform.position, data.damage, data.areaRadius, debuffComponent, knockbackComponent);
                }
                else
                    CheckForEnemiesAtPosition(transform.position);
            }
        }
        else
        {
            if (homingComponent != null)
                homingComponent.UpdateHoming();

            if (rb != null)
                rb.linearVelocity = direction * data.speed;

            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || hasExploded)
            return;

        if (arcTrajectoryComponent != null && !arcTrajectoryComponent.IsArcComplete())
            return;

        if (other.CompareTag(enemyTag))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead() && pierceCount < data.maxPierceCount)
                HitEnemy(enemy);
        }
    }

    void HitEnemy(Enemy enemy)
    {
        if (enemy == null || enemy.IsDead())
            return;

        hasHitTarget = true;
        enemy.TakeDamage(data.damage, data.debuffType, data.debuffDuration, data.debuffIntensity);
        debuffComponent?.ApplyDebuff(enemy);

        if (knockbackComponent != null)
        {
            Vector2 knockbackDirection = direction;
            knockbackComponent.ApplyKnockback(enemy, knockbackDirection, 1f, data.knockbackDuration);
        }

        OnProjectileHitEnemy?.Invoke(this, enemy);
        TriggerOnContactEffects(transform.position);

        if (data.piercing && pierceCount < data.maxPierceCount)
        {
            pierceCount++;
            if (data.hasAreaEffect)
            {
                explosionComponent?.Explode(transform.position, data.damage, data.areaRadius, debuffComponent, knockbackComponent);
                TriggerOnContactEffects(transform.position);
            }
        }
        else
        {
            if (data.hasAreaEffect)
            {
                explosionComponent?.Explode(transform.position, data.damage, data.areaRadius, debuffComponent, knockbackComponent);
                TriggerOnContactEffects(transform.position);
            }
            else
                DestroyProjectile();
        }
    }

    void CheckForEnemiesAtPosition(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.5f);
        bool hitEnemy = false;

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag(enemyTag))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsDead())
                {
                    HitEnemy(enemy);
                    hitEnemy = true;
                }
            }
        }

        if (hitEnemy || data.hasAreaEffect)
        {
            TriggerOnContactEffects(position);
            explosionComponent?.Explode(position, data.damage, data.areaRadius, debuffComponent, knockbackComponent);
        }
        else
            DestroyProjectile();
    }

    public void TriggerOnContactEffects(Vector3 position)
    {
        if (onContactEffectPrefabs != null && onContactEffectPrefabs.Length > 0)
        {
            foreach (GameObject effectPrefab in onContactEffectPrefabs)
            {
                if (effectPrefab != null)
                {
                    GameObject effectInstance = Instantiate(effectPrefab, position, Quaternion.identity);
                    ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        float duration = ps.main.duration;
                        Destroy(effectInstance, duration);
                    }
                    else
                        Destroy(effectInstance, 1f);
                }
            }
        }
    }

    public void DestroyProjectile()
    {
        if (trailRenderer != null)
            trailRenderer.enabled = false;

        if (inFlightEffectInstances != null)
        {
            foreach (GameObject effect in inFlightEffectInstances)
            {
                if (effect != null)
                    Destroy(effect);
            }
            inFlightEffectInstances = null;
        }

        OnProjectileDestroyed?.Invoke(this);
        gameObject.SetActive(false);
    }

    public ProjectileData GetProjectileData() => data;
    public float GetRemainingLifetime() => currentLifetime;
    public int GetPierceCount() => pierceCount;

    void OnDrawGizmos()
    {
        if (data != null)
        {
            if (data.usesArc && targetPosition != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = transform.position;
                Vector3 end = targetPosition;

                for (int i = 0; i <= 20; i++)
                {
                    float t = i / 20f;
                    Vector3 pos = Vector3.Lerp(start, end, t);
                    pos.y += data.arcHeight * 4f * t * (1f - t);

                    if (i > 0)
                    {
                        float prevT = (i - 1) / 20f;
                        Vector3 prevPos = Vector3.Lerp(start, end, prevT);
                        prevPos.y += data.arcHeight * 4f * prevT * (1f - prevT);
                        Gizmos.DrawLine(prevPos, pos);
                    }
                }
            }

            if (data.hasAreaEffect)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, data.areaRadius);
            }

            if (data.isHoming)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, data.homingRange);
            }
        }
    }
}
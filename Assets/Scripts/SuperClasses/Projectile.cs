using UnityEngine;
using System;

[System.Serializable]
public class ProjectileData
{
    [Header("Basic Properties")]
    public float damage = 10f;
    public float speed = 5f;
    public float lifetime = 5f;

    [Header("Debuff Properties")]
    public DebuffType debuffType = DebuffType.None;
    public float debuffDuration = 0f;
    public float debuffIntensity = 0f;

    [Header("Special Properties")]
    public bool piercing = false;
    public int maxPierceCount = 1;
    public bool hasAreaEffect = false;
    public float areaRadius = 1f;

    [Header("Knockback Properties")]
    public bool hasKnockback = false;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    [Header("Homing Properties")]
    public bool isHoming = true;
    public float homingStrength = 2f;
    public float homingRange = 3f;

    [Header("Trajectory Properties")]
    public bool usesArc = false;
    public float arcHeight = 2f;

    [Header("Explosion Properties")]
    public bool explodesOnContact = true;
    public float explosionTimer = 0f;

    public ProjectileData Clone()
    {
        return new ProjectileData
        {
            damage = this.damage,
            speed = this.speed,
            lifetime = this.lifetime,
            debuffType = this.debuffType,
            debuffDuration = this.debuffDuration,
            debuffIntensity = this.debuffIntensity,
            piercing = this.piercing,
            maxPierceCount = this.maxPierceCount,
            hasAreaEffect = this.hasAreaEffect,
            areaRadius = this.areaRadius,
            hasKnockback = this.hasKnockback,
            knockbackForce = this.knockbackForce,
            knockbackDuration = this.knockbackDuration,
            isHoming = this.isHoming,
            homingStrength = this.homingStrength,
            homingRange = this.homingRange,
            usesArc = this.usesArc,
            arcHeight = this.arcHeight,
            explodesOnContact = this.explodesOnContact,
            explosionTimer = this.explosionTimer
        };
    }
}

public class Projectile : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D projectileCollider;
    [SerializeField] private LayerMask enemyLayerMask = 9;

    private ProjectileData data;
    private Enemy targetEnemy;
    private Vector2 direction;
    private Vector3 targetPosition;
    private bool isInitialized = false;
    private float explosionTimer = 0f;
    private bool hasExploded = false;
    private float currentLifetime;
    private int pierceCount = 0;
    private bool hasHitTarget = false;

    // Arc trajectory variables
    private Vector3 startPosition;
    private float arcProgress = 0f;
    private bool usingArcTrajectory = false;
    private float totalDistance;
    private float arcSpeed;

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

    public void Initialize(Vector2 dir, ProjectileData projectileData, Enemy target = null)
    {
        direction = dir.normalized;
        data = projectileData.Clone();
        targetEnemy = target;
        isInitialized = true;
        hasExploded = false;
        currentLifetime = data.lifetime;
        pierceCount = 0;
        hasHitTarget = false;

        if (data.explosionTimer > 0f)
            explosionTimer = data.explosionTimer;

        if (data.usesArc && targetPosition != Vector3.zero)
        {
            SetupArcTrajectory();
        }
        else
        {
            if (rb != null)
                rb.linearVelocity = direction * data.speed;
        }

        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
            trailRenderer.Clear();
        }

        Debug.Log($"Projectile initialized: Arc={data.usesArc}, Homing={data.isHoming}, Direction={direction}, Speed={data.speed}");
    }

    public void SetTargetPosition(Vector3 target)
    {
        targetPosition = target;

        if (isInitialized && data.usesArc)
            SetupArcTrajectory();
    }

    void SetupArcTrajectory()
    {
        startPosition = transform.position;
        totalDistance = Vector3.Distance(startPosition, targetPosition);
        arcSpeed = data.speed;
        arcProgress = 0f;
        usingArcTrajectory = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Debug.Log($"Arc trajectory setup: Start={startPosition}, Target={targetPosition}, Distance={totalDistance}");
    }

    void Update()
    {
        if (!isInitialized)
            return;

        // Handle lifetime
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            DestroyProjectile();
            return;
        }

        // Handle explosion timer
        if (data.explosionTimer > 0f && !hasExploded)
        {
            explosionTimer -= Time.deltaTime;
            if (explosionTimer <= 0f)
            {
                ExplodeProjectile();
                return;
            }
        }

        if (usingArcTrajectory)
        {
            UpdateArcMovement();
        }
        else
        {
            // Handle homing for non-arc projectiles
            if (data.isHoming && !hasHitTarget)
                HandleHoming();

            // Update velocity and rotation for standard projectiles
            if (rb != null)
                rb.linearVelocity = direction * data.speed;

            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    void HandleHoming()
    {
        // Find target if we don't have one or if current target is dead
        if (targetEnemy == null || targetEnemy.IsDead())
            targetEnemy = FindClosestEnemy();

        if (targetEnemy != null)
        {
            Vector2 targetDirection = ((Vector2)targetEnemy.transform.position - (Vector2)transform.position).normalized;
            direction = Vector2.Lerp(direction, targetDirection, data.homingStrength * Time.deltaTime).normalized;
        }
    }

    Enemy FindClosestEnemy()
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, data.homingRange, enemyLayerMask);
        Enemy closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D col in nearbyEnemies)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead())
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        return closestEnemy;
    }

    void UpdateArcMovement()
    {
        if (hasExploded)
            return;

        arcProgress += (arcSpeed / totalDistance) * Time.deltaTime;

        if (arcProgress >= 1f)
        {
            transform.position = targetPosition;

            if (data.explosionTimer > 0f || !data.explodesOnContact)
                ExplodeProjectile();
            else
                CheckForEnemiesAtPosition(targetPosition);
            return;
        }

        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, arcProgress);

        float arcHeightMultiplier = 4f * arcProgress * (1f - arcProgress);
        currentPos.y += data.arcHeight * arcHeightMultiplier;

        transform.position = currentPos;

        if (arcProgress > 0.01f)
        {
            Vector3 lastPos = Vector3.Lerp(startPosition, targetPosition, arcProgress - 0.01f);
            lastPos.y += data.arcHeight * 4f * (arcProgress - 0.01f) * (1f - (arcProgress - 0.01f));

            Vector3 moveDirection = (currentPos - lastPos).normalized;
            if (moveDirection != Vector3.zero)
            {
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    void CheckForEnemiesAtPosition(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.5f, enemyLayerMask);
        bool hitEnemy = false;

        foreach (Collider2D col in colliders)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                HitEnemy(enemy);
                hitEnemy = true;
            }
        }

        if (hitEnemy || data.hasAreaEffect)
        {
            ExplodeProjectile();
        }
        else
        {
            DestroyProjectile();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || hasExploded)
            return;

        if (usingArcTrajectory && arcProgress < 0.95f)
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null && !enemy.IsDead() && pierceCount < data.maxPierceCount)
        {
            HitEnemy(enemy);
        }
    }

    void HitEnemy(Enemy enemy)
    {
        if (enemy == null || enemy.IsDead())
            return;

        hasHitTarget = true;

        enemy.TakeDamage(data.damage);

        if (data.debuffDuration > 0f)
        {
            Debuff debuff = new Debuff(data.debuffType, data.debuffDuration, data.debuffIntensity);
            enemy.ApplyDebuff(debuff);
        }

        if (data.hasKnockback)
        {
            Vector2 knockbackDirection;
            if (usingArcTrajectory)
                knockbackDirection = (enemy.transform.position - transform.position).normalized;
            else
                knockbackDirection = new Vector2(direction.x, 0f).normalized;

            enemy.ApplyKnockback(knockbackDirection, data.knockbackForce, data.knockbackDuration);
        }

        OnProjectileHitEnemy?.Invoke(this, enemy);

        Debug.Log($"Projectile hit {enemy.name} for {data.damage} damage");

        // Handle piercing
        if (data.piercing && pierceCount < data.maxPierceCount)
        {
            pierceCount++;
            Debug.Log($"Projectile pierced enemy! Pierce count: {pierceCount}/{data.maxPierceCount}");

            if (data.hasAreaEffect)
                ExplodeProjectile();
        }
        else
        {
            if (data.hasAreaEffect)
                ExplodeProjectile();
            else
                DestroyProjectile();
        }
    }

    void ExplodeProjectile()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        Debug.Log($"Projectile exploding at {transform.position}");

        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        if (data.hasAreaEffect)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, data.areaRadius, enemyLayerMask);

            foreach (Collider2D col in colliders)
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsDead())
                {
                    float distance = Vector2.Distance(transform.position, enemy.transform.position);
                    float damageMultiplier = 1f - (distance / data.areaRadius);
                    damageMultiplier = Mathf.Clamp01(damageMultiplier);

                    float areaDamage = data.damage * damageMultiplier;
                    enemy.TakeDamage(areaDamage);

                    if (data.debuffDuration > 0f)
                    {
                        Debuff debuff = new Debuff(data.debuffType, data.debuffDuration, data.debuffIntensity);
                        enemy.ApplyDebuff(debuff);
                    }

                    if (data.hasKnockback)
                    {
                        Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
                        float knockbackMultiplier = damageMultiplier;
                        enemy.ApplyKnockback(knockbackDirection, data.knockbackForce * knockbackMultiplier, data.knockbackDuration);
                    }

                    Debug.Log($"Area explosion hit {enemy.name} for {areaDamage} damage (multiplier: {damageMultiplier})");
                }
            }

            Debug.Log($"Area effect hit {colliders.Length} enemies with {data.areaRadius} radius");
        }

        DestroyProjectile();
    }

    void DestroyProjectile()
    {
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }

        OnProjectileDestroyed?.Invoke(this);

        gameObject.SetActive(false);
    }

    public ProjectileData GetProjectileData()
    {
        return data;
    }

    public float GetRemainingLifetime() => currentLifetime;
    public int GetPierceCount() => pierceCount;

    void OnDrawGizmos()
    {
        // Draw arc trajectory
        if (data != null && data.usesArc && targetPosition != Vector3.zero)
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

        // Draw area effect radius
        if (data != null && data.hasAreaEffect)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.areaRadius);
        }

        // Draw homing range
        if (data != null && data.isHoming)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, data.homingRange);
        }
    }
}
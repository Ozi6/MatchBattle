using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using DamageNumbersPro;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float baseMovementSpeed = 2f;
    [SerializeField] private float currentMovementSpeed;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Visual Components")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthFill;
    [SerializeField] private Transform debuffIconContainer;
    [SerializeField] private GameObject debuffIconPrefab;
    [SerializeField] private DamageNumber damageNumberPrefab;
    [SerializeField] private Material whiteFlashMaterial;

    [Header("Movement")]
    [SerializeField] private Transform targetPlayer;
    [SerializeField] private bool canMove = true;

    [Header("Combat")]
    [SerializeField] private Player playerScript;

    private List<Debuff> activeDebuffs = new List<Debuff>();
    private Dictionary<DebuffType, GameObject> debuffIcons = new Dictionary<DebuffType, GameObject>();
    private float lastAttackTime;
    private bool isDead = false;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isKnockbacked = false;
    private float knockbackTimer = 0f;

    private Dictionary<DebuffType, float> debuffTimers = new Dictionary<DebuffType, float>();

    public Action<Enemy> OnEnemyDeath;
    public Action<Enemy, float> OnEnemyTakeDamage;
    public Action<Enemy, float> OnEnemyAttackPlayer;

    private float[] enemyBaseData = new float[2];
    private bool isFlashing = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentHealth = maxHealth;
        currentMovementSpeed = baseMovementSpeed;
        enemyBaseData[0] = currentHealth;
        enemyBaseData[1] = attackDamage;

        if (targetPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                targetPlayer = player.transform;
        }

        InitializeHealthBar();
        FindAndSetPlayerTarget();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            bool hasDeathState = false;
            foreach (var state in animator.runtimeAnimatorController.animationClips)
            {
                if (state.name.ToLower().Contains("death"))
                {
                    hasDeathState = true;
                    break;
                }
            }
            if (!hasDeathState)
                Debug.LogWarning($"No 'Death' animation found in Animator for {gameObject.name}. Ensure the death animation is set up in the Animator Controller.");
        }
        else
        {
            Debug.LogWarning($"Animator or AnimatorController missing on {gameObject.name}. Death animation will not play.");
        }
    }

    private void FindAndSetPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerScript = player.GetComponent<Player>();

            if (playerScript == null)
                Debug.LogWarning($"Player GameObject found but no Player script component attached!", this);
        }
        else
        {
            Debug.LogWarning($"No GameObject with tag 'Player' found in the scene!", this);
            StartCoroutine(TryFindPlayer());
        }
    }

    private IEnumerator TryFindPlayer()
    {
        float retryDelay = 0.5f;
        int maxAttempts = 3;
        int attempts = 0;

        while (attempts < maxAttempts && targetPlayer == null)
        {
            yield return new WaitForSeconds(retryDelay);
            attempts++;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetPlayer = player.transform;
                playerScript = player.GetComponent<Player>();
                Debug.Log($"Player found after {attempts} attempts!", this);
            }
        }

        if (targetPlayer == null)
            Debug.LogError($"Failed to find Player after {maxAttempts} attempts!", this);
    }

    void Start()
    {
        UpdateHealthBar();
    }

    void Update()
    {
        if (healthBar != null && !healthBar.gameObject.activeInHierarchy)
        {
            healthBar.gameObject.SetActive(true);
            currentHealth = maxHealth = healthBar.value = healthBar.maxValue = enemyBaseData[0];
            attackDamage = enemyBaseData[1];
            UpdateHealthBar();
        }

        if (isDead)
            return;

        ProcessDebuffs();
        HandleKnockback();
        HandleMovement();
        HandleAttack();
    }

    void InitializeHealthBar()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<Slider>();
            if (healthBar == null)
            {
                Debug.LogWarning("No health bar found as child of this enemy! Please assign it in the inspector.", this);
                return;
            }
        }

        if (healthBar != null && !healthBar.gameObject.activeInHierarchy)
            healthBar.gameObject.SetActive(true);

        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
        healthBar.interactable = false;

        if (healthFill == null)
        {
            Transform fillTransform = healthBar.transform.Find("Fill Area/Fill");
            if (fillTransform != null)
                healthFill = fillTransform.GetComponent<Image>();

            if (healthFill == null)
                Debug.LogWarning("HealthFill Image not found in health bar! Ensure Fill Area/Fill exists.", this);
        }
    }

    void HandleMovement()
    {
        if (!canMove || targetPlayer == null || currentMovementSpeed <= 0 || isKnockbacked)
        {
            if (animator != null)
                animator.SetFloat("RunState", 0.5f);
            return;
        }

        Vector2 direction = new Vector2(targetPlayer.position.x - transform.position.x, 0f).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer > attackRange)
        {
            rb.linearVelocity = direction * currentMovementSpeed;

            if (spriteRenderer != null)
                spriteRenderer.flipX = direction.x < 0;

            if (animator != null)
                animator.SetFloat("RunState", 0.5f);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetFloat("RunState", 0f);
        }
    }

    void HandleAttack()
    {
        if (targetPlayer == null || isDead || isKnockbacked)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    void HandleKnockback()
    {
        if (isKnockbacked)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockbacked = false;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    void AttackPlayer()
    {
        if (animator != null)
            animator.SetTrigger("Attack");

        if (playerScript != null)
            playerScript.TakeDamage(attackDamage);
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Player foundPlayer = playerObj.GetComponent<Player>();
                if (foundPlayer != null)
                {
                    foundPlayer.TakeDamage(attackDamage);
                    playerScript = foundPlayer;
                }
            }
        }

        OnEnemyAttackPlayer?.Invoke(this, attackDamage);
        Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage!");
    }

    public void TakeDamage(float damage, DebuffType debuffType = DebuffType.Bleed, float debuffDuration = 0f, float debuffIntensity = 0f)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (damageNumberPrefab != null)
        {
            Vector3 damagePosition = transform.position + new Vector3(0f, 1f, 0f);
            DamageNumber damageNumber = damageNumberPrefab.Spawn(damagePosition, damage);
            damageNumber.SetFollowedTarget(transform);
            damageNumber.SetColor(GetDamageColor(debuffType));
        }

        if (debuffDuration > 0)
            ApplyDebuff(new Debuff(debuffType, debuffDuration, debuffIntensity));

        UpdateHealthBar();
        OnEnemyTakeDamage?.Invoke(this, damage);

        if(!isFlashing)
            StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
            StartCoroutine(Die());

        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (isDead)
            return;

        isKnockbacked = true;
        knockbackTimer = duration;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        Debug.Log($"{gameObject.name} received knockback: force={force}, duration={duration}");
    }

    public void ApplyDebuff(Debuff debuff)
    {
        if (isDead)
            return;

        Debuff existingDebuff = activeDebuffs.Find(d => d.type == debuff.type);

        if (existingDebuff != null)
        {
            existingDebuff.timeRemaining = debuff.duration;
            if (debuff.intensity > existingDebuff.intensity)
                existingDebuff.intensity = debuff.intensity;
        }
        else
        {
            activeDebuffs.Add(debuff);
            CreateDebuffIcon(debuff.type);
            ApplyDebuffEffect(debuff);
        }

        Debug.Log($"{gameObject.name} got debuff: {debuff.type} for {debuff.duration}s with intensity {debuff.intensity}");
    }

    void ProcessDebuffs()
    {
        for (int i = activeDebuffs.Count - 1; i >= 0; i--)
        {
            Debuff debuff = activeDebuffs[i];
            debuff.timeRemaining -= Time.deltaTime;

            if (debuff.type == DebuffType.Bleed || debuff.type == DebuffType.Poison)
            {
                if (!debuffTimers.ContainsKey(debuff.type))
                    debuffTimers[debuff.type] = 0f;

                debuffTimers[debuff.type] += Time.deltaTime;

                if (debuffTimers[debuff.type] >= debuff.tickInterval)
                {
                    TakeDamage(debuff.intensity, debuff.type);
                    debuffTimers[debuff.type] = 0f;
                }
            }

            if (debuff.timeRemaining <= 0)
            {
                RemoveDebuff(debuff.type);
                activeDebuffs.RemoveAt(i);
            }
        }
    }

    void ApplyDebuffEffect(Debuff debuff)
    {
        switch (debuff.type)
        {
            case DebuffType.Slow:
                float slowPercentage = debuff.intensity / 100f;
                currentMovementSpeed = baseMovementSpeed * (1f - slowPercentage);
                break;

            case DebuffType.Stun:
            case DebuffType.Freeze:
                canMove = false;
                currentMovementSpeed = 0f;
                break;
        }
    }

    void RemoveDebuff(DebuffType debuffType)
    {
        switch (debuffType)
        {
            case DebuffType.Slow:
                RecalculateMovementSpeed();
                break;

            case DebuffType.Stun:
            case DebuffType.Freeze:
                bool hasMovementImpairingDebuff = activeDebuffs.Exists(d =>
                    d.type == DebuffType.Stun || d.type == DebuffType.Freeze);
                if (!hasMovementImpairingDebuff)
                {
                    canMove = true;
                    RecalculateMovementSpeed();
                }
                break;
        }

        if (debuffIcons.ContainsKey(debuffType))
        {
            Destroy(debuffIcons[debuffType]);
            debuffIcons.Remove(debuffType);
        }

        if (debuffTimers.ContainsKey(debuffType))
            debuffTimers.Remove(debuffType);
    }

    void RecalculateMovementSpeed()
    {
        currentMovementSpeed = baseMovementSpeed;

        foreach (Debuff debuff in activeDebuffs)
        {
            if (debuff.type == DebuffType.Slow)
            {
                float slowPercentage = debuff.intensity / 100f;
                currentMovementSpeed *= (1f - slowPercentage);
            }
        }
    }

    void CreateDebuffIcon(DebuffType debuffType)
    {
        if (debuffIconContainer == null || debuffIconPrefab == null)
            return;

        if (!debuffIcons.ContainsKey(debuffType))
        {
            GameObject icon = Instantiate(debuffIconPrefab, debuffIconContainer);
            debuffIcons[debuffType] = icon;

            Image iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
                iconImage.color = GetDebuffColor(debuffType);

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                float iconSpacing = 0.3f;
                int iconCount = debuffIcons.Count;
                iconRect.anchoredPosition = new Vector2((iconCount - 1) * iconSpacing, 0f);
            }
        }
    }

    Color GetDebuffColor(DebuffType debuffType)
    {
        switch (debuffType)
        {
            case DebuffType.Bleed: return Color.red;
            case DebuffType.Poison: return Color.green;
            case DebuffType.Slow: return Color.blue;
            case DebuffType.Stun: return Color.yellow;
            case DebuffType.Freeze: return Color.cyan;
            default: return Color.white;
        }
    }

    Color GetDamageColor(DebuffType debuffType)
    {
        return GetDebuffColor(debuffType);
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            healthBar.gameObject.SetActive(currentHealth <= maxHealth && currentHealth > 0);

            if (healthFill != null)
            {
                float healthPercent = currentHealth / maxHealth;
                healthFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
            }
        }
    }

    IEnumerator DamageFlash()
    {
        isFlashing = true;

        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        Material[] originalMaterials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalMaterials[i] = spriteRenderers[i].material;
            spriteRenderers[i].material = whiteFlashMaterial;
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].material = originalMaterials[i];

        isFlashing = false;
    }

    IEnumerator Die()
    {
        if (isDead)
            yield break;

        isDead = true;
        canMove = false;

        if (animator != null)
        {
            animator.SetTrigger("Death");

            float deathAnimationLength = 0f;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.ToLower().Contains("death"))
                {
                    deathAnimationLength = clip.length;
                    break;
                }
            }

            yield return new WaitForSeconds(deathAnimationLength);
        }
        else
            yield return new WaitForSeconds(2f);

        OnEnemyDeath?.Invoke(this);

        foreach (var icon in debuffIcons.Values)
        {
            if (icon != null)
                Destroy(icon);
        }
        debuffIcons.Clear();

        Debug.Log($"{gameObject.name} died!");

        Destroy(gameObject);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetMovementSpeed() => currentMovementSpeed;
    public bool IsDead() => isDead;
    public List<Debuff> GetActiveDebuffs() => new List<Debuff>(activeDebuffs);

    public void SetMaxHealth(float health)
    {
        maxHealth = health;
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void SetMovementSpeed(float speed)
    {
        baseMovementSpeed = speed;
        if (!activeDebuffs.Exists(d => d.type == DebuffType.Slow))
            currentMovementSpeed = baseMovementSpeed;
    }

    public void SetAttackDamage(float damage) => attackDamage = damage;
    public void SetAttackRange(float range) => attackRange = range;
    public void SetAttackCooldown(float cooldown) => attackCooldown = cooldown;

    public float GetAttackDamage()
    {
        return attackDamage;
    }
}
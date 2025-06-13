using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

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
    [SerializeField] private Slider healthBarPrefab;
    [SerializeField] private Image healthFill;
    [SerializeField] private Transform debuffIconContainer;
    [SerializeField] private GameObject debuffIconPrefab;

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
    private Slider healthBarInstance;
    private bool isKnockbacked = false;
    private float knockbackTimer = 0f;

    private Dictionary<DebuffType, float> debuffTimers = new Dictionary<DebuffType, float>();

    public Action<Enemy> OnEnemyDeath;
    public Action<Enemy, float> OnEnemyTakeDamage;
    public Action<Enemy, float> OnEnemyAttackPlayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
        currentMovementSpeed = baseMovementSpeed;

        if (targetPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                targetPlayer = player.transform;
        }

        InitializeHealthBar();
        FindAndSetPlayerTarget();
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
        if (!healthBarInstance.IsActive())
            healthBarInstance.gameObject.SetActive(true);

        if (isDead)
            return;

        ProcessDebuffs();
        HandleKnockback();
        HandleMovement();
        HandleAttack();
        UpdateHealthBarPosition();
    }

    void InitializeHealthBar()
    {
        if (healthBarPrefab == null)
        {
            Debug.LogWarning("HealthBar prefab is not assigned!", this);
            return;
        }

        Canvas uiCanvas = FindAnyObjectByType<Canvas>();
        if (uiCanvas == null || uiCanvas.renderMode != RenderMode.WorldSpace)
        {
            GameObject canvasObj = new GameObject("EnemyUICanvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.WorldSpace;
            uiCanvas.worldCamera = Camera.main;
            uiCanvas.transform.localScale = new Vector3(0.02f, 0.02f, 1f);
        }

        healthBarInstance = Instantiate(healthBarPrefab, uiCanvas.transform);
        healthBarInstance.gameObject.SetActive(true);

        healthBarInstance.name = $"{gameObject.name}_HealthBar";

        healthBarInstance.maxValue = maxHealth;
        healthBarInstance.value = currentHealth;
        healthBarInstance.interactable = false;

        Transform fillTransform = healthBarInstance.transform.Find("Fill Area/Fill");
        if (fillTransform != null)
            healthFill = fillTransform.GetComponent<Image>();
        if (healthFill == null)
            Debug.LogWarning("HealthFill Image not found in health bar prefab! Ensure Fill Area/Fill exists.", this);
    }

    void UpdateHealthBarPosition()
    {
        if (healthBarInstance != null)
        {
            Vector3 worldPosition = transform.position + new Vector3(0f, 1f, 0f);
            healthBarInstance.transform.position = worldPosition;
            healthBarInstance.transform.rotation = Quaternion.identity;
        }
    }

    void HandleMovement()
    {
        if (!canMove || targetPlayer == null || currentMovementSpeed <= 0 || isKnockbacked)
            return;

        Vector2 direction = new Vector2(targetPlayer.position.x - transform.position.x, 0f).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer > attackRange)
        {
            rb.linearVelocity = direction * currentMovementSpeed;

            if (spriteRenderer != null)
                spriteRenderer.flipX = direction.x < 0;

            if (animator != null)
            {
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", direction.x);
                animator.SetFloat("MoveY", direction.y);
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetBool("IsMoving", false);
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

        if (debuffDuration > 0)
            ApplyDebuff(new Debuff(debuffType, debuffDuration, debuffIntensity));

        UpdateHealthBar();
        OnEnemyTakeDamage?.Invoke(this, damage);

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
            Die();

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
                    TakeDamage(debuff.intensity);
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

    void UpdateHealthBar()
    {
        if (healthBarInstance != null)
        {
            healthBarInstance.value = currentHealth;
            healthBarInstance.gameObject.SetActive(currentHealth < maxHealth && currentHealth > 0);

            if (healthFill != null)
            {
                float healthPercent = currentHealth / maxHealth;
                healthFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
            }
        }
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        canMove = false;

        if (animator != null)
            animator.SetTrigger("Death");

        OnEnemyDeath?.Invoke(this);

        foreach (var icon in debuffIcons.Values)
        {
            if (icon != null)
                Destroy(icon);
        }
        debuffIcons.Clear();

        if (healthBarInstance != null)
            Destroy(healthBarInstance.gameObject);

        Debug.Log($"{gameObject.name} died!");

        Destroy(gameObject, 2f);
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
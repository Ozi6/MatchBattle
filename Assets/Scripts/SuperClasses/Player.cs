using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class Player : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float baseArmor = 5f;
    [SerializeField] private float baseDamageMultiplier = 1f;

    [Header("UI Components")]
    [SerializeField] private Slider healthBarUI;
    [SerializeField] private Image healthFillUI;
    [SerializeField] private Text healthText;

    [Header("Invulnerability")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private bool isInvulnerable = false;

    private List<PlayerBuff> activeBuffs = new List<PlayerBuff>();
    private float lastDamageTime = 0f;
    private bool isDead = false;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public Action<Player> OnPlayerDeath;
    public Action<Player, float> OnPlayerTakeDamage;
    public Action<Player, float> OnPlayerHeal;
    public Action<Player, float> OnPlayerArmorChanged;
    public Action<Player, float> OnPlayerDamageMultiplierChanged;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        InitializeUI();
        UpdateHealthUI();
    }

    void Update()
    {
        if (isDead)
            return;

        ProcessBuffs();
        UpdateInvulnerability();
    }

    void InitializeUI()
    {
        if (healthBarUI != null)
        {
            healthBarUI.maxValue = maxHealth;
            healthBarUI.value = currentHealth;

            if (healthFillUI == null)
            {
                Transform fillTransform = healthBarUI.transform.Find("Fill Area/Fill");
                if (fillTransform != null)
                    healthFillUI = fillTransform.GetComponent<Image>();
            }
        }
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthBarUI != null)
        {
            healthBarUI.value = currentHealth;

            if (healthFillUI != null)
            {
                float healthPercent = currentHealth / maxHealth;
                healthFillUI.color = Color.Lerp(Color.red, Color.green, healthPercent);
            }
        }

        if (healthText != null)
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
    }

    void ProcessBuffs()
    {
        bool armorChanged = false;
        bool damageChanged = false;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            PlayerBuff buff = activeBuffs[i];
            buff.timeRemaining -= Time.deltaTime;

            if (buff.type == PlayerBuffType.Regeneration)
            {
                buff.tickTimer += Time.deltaTime;
                if (buff.tickTimer >= buff.tickInterval)
                {
                    Heal(buff.intensity);
                    buff.tickTimer = 0f;
                }
            }

            if (buff.timeRemaining <= 0)
            {
                if (buff.type == PlayerBuffType.ArmorBoost)
                    armorChanged = true;
                if (buff.type == PlayerBuffType.DamageBoost)
                    damageChanged = true;

                activeBuffs.RemoveAt(i);
            }
        }

        if (armorChanged)
            OnPlayerArmorChanged?.Invoke(this, GetCurrentArmor());
        if (damageChanged)
            OnPlayerDamageMultiplierChanged?.Invoke(this, GetDamageMultiplier());
    }

    void UpdateInvulnerability()
    {
        if (isInvulnerable && Time.time - lastDamageTime >= invulnerabilityDuration)
        {
            isInvulnerable = false;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead || isInvulnerable)
            return;

        float currentArmor = GetCurrentArmor();
        float finalDamage = Mathf.Max(1f, damage - currentArmor);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        isInvulnerable = true;
        lastDamageTime = Time.time;

        UpdateHealthUI();
        OnPlayerTakeDamage?.Invoke(this, finalDamage);

        StartCoroutine(DamageFlash());

        if (animator != null)
            animator.SetTrigger("TakeDamage");

        CheckGameOver();

        Debug.Log($"Player took {finalDamage} damage (reduced from {damage} by {currentArmor} armor). Health: {currentHealth}/{maxHealth}");
    }

    public void Heal(float healAmount)
    {
        if (isDead)
            return;

        float actualHeal = Mathf.Min(healAmount, maxHealth - currentHealth);
        currentHealth += actualHeal;

        UpdateHealthUI();
        OnPlayerHeal?.Invoke(this, actualHeal);

        if (animator != null && actualHeal > 0)
            animator.SetTrigger("Heal");

        Debug.Log($"Player healed for {actualHeal}. Health: {currentHealth}/{maxHealth}");
    }

    void CheckGameOver()
    {
        if (currentHealth <= 0 && !isDead)
            Die();
    }

    void Die()
    {
        isDead = true;

        if (animator != null)
            animator.SetTrigger("Death");

        OnPlayerDeath?.Invoke(this);
        Debug.Log("Player died! Game Over.");
    }

    public void AddBuff(PlayerBuffType buffType, float duration, float intensity, float tickInterval = 1f)
    {
        if (isDead)
            return;

        PlayerBuff existingBuff = activeBuffs.Find(b => b.type == buffType);

        if (existingBuff != null)
        {
            existingBuff.timeRemaining = duration;
            if (intensity > existingBuff.intensity)
                existingBuff.intensity = intensity;
        }
        else
            activeBuffs.Add(new PlayerBuff(buffType, duration, intensity, tickInterval));

        if (buffType == PlayerBuffType.ArmorBoost)
            OnPlayerArmorChanged?.Invoke(this, GetCurrentArmor());
        else if (buffType == PlayerBuffType.DamageBoost)
            OnPlayerDamageMultiplierChanged?.Invoke(this, GetDamageMultiplier());

        Debug.Log($"Player received buff: {buffType} for {duration}s with intensity {intensity}");
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.5f, 0.5f, 0.8f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public float GetCurrentArmor()
    {
        float totalArmor = baseArmor;
        foreach (PlayerBuff buff in activeBuffs)
        {
            if (buff.type == PlayerBuffType.ArmorBoost)
                totalArmor += buff.intensity;
        }
        return totalArmor;
    }

    public float GetDamageMultiplier()
    {
        float totalMultiplier = baseDamageMultiplier;
        foreach (PlayerBuff buff in activeBuffs)
        {
            if (buff.type == PlayerBuffType.DamageBoost)
                totalMultiplier += buff.intensity;
        }
        return totalMultiplier;
    }

    public float CalculateFinalDamage(float baseDamage)
    {
        return baseDamage * GetDamageMultiplier();
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsDead() => isDead;
    public bool IsInvulnerable() => isInvulnerable;
    public List<PlayerBuff> GetActiveBuffs() => new List<PlayerBuff>(activeBuffs);

    public void SetMaxHealth(float newMaxHealth)
    {
        float healthPercentage = currentHealth / maxHealth;
        maxHealth = newMaxHealth;
        currentHealth = maxHealth * healthPercentage;
        UpdateHealthUI();
    }

    public void SetBaseArmor(float newArmor)
    {
        baseArmor = newArmor;
        OnPlayerArmorChanged?.Invoke(this, GetCurrentArmor());
    }

    public void SetBaseDamageMultiplier(float newMultiplier)
    {
        baseDamageMultiplier = newMultiplier;
        OnPlayerDamageMultiplierChanged?.Invoke(this, GetDamageMultiplier());
    }

    public void FullHeal()
    {
        Heal(maxHealth);
    }
}

[System.Serializable]
public class PlayerBuff
{
    public PlayerBuffType type;
    public float timeRemaining;
    public float intensity;
    public float tickInterval;
    public float tickTimer;

    public PlayerBuff(PlayerBuffType buffType, float duration, float intens, float tickInt = 1f)
    {
        type = buffType;
        timeRemaining = duration;
        intensity = intens;
        tickInterval = tickInt;
        tickTimer = 0f;
    }
}

public enum PlayerBuffType
{
    DamageBoost,
    ArmorBoost,
    Regeneration
}
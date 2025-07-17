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
    [SerializeField] private float baseCritChance = 0.01f;

    [Header("UI Components")]
    [SerializeField] private Slider healthBarUI;
    [SerializeField] private Image healthFillUI;
    [SerializeField] private Text healthText;
    [SerializeField] private GameObject playerObj;

    [Header("Invulnerability")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private bool isInvulnerable = false;

    private List<PlayerBuff> activeBuffs = new List<PlayerBuff>();
    private float lastDamageTime = 0f;
    private bool isDead = false;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private static CombineManager combineManager;
    private static CombatManager combatManager;

    public Action<Player> OnPlayerDeath;
    public Action<Player, float> OnPlayerTakeDamage;
    public Action<Player, float> OnPlayerHeal;
    public Action<Player, float> OnPlayerArmorChanged;
    public Action<Player, float> OnPlayerDamageMultiplierChanged;
    public Action<Player, float> OnPlayerCritChanceChanged;

    void Awake()
    {
        if (playerObj != null && PlayerInventory.Instance != null)
        {
            GameObject selectedCharacterPrefab = PlayerInventory.Instance.GetSelectedCharacterPrefab();
            if (selectedCharacterPrefab != null)
            {
                Vector3 localPosition = Vector3.zero;
                Quaternion localRotation = Quaternion.identity;
                Vector3 localScale = Vector3.one;
                if (playerObj.transform.childCount > 0)
                {
                    Transform oldModelTransform = playerObj.transform.GetChild(0);
                    localPosition = oldModelTransform.localPosition;
                    localRotation = oldModelTransform.localRotation;
                    localScale = oldModelTransform.localScale;
                    Destroy(oldModelTransform.gameObject);
                }
                GameObject newModel = Instantiate(selectedCharacterPrefab, playerObj.transform);
                newModel.transform.localPosition = localPosition;
                newModel.transform.localRotation = localRotation;
                newModel.transform.localScale = localScale;
            }
        }
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        currentHealth = maxHealth;

        combineManager = FindAnyObjectByType<CombineManager>();
        combatManager = FindAnyObjectByType<CombatManager>();
        ApplyAllPerks();
    }

    void Start()
    {
        InitializeUI();
        UpdateHealthUI();

        if (combineManager != null)
            combineManager.OnCombatActionTriggered += HandleCombatAction;
    }

    void OnDestroy()
    {
        if (combineManager != null)
            combineManager.OnCombatActionTriggered -= HandleCombatAction;
    }

    void HandleCombatAction(BlockType blockType, int comboSize, List<PuzzleBlock> matchedBlocks)
    {
        if (combatManager == null || combatManager.combatActionDict == null)
        {
            Debug.LogWarning("CombatManager or combatActionDict is null, cannot process combat action.");
            return;
        }

        if (combatManager.combatActionDict.TryGetValue(blockType, out CombatAction action) && !action.isDefensive)
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
                Debug.Log($"Player triggered Attack animation for {blockType} combo (size: {comboSize})");
            }
        }
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

    void Update()
    {
        if (isDead)
            return;

        ProcessBuffs();
        UpdateInvulnerability();
    }

    void ProcessBuffs()
    {
        bool armorChanged = false;
        bool damageChanged = false;
        bool critChanceChanged = false;

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
                if (buff.type == PlayerBuffType.CritChanceBoost)
                    critChanceChanged = true;

                activeBuffs.RemoveAt(i);
            }
        }

        if (armorChanged)
            OnPlayerArmorChanged?.Invoke(this, GetCurrentArmor());
        if (damageChanged)
            OnPlayerDamageMultiplierChanged?.Invoke(this, GetDamageMultiplier());
        if (critChanceChanged)
            OnPlayerCritChanceChanged?.Invoke(this, GetCritChance());
    }

    void UpdateInvulnerability()
    {
        if (isInvulnerable && Time.time - lastDamageTime >= invulnerabilityDuration)
            isInvulnerable = false;
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
        else if (buffType == PlayerBuffType.CritChanceBoost)
            OnPlayerCritChanceChanged?.Invoke(this, GetCritChance());

        Debug.Log($"Player received buff: {buffType} for {duration}s with intensity {intensity}");
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.5f, 0.5f, 0.8f);
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null && !isInvulnerable)
                spriteRenderer.color = Color.white;
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

    public float GetCritChance()
    {
        float totalCritChance = baseCritChance;
        foreach (PlayerBuff buff in activeBuffs)
        {
            if (buff.type == PlayerBuffType.CritChanceBoost)
                totalCritChance += buff.intensity;
        }
        return Mathf.Clamp(totalCritChance, 0f, 1f);
    }

    public float GetBaseCritChance()
    {
        return baseCritChance;
    }

    public void SetBaseCritChance(float newCritChance)
    {
        baseCritChance = Mathf.Clamp(newCritChance, 0f, 1f);
        OnPlayerCritChanceChanged?.Invoke(this, GetCritChance());
    }

    public float CalculateFinalDamage(float baseDamage)
    {
        float critMultiplier = UnityEngine.Random.value < GetCritChance() ? 2f : 1f;
        return baseDamage * GetDamageMultiplier() * critMultiplier;
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

    private void ApplyAllPerks()
    {
        if (PlayerInventory.Instance != null)
        {
            List<Perk> perks = PlayerInventory.Instance.GetAllPerks();
            foreach (Perk perk in perks)
                if (perk != null)
                    perk.ApplyPerk(this);
        }
    }
}

[Serializable]
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
    Regeneration,
    CritChanceBoost
}

public class InventorySlot
{
    public Item item;
    public int quantity;

    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }

    public InventorySlot(Item newItem, int qty = 1)
    {
        item = newItem;
        quantity = qty;
    }

    public bool IsEmpty()
    {
        return item == null || quantity <= 0;
    }
}
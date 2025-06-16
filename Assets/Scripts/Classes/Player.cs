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
    private static CombineManager combineManager;
    private static CombatManager combatManager;

    public Action<Player> OnPlayerDeath;
    public Action<Player, float> OnPlayerTakeDamage;
    public Action<Player, float> OnPlayerHeal;
    public Action<Player, float> OnPlayerArmorChanged;
    public Action<Player, float> OnPlayerDamageMultiplierChanged;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        currentHealth = maxHealth;

        combineManager = FindAnyObjectByType<CombineManager>();
        combatManager = FindAnyObjectByType<CombatManager>();
    }

    void Start()
    {
        InitializeUI();
        UpdateHealthUI();

        if (combineManager != null)
        {
            combineManager.OnCombatActionTriggered += HandleCombatAction;
        }
    }

    void OnDestroy()
    {
        if (combineManager != null)
        {
            combineManager.OnCombatActionTriggered -= HandleCombatAction;
        }
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

public class Item
{
    public int id;
    public string name;
    public string description;
    public ItemType itemType;
    public Sprite icon;
    public ItemRarity rarity;

    [Header("Stat Bonuses")]
    public float healthBonus;
    public float armorBonus;
    public float damageBonus;

    public Item(int itemId, string itemName, string desc, ItemType type, ItemRarity itemRarity)
    {
        id = itemId;
        name = itemName;
        description = desc;
        itemType = type;
        rarity = itemRarity;
    }
}

public enum ItemType
{
    OffhandWeapon,
    Helmet,
    Boots,
    ChestGuard,
    Charm,
    Consumable
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[System.Serializable]
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

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private List<InventorySlot> inventorySlots;

    [Header("Equipment Slots")]
    [SerializeField] private Item equippedOffhandWeapon;
    [SerializeField] private Item equippedHelmet;
    [SerializeField] private Item equippedBoots;
    [SerializeField] private Item equippedChestGuard;
    [SerializeField] private Item equippedCharm1;
    [SerializeField] private Item equippedCharm2;

    private Player player;

    public Action<Item> OnItemEquipped;
    public Action<Item> OnItemUnequipped;
    public Action<Item, int> OnItemAdded;
    public Action<Item, int> OnItemRemoved;

    void Awake()
    {
        player = GetComponent<Player>();
        InitializeInventory();
    }

    void InitializeInventory()
    {
        inventorySlots = new List<InventorySlot>();
        for (int i = 0; i < inventorySize; i++)
        {
            inventorySlots.Add(new InventorySlot());
        }
    }

    public bool AddItem(Item item, int quantity = 1)
    {
        if (item == null) return false;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (!inventorySlots[i].IsEmpty() && inventorySlots[i].item.id == item.id)
            {
                inventorySlots[i].quantity += quantity;
                OnItemAdded?.Invoke(item, quantity);
                return true;
            }
        }

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].IsEmpty())
            {
                inventorySlots[i] = new InventorySlot(item, quantity);
                OnItemAdded?.Invoke(item, quantity);
                return true;
            }
        }

        return false;
    }

    public bool RemoveItem(Item item, int quantity = 1)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (!inventorySlots[i].IsEmpty() && inventorySlots[i].item.id == item.id)
            {
                if (inventorySlots[i].quantity >= quantity)
                {
                    inventorySlots[i].quantity -= quantity;
                    if (inventorySlots[i].quantity <= 0)
                    {
                        inventorySlots[i] = new InventorySlot();
                    }
                    OnItemRemoved?.Invoke(item, quantity);
                    return true;
                }
            }
        }
        return false;
    }

    public bool EquipItem(Item item)
    {
        if (item == null) return false;

        Item previousItem = null;

        switch (item.itemType)
        {
            case ItemType.OffhandWeapon:
                previousItem = equippedOffhandWeapon;
                equippedOffhandWeapon = item;
                break;
            case ItemType.Helmet:
                previousItem = equippedHelmet;
                equippedHelmet = item;
                break;
            case ItemType.Boots:
                previousItem = equippedBoots;
                equippedBoots = item;
                break;
            case ItemType.ChestGuard:
                previousItem = equippedChestGuard;
                equippedChestGuard = item;
                break;
            case ItemType.Charm:
                if (equippedCharm1 == null)
                {
                    equippedCharm1 = item;
                }
                else if (equippedCharm2 == null)
                {
                    equippedCharm2 = item;
                }
                else
                {
                    previousItem = equippedCharm1;
                    equippedCharm1 = item;
                }
                break;
            default:
                return false;
        }

        if (previousItem != null)
        {
            AddItem(previousItem);
            RemoveEquipmentStats(previousItem);
        }

        RemoveItem(item);

        ApplyEquipmentStats(item);

        OnItemEquipped?.Invoke(item);
        return true;
    }

    public bool UnequipItem(ItemType itemType, int charmSlot = 1)
    {
        Item itemToUnequip = null;

        switch (itemType)
        {
            case ItemType.OffhandWeapon:
                itemToUnequip = equippedOffhandWeapon;
                equippedOffhandWeapon = null;
                break;
            case ItemType.Helmet:
                itemToUnequip = equippedHelmet;
                equippedHelmet = null;
                break;
            case ItemType.Boots:
                itemToUnequip = equippedBoots;
                equippedBoots = null;
                break;
            case ItemType.ChestGuard:
                itemToUnequip = equippedChestGuard;
                equippedChestGuard = null;
                break;
            case ItemType.Charm:
                if (charmSlot == 1)
                {
                    itemToUnequip = equippedCharm1;
                    equippedCharm1 = null;
                }
                else
                {
                    itemToUnequip = equippedCharm2;
                    equippedCharm2 = null;
                }
                break;
        }

        if (itemToUnequip != null)
        {
            AddItem(itemToUnequip);
            RemoveEquipmentStats(itemToUnequip);
            OnItemUnequipped?.Invoke(itemToUnequip);
            return true;
        }

        return false;
    }

    void ApplyEquipmentStats(Item item)
    {
        if (player == null) return;

        if (item.healthBonus > 0)
        {
            player.SetMaxHealth(player.GetMaxHealth() + item.healthBonus);
        }

        if (item.armorBonus > 0)
        {
            player.SetBaseArmor(player.GetCurrentArmor() + item.armorBonus);
        }

        if (item.damageBonus > 0)
        {
            player.SetBaseDamageMultiplier(player.GetDamageMultiplier() + item.damageBonus);
        }
    }

    void RemoveEquipmentStats(Item item)
    {
        if (player == null) return;

        if (item.healthBonus > 0)
        {
            player.SetMaxHealth(player.GetMaxHealth() - item.healthBonus);
        }

        if (item.armorBonus > 0)
        {
            player.SetBaseArmor(player.GetCurrentArmor() - item.armorBonus);
        }

        if (item.damageBonus > 0)
        {
            player.SetBaseDamageMultiplier(player.GetDamageMultiplier() - item.damageBonus);
        }
    }

    public int GetItemCount(Item item)
    {
        int count = 0;
        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty() && slot.item.id == item.id)
            {
                count += slot.quantity;
            }
        }
        return count;
    }

    public bool HasItem(Item item, int quantity = 1)
    {
        return GetItemCount(item) >= quantity;
    }

    public List<InventorySlot> GetInventorySlots()
    {
        return new List<InventorySlot>(inventorySlots);
    }

    public Item GetEquippedItem(ItemType itemType, int charmSlot = 1)
    {
        switch (itemType)
        {
            case ItemType.OffhandWeapon: return equippedOffhandWeapon;
            case ItemType.Helmet: return equippedHelmet;
            case ItemType.Boots: return equippedBoots;
            case ItemType.ChestGuard: return equippedChestGuard;
            case ItemType.Charm: return charmSlot == 1 ? equippedCharm1 : equippedCharm2;
            default: return null;
        }
    }

    public Dictionary<ItemType, Item> GetAllEquippedItems()
    {
        Dictionary<ItemType, Item> equipped = new Dictionary<ItemType, Item>();

        if (equippedOffhandWeapon != null) equipped[ItemType.OffhandWeapon] = equippedOffhandWeapon;
        if (equippedHelmet != null) equipped[ItemType.Helmet] = equippedHelmet;
        if (equippedBoots != null) equipped[ItemType.Boots] = equippedBoots;
        if (equippedChestGuard != null) equipped[ItemType.ChestGuard] = equippedChestGuard;
        if (equippedCharm1 != null) equipped[ItemType.Charm] = equippedCharm1;

        return equipped;
    }

    public bool IsInventoryFull()
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.IsEmpty()) return false;
        }
        return true;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i] = new InventorySlot();
        }
    }
}
using System.Collections.Generic;
using System;
using UnityEngine;


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
            if (!inventorySlots[i].IsEmpty() && inventorySlots[i].item.itemID == item.itemID)
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
            if (!inventorySlots[i].IsEmpty() && inventorySlots[i].item.itemID == item.itemID)
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
            if (!slot.IsEmpty() && slot.item.itemID == item.itemID)
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

        if (equippedOffhandWeapon != null)
            equipped[ItemType.OffhandWeapon] = equippedOffhandWeapon;
        if (equippedHelmet != null)
            equipped[ItemType.Helmet] = equippedHelmet;
        if (equippedBoots != null)
            equipped[ItemType.Boots] = equippedBoots;
        if (equippedChestGuard != null)
            equipped[ItemType.ChestGuard] = equippedChestGuard;
        if (equippedCharm1 != null) 
            equipped[ItemType.Charm] = equippedCharm1;

        return equipped;
    }

    public bool IsInventoryFull()
    {
        foreach (InventorySlot slot in inventorySlots)
            if (slot.IsEmpty())
                return false;
        return true;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
            inventorySlots[i] = new InventorySlot();
    }
}
using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private List<Item> inventoryItems = new List<Item>();
    private Dictionary<ItemType, Item> equippedItems = new Dictionary<ItemType, Item>();
    private Item charm1;
    private Item charm2;
    private List<Perk> ownedPerks = new List<Perk>();
    [SerializeField] private int maxCapacity = 20;
    [SerializeField] private float currency = 1000f;

    public static PlayerInventory Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public bool AddItem(Item item)
    {
        if (inventoryItems.Count >= maxCapacity)
            return false;

        inventoryItems.Add(item);
        return true;
    }

    public bool AddPerk(Perk perk)
    {
        if (perk == null)
            return false;

        if (!ownedPerks.Contains(perk))
        {
            ownedPerks.Add(perk);
            return true;
        }
        return false;
    }

    public List<Perk> GetAllPerks()
    {
        return new List<Perk>(ownedPerks);
    }

    public List<Item> GetAllItems()
    {
        return new List<Item>(inventoryItems);
    }

    public int GetItemCount()
    {
        return inventoryItems.Count;
    }

    public int GetMaxCapacity()
    {
        return maxCapacity;
    }

    public bool EquipItem(Item item, ItemType slotType, int charmSlot = 0)
    {
        if (item == null || item.itemType != slotType)
            return false;

        if (inventoryItems.Contains(item))
        {
            Item currentEquipped = GetEquippedItem(slotType, charmSlot);
            inventoryItems.Remove(item);

            if (slotType == ItemType.Charm)
            {
                if (charmSlot == 1)
                {
                    if (charm1 != null)
                        inventoryItems.Add(charm1);
                    charm1 = item;
                }
                else if (charmSlot == 2)
                {
                    if (charm2 != null)
                        inventoryItems.Add(charm2);
                    charm2 = item;
                }
                else
                {
                    if (charm1 == null)
                        charm1 = item;
                    else if (charm2 == null)
                        charm2 = item;
                    else
                    {
                        inventoryItems.Add(charm1);
                        charm1 = item;
                    }
                }
            }
            else
            {
                if (currentEquipped != null)
                    inventoryItems.Add(currentEquipped);
                equippedItems[slotType] = item;
            }

            return true;
        }
        return false;
    }

    public bool UnequipItem(ItemType slotType, int charmSlot, int inventoryIndex)
    {
        if (inventoryItems.Count >= maxCapacity)
            return false;

        Item equippedItem = GetEquippedItem(slotType, charmSlot);
        if (equippedItem != null)
        {
            if (slotType == ItemType.Charm)
            {
                if (charmSlot == 1)
                    charm1 = null;
                else if (charmSlot == 2)
                    charm2 = null;
            }
            else
                equippedItems[slotType] = null;

            if (inventoryIndex >= 0 && inventoryIndex < inventoryItems.Count)
                inventoryItems[inventoryIndex] = equippedItem;
            else
                inventoryItems.Add(equippedItem);

            return true;
        }
        return false;
    }

    public Item GetEquippedItem(ItemType slotType, int charmSlot = 0)
    {
        if (slotType == ItemType.Charm)
        {
            if (charmSlot == 1)
                return charm1;
            if (charmSlot == 2)
                return charm2;
            return charm1 ?? charm2;
        }

        equippedItems.TryGetValue(slotType, out Item item);
        return item;
    }

    public void SwapInventoryItems(int index1, int index2)
    {
        if (index1 >= 0 && index1 < inventoryItems.Count && index2 >= 0 && index2 < inventoryItems.Count)
        {
            Item temp = inventoryItems[index1];
            inventoryItems[index1] = inventoryItems[index2];
            inventoryItems[index2] = temp;
        }
    }

    public Item GetItemAtSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < inventoryItems.Count)
            return inventoryItems[slotIndex];
        return null;
    }

    public bool SwapEquipmentItems(ItemType sourceType, int sourceCharmSlot, ItemType targetType, int targetCharmSlot)
    {
        if (sourceType == ItemType.Charm && targetType == ItemType.Charm)
        {
            Item temp = sourceCharmSlot == 1 ? charm1 : charm2;
            if (sourceCharmSlot == 1)
                charm1 = targetCharmSlot == 1 ? charm1 : charm2;
            else
                charm2 = targetCharmSlot == 1 ? charm1 : charm2;
            if (targetCharmSlot == 1)
                charm1 = temp;
            else
                charm2 = temp;
            return true;
        }
        else if (equippedItems.ContainsKey(sourceType) && equippedItems.ContainsKey(targetType))
        {
            Item temp = equippedItems[sourceType];
            equippedItems[sourceType] = equippedItems[targetType];
            equippedItems[targetType] = temp;
            return true;
        }
        return false;
    }

    public bool MergeItems(Item item1, Item item2, Item item3)
    {
        if (item1 == null || item2 == null || item3 == null)
            return false;

        if (!inventoryItems.Contains(item1) || !inventoryItems.Contains(item2) || !inventoryItems.Contains(item3))
            return false;

        if (item1.itemID != item2.itemID || item2.itemID != item3.itemID ||
            item1.rarity != item2.rarity || item2.rarity != item3.rarity)
            return false;

        if (item1.rarity == ItemRarity.Legendary)
            return false;

        Item upgradedItem = ScriptableObject.CreateInstance<Item>();
        upgradedItem.itemID = item1.itemID;
        upgradedItem.itemName = item1.itemName;
        upgradedItem.description = item1.description;
        upgradedItem.icon = item1.icon;
        upgradedItem.rarity = (ItemRarity)((int)item1.rarity + 1);
        upgradedItem.itemType = item1.itemType;
        upgradedItem.healthBonus = item1.healthBonus;
        upgradedItem.armorBonus = item1.armorBonus;
        upgradedItem.damageBonus = item1.damageBonus;

        inventoryItems.Remove(item1);
        inventoryItems.Remove(item2);
        inventoryItems.Remove(item3);
        AddItem(upgradedItem);

        return true;
    }

    public int GetFirstEmptySlot()
    {
        for (int i = 0; i < maxCapacity; i++)
        {
            if (i >= inventoryItems.Count || inventoryItems[i] == null)
                return i;
        }
        return -1;
    }

    public float GetCurrency()
    {
        return currency;
    }

    public bool SpendCurrency(float amount)
    {
        if (currency >= amount)
        {
            currency -= amount;
            return true;
        }
        return false;
    }

    public void AddCurrency(float amount)
    {
        currency += amount;
    }
}
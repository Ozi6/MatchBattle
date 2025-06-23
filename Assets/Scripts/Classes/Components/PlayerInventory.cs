using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private List<Item> inventoryItems = new List<Item>();
    private Dictionary<ItemType, Item> equippedItems = new Dictionary<ItemType, Item>();
    private Item charm1;
    private Item charm2;
    [SerializeField] private int maxCapacity = 20;

    void Awake()
    {
        equippedItems[ItemType.OffhandWeapon] = null;
        equippedItems[ItemType.Helmet] = null;
        equippedItems[ItemType.ChestGuard] = null;
        equippedItems[ItemType.Boots] = null;
        charm1 = null;
        charm2 = null;
    }

    public bool AddItem(Item item)
    {
        if (inventoryItems.Count >= maxCapacity)
            return false;

        inventoryItems.Add(item);
        return true;
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
                    // Default to first empty charm slot
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
            {
                equippedItems[slotType] = null;
            }

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
            return charm1 ?? charm2; // Return first non-null charm if no specific slot
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
}
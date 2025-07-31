using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[System.Serializable]
public class Character
{
    public string characterName;
    public int characterID;
    public RenderTexture characterRenderTexture;
    public bool isLocked;
    public float purchaseCost;
    public List<BlockType> blockTypes;
    public GameObject prefab;
}

public class PlayerInventory : MonoBehaviour
{
    private List<Item> inventoryItems = new List<Item>();
    private Dictionary<ItemType, Item> equippedItems = new Dictionary<ItemType, Item>();
    private Item charm1;
    private Item charm2;
    private List<Perk> ownedPerks = new List<Perk>();
    [SerializeField] private int maxCapacity = 20;
    [SerializeField] private float currency = 1000f;
    [SerializeField] private CharacterDatabase characterDatabase;
    private Character selectedCharacter;
    [SerializeField] private ItemDatabase itemDatabase;

    public static PlayerInventory Instance { get; private set; }

    public event Action<int> OnCharacterUnlocked;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadInventory();
            InitializeCharacters();
        }
        else
            Destroy(gameObject);
    }

    private void InitializeCharacters()
    {
        selectedCharacter = characterDatabase.characters.FirstOrDefault(c => !c.isLocked) ?? characterDatabase.characters[0];
        Debug.Log($"Selected character: {selectedCharacter?.characterName} (ID: {selectedCharacter?.characterID}, isLocked: {selectedCharacter?.isLocked})");
    }

    public bool AddItem(Item item)
    {
        if (inventoryItems.Count >= maxCapacity)
            return false;

        inventoryItems.Add(item);
        SaveInventory();
        return true;
    }

    public bool AddPerk(Perk perk)
    {
        if (perk == null)
            return false;

        if (!ownedPerks.Contains(perk))
        {
            ownedPerks.Add(perk);
            SaveInventory();
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

            SaveInventory();
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

            SaveInventory();
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
        upgradedItem.itemType = (ItemType)item1.itemType;
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
            SaveInventory();
            return true;
        }
        return false;
    }

    public void AddCurrency(float amount)
    {
        currency += amount;
        SaveInventory();
    }

    public List<Character> GetAvailableCharacters()
    {
        return new List<Character>(characterDatabase.characters);
    }

    public Character GetSelectedCharacter()
    {
        return selectedCharacter;
    }

    public void SelectCharacter(int characterID)
    {
        foreach (Character character in characterDatabase.characters)
        {
            if (character.characterID == characterID && !character.isLocked)
            {
                selectedCharacter = character;
                SaveInventory();
                return;
            }
        }
    }

    public bool UnlockCharacter(int characterID)
    {
        foreach (Character character in characterDatabase.characters)
        {
            if (character.characterID == characterID && character.isLocked)
            {
                if (SpendCurrency(character.purchaseCost))
                {
                    character.isLocked = false;
                    SaveInventory();
                    OnCharacterUnlocked?.Invoke(characterID);
                    Debug.Log($"Character {character.characterName} (ID: {characterID}) unlocked");
                    return true;
                }
                Debug.Log($"Failed to unlock character {character.characterName} (ID: {characterID}): Insufficient currency");
                return false;
            }
        }
        Debug.Log($"Failed to unlock character ID {characterID}: Already unlocked or not found");
        return false;
    }

    public List<BlockType> GetSelectedCharacterBlockTypes()
    {
        return selectedCharacter != null ? selectedCharacter.blockTypes : new List<BlockType>();
    }

    public GameObject GetSelectedCharacterPrefab()
    {
        return selectedCharacter != null ? selectedCharacter.prefab : null;
    }

    public void SaveInventory()
    {
        PlayerPrefs.SetFloat("PlayerCurrency", currency);

        PlayerPrefs.SetInt("SelectedCharacterID", selectedCharacter?.characterID ?? -1);

        string unlockedCharacters = string.Join(",", characterDatabase.characters
            .Select(c => c.characterID + ":" + (c.isLocked ? "0" : "1")));
        PlayerPrefs.SetString("UnlockedCharacters", unlockedCharacters);

        string itemsData = string.Join(",", inventoryItems
            .Select(item => $"{item.itemID}:{(int)item.rarity}:{(int)item.itemType}"));
        PlayerPrefs.SetString("InventoryItems", itemsData);

        string equippedData = string.Join(",", equippedItems
            .Select(kvp => $"{(int)kvp.Key}:{kvp.Value?.itemID ?? -1}"));
        PlayerPrefs.SetString("EquippedItems", equippedData);

        PlayerPrefs.SetInt("Charm1", charm1?.itemID ?? -1);
        PlayerPrefs.SetInt("Charm2", charm2?.itemID ?? -1);

        string perksData = string.Join(",", ownedPerks
            .Select(perk => perk.perkName));
        PlayerPrefs.SetString("OwnedPerks", perksData);

        PlayerPrefs.Save();
    }

    public void LoadInventory()
    {
        currency = PlayerPrefs.GetFloat("PlayerCurrency", 1000f);

        int selectedCharacterID = PlayerPrefs.GetInt("SelectedCharacterID", -1);
        selectedCharacter = characterDatabase.characters.FirstOrDefault(c => c.characterID == selectedCharacterID) ?? characterDatabase.characters[0];

        string unlockedCharacters = PlayerPrefs.GetString("UnlockedCharacters", "");
        if (!string.IsNullOrEmpty(unlockedCharacters))
        {
            foreach (string charData in unlockedCharacters.Split(','))
            {
                string[] parts = charData.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int charID) && int.TryParse(parts[1], out int isUnlocked))
                {
                    Character character = characterDatabase.characters.FirstOrDefault(c => c.characterID == charID);
                    if (character != null)
                    {
                        character.isLocked = isUnlocked == 0;
                        Debug.Log($"Loaded character {character.characterName} (ID: {charID}, isLocked: {character.isLocked})");
                    }
                }
            }
        }
        else
        {
            Debug.Log("No UnlockedCharacters data found in PlayerPrefs. Using CharacterDatabase defaults.");
        }

        inventoryItems.Clear();
        string itemsData = PlayerPrefs.GetString("InventoryItems", "");
        if (!string.IsNullOrEmpty(itemsData))
        {
            foreach (string itemData in itemsData.Split(','))
            {
                string[] parts = itemData.Split(':');
                if (parts.Length == 3 && int.TryParse(parts[0], out int itemID) &&
                    int.TryParse(parts[1], out int rarity) && int.TryParse(parts[2], out int itemType))
                {
                    Item item = itemDatabase.GetItemByID(itemID);
                    if (item != null)
                    {
                        Item itemInstance = ScriptableObject.CreateInstance<Item>();
                        itemInstance.itemID = item.itemID;
                        itemInstance.itemName = item.itemName;
                        itemInstance.description = item.description;
                        itemInstance.icon = item.icon;
                        itemInstance.rarity = (ItemRarity)rarity;
                        itemInstance.itemType = (ItemType)itemType;
                        itemInstance.healthBonus = item.healthBonus;
                        itemInstance.armorBonus = item.armorBonus;
                        itemInstance.damageBonus = item.damageBonus;
                        inventoryItems.Add(itemInstance);
                    }
                }
            }
        }

        equippedItems.Clear();
        string equippedData = PlayerPrefs.GetString("EquippedItems", "");
        if (!string.IsNullOrEmpty(equippedData))
        {
            foreach (string equipData in equippedData.Split(','))
            {
                string[] parts = equipData.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int slotType) && int.TryParse(parts[1], out int itemID))
                {
                    if (itemID != -1)
                    {
                        Item item = inventoryItems.FirstOrDefault(i => i.itemID == itemID);
                        if (item != null)
                            equippedItems[(ItemType)slotType] = item;
                    }
                }
            }
        }

        int charm1ID = PlayerPrefs.GetInt("Charm1", -1);
        int charm2ID = PlayerPrefs.GetInt("Charm2", -1);
        charm1 = charm1ID != -1 ? inventoryItems.FirstOrDefault(i => i.itemID == charm1ID) : null;
        charm2 = charm2ID != -1 ? inventoryItems.FirstOrDefault(i => i.itemID == charm2ID) : null;

        ownedPerks.Clear();
        string perksData = PlayerPrefs.GetString("OwnedPerks", "");
        if (!string.IsNullOrEmpty(perksData) && PerkManager.Instance != null)
        {
            foreach (string perkName in perksData.Split(','))
            {
                if (!string.IsNullOrEmpty(perkName.Trim()))
                {
                    var perkNodeData = PerkManager.Instance.perkNodeData.FirstOrDefault(p => p.perk != null && p.perk.perkName == perkName.Trim());
                    if (perkNodeData.perk != null)
                        ownedPerks.Add(perkNodeData.perk);
                }
            }
        }
    }

    public bool HasPerk(Perk perk)
    {
        return ownedPerks.Contains(perk);
    }
}
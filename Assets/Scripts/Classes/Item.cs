using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item")]
public class Item : ScriptableObject
{
    public int itemID;
    public string itemName;
    public string description;
    public Sprite icon;
    public ItemRarity rarity;
    public ItemType itemType;
    public float healthBonus;
    public float armorBonus;
    public float damageBonus;
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum ItemType
{
    OffhandWeapon,
    Helmet,
    Boots,
    ChestGuard,
    Charm
}
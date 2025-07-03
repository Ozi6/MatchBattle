using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Items")]
    [SerializeField] private List<ShopItem> availableItems = new List<ShopItem>();
    [SerializeField] private List<Item> itemDatabase = new List<Item>();

    [Header("Shop Settings")]
    [SerializeField] private int maxShopItems = 12;
    [SerializeField] private bool refreshDaily = true;

    public static ShopManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GenerateShopItems();
        }
        else
            Destroy(gameObject);
    }

    void GenerateShopItems()
    {
        availableItems.Clear();

        for (int i = 0; i < maxShopItems && i < itemDatabase.Count; i++)
        {
            Item randomItem = itemDatabase[Random.Range(0, itemDatabase.Count)];
            float price = CalculateItemPrice(randomItem);
            int stock = Random.Range(1, 6);

            ShopItem shopItem = new ShopItem(randomItem, price, stock);
            availableItems.Add(shopItem);
        }
    }

    float CalculateItemPrice(Item item)
    {
        float basePrice = 50;

        switch (item.rarity)
        {
            case ItemRarity.Common:
                basePrice = 50;
                break;
            case ItemRarity.Uncommon:
                basePrice = 100;
                break;
            case ItemRarity.Rare:
                basePrice = 200;
                break;
            case ItemRarity.Epic:
                basePrice = 400;
                break;
            case ItemRarity.Legendary:
                basePrice = 800;
                break;
        }

        basePrice += item.healthBonus * 5;
        basePrice += item.armorBonus * 10;
        basePrice += item.damageBonus * 15;

        return basePrice;
    }

    public List<ShopItem> GetAvailableItems()
    {
        return new List<ShopItem>(availableItems);
    }

    public void RefreshShop()
    {
        GenerateShopItems();
    }

    public void AddItemToShop(Item item, int price, int stock = 0)
    {
        ShopItem shopItem = new ShopItem(item, price, stock);
        availableItems.Add(shopItem);
    }

    public void RemoveItemFromShop(ShopItem shopItem)
    {
        availableItems.Remove(shopItem);
    }

    public bool RefreshesDaily()
    {
        return refreshDaily;
    }
}
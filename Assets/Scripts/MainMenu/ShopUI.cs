using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform itemGrid;
    [SerializeField] private Transform characterGrid;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private GameObject shopCharacterPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Settings")]
    [SerializeField] private bool showRefreshButton = true;

    private List<GameObject> spawnedItemSlots = new List<GameObject>();
    private List<GameObject> spawnedCharacterSlots = new List<GameObject>();

    void Start()
    {
        if (refreshButton != null)
        {
            refreshButton.gameObject.SetActive(showRefreshButton && ShopManager.Instance.RefreshesDaily());
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }
        UpdateCurrencyDisplay();
        PopulateShop();
    }

    void PopulateShop()
    {
        foreach (GameObject slot in spawnedItemSlots)
            Destroy(slot);
        foreach (GameObject slot in spawnedCharacterSlots)
            Destroy(slot);
        spawnedItemSlots.Clear();
        spawnedCharacterSlots.Clear();

        List<ShopItem> items = ShopManager.Instance.GetAvailableItems();
        foreach (ShopItem shopItem in items)
        {
            if (shopItem.item != null)
            {
                GameObject slot = Instantiate(shopItemPrefab, itemGrid);
                spawnedItemSlots.Add(slot);
                ShopItemUI itemUI = slot.GetComponent<ShopItemUI>();
                itemUI.Setup(shopItem);
            }
        }

        List<ShopCharacter> characters = ShopManager.Instance.GetAvailableCharacters();
        foreach (ShopCharacter shopCharacter in characters)
        {
            if (!shopCharacter.character.isLocked)
                continue;

            GameObject slot = Instantiate(shopCharacterPrefab, characterGrid);
            spawnedCharacterSlots.Add(slot);
            ShopCharacterUI characterUI = slot.GetComponent<ShopCharacterUI>();
            characterUI.Setup(shopCharacter);
        }
    }

    void UpdateCurrencyDisplay()
    {
        currencyText.text = $"{PlayerInventory.Instance.GetCurrency():F0}";
    }

    void OnRefreshButtonClicked()
    {
        ShopManager.Instance.RefreshShop();
        PopulateShop();
    }

    public void OnItemPurchased()
    {
        UpdateCurrencyDisplay();
        PopulateShop();
    }
}
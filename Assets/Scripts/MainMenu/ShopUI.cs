using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform itemGrid;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Settings")]
    [SerializeField] private bool showRefreshButton = true;

    private List<GameObject> spawnedItemSlots = new List<GameObject>();

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
        spawnedItemSlots.Clear();
        List<ShopItem> items = ShopManager.Instance.GetAvailableItems();
        foreach (ShopItem shopItem in items)
        {
            GameObject slot = Instantiate(shopItemPrefab, itemGrid);
            spawnedItemSlots.Add(slot);
            ShopItemUI itemUI = slot.GetComponent<ShopItemUI>();
            itemUI.Setup(shopItem);
        }
    }

    void UpdateCurrencyDisplay()
    {
        currencyText.text = $"Currency: {PlayerInventory.Instance.GetCurrency():F0}";
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
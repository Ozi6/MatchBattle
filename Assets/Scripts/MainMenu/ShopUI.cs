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
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Settings")]
    [SerializeField] private bool showRefreshButton = true;

    private List<GameObject> spawnedItemSlots = new List<GameObject>();

    void Start()
    {
        if(refreshButton != null)
        {
            refreshButton.gameObject.SetActive(showRefreshButton && ShopManager.Instance.RefreshesDaily());
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }
        closeButton.onClick.AddListener(OnCloseButtonClicked);
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

    void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OnItemPurchased()
    {
        UpdateCurrencyDisplay();
        PopulateShop();
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI stockText;
    [SerializeField] private Button buyButton;

    private ShopItem shopItem;

    public void Setup(ShopItem item)
    {
        shopItem = item;
        itemIcon.sprite = item.item.icon;
        itemNameText.text = item.item.name;
        priceText.text = $"${item.price:F0}";
        stockText.text = $"Stock: {item.stock}";
        buyButton.interactable = item.isAvailable && item.stock > 0 && PlayerInventory.Instance.GetItemCount() < PlayerInventory.Instance.GetMaxCapacity();
        buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    void OnBuyButtonClicked()
    {
        if (PlayerInventory.Instance.GetCurrency() >= shopItem.price && shopItem.stock > 0 && PlayerInventory.Instance.AddItem(shopItem.item))
        {
            PlayerInventory.Instance.SpendCurrency(shopItem.price);
            shopItem.stock--;
            if (shopItem.stock <= 0)
            {
                shopItem.isAvailable = false;
                ShopManager.Instance.RemoveItemFromShop(shopItem);
            }
            FindAnyObjectByType<ShopUI>().OnItemPurchased();
        }
        else
            Debug.Log("Purchase failed: Insufficient currency or inventory full.");
    }
}
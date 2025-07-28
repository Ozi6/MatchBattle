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
    private ShopCharacter shopCharacter;

    public void Setup(ShopItem item)
    {
        shopItem = item;
        shopCharacter = null;
        itemIcon.sprite = item.item.icon;
        itemNameText.text = item.item.name;
        priceText.text = $"${item.price:F0}";
        stockText.text = $"Stock: {item.stock}";
        buyButton.interactable = item.isAvailable && item.stock > 0 && PlayerInventory.Instance.GetItemCount() < PlayerInventory.Instance.GetMaxCapacity();
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyItemClicked);
    }

    public void Setup(ShopCharacter character)
    {
        shopCharacter = character;
        shopItem = null;
        itemIcon.sprite = GetCharacterSprite(character.character);
        itemNameText.text = character.character.characterName;
        priceText.text = $"${character.price:F0}";
        stockText.text = character.character.isLocked ? "Locked" : "Unlocked";
        buyButton.interactable = character.character.isLocked && PlayerInventory.Instance.GetCurrency() >= character.price;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyCharacterClicked);
    }

    private Sprite GetCharacterSprite(Character character)
    {
        return character.prefab?.GetComponent<SpriteRenderer>()?.sprite;
    }

    void OnBuyItemClicked()
    {
        if (shopItem == null) return;

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
    }

    void OnBuyCharacterClicked()
    {
        if (shopCharacter == null) return;

        if (PlayerInventory.Instance.GetCurrency() >= shopCharacter.price && PlayerInventory.Instance.UnlockCharacter(shopCharacter.character.characterID))
        {
            shopCharacter.character.isLocked = false;
            ShopManager.Instance.RemoveCharacterFromShop(shopCharacter);
            FindAnyObjectByType<ShopUI>().OnItemPurchased();
        }
    }
}
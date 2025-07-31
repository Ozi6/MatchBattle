using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopCharacterUI : MonoBehaviour
{
    [SerializeField] private RawImage characterPreview;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button buyButton;

    private ShopCharacter shopCharacter;

    public void Setup(ShopCharacter character)
    {
        shopCharacter = character;
        characterPreview.texture = character.character.characterRenderTexture;
        characterNameText.text = character.character.characterName;
        if(priceText != null)
            priceText.text = $"${character.price:F0}";
        if(statusText != null)
            statusText.text = character.character.isLocked ? "Locked" : "Unlocked";
        buyButton.interactable = character.character.isLocked && PlayerInventory.Instance.GetCurrency() >= character.price;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyCharacterClicked);
    }

    void OnBuyCharacterClicked()
    {
        if (shopCharacter == null)
            return;

        if (PlayerInventory.Instance.GetCurrency() >= shopCharacter.price && PlayerInventory.Instance.UnlockCharacter(shopCharacter.character.characterID))
        {
            shopCharacter.character.isLocked = false;
            ShopManager.Instance.RemoveCharacterFromShop(shopCharacter);
            FindAnyObjectByType<ShopUI>().OnItemPurchased();
        }
    }
}
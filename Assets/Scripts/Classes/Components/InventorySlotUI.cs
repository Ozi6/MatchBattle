using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Components")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Text itemName;
    [SerializeField] private Text itemQuantity;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Button slotButton;
    [SerializeField] private GameObject emptySlotIndicator;
    [SerializeField] private GameObject newItemGlow;
    [SerializeField] private Image selectionBorder; // New selection border for merging

    [Header("Visual Settings")]
    [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color filledSlotColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    [SerializeField] private Color hoverColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color selectedColor = new Color(0f, 1f, 0f, 0.5f); // Green for selected items

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.gray;
    [SerializeField] private Color uncommonColor = Color.green;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;

    private Item currentItem;
    private int slotIndex;
    private ItemType? slotType;
    private Action<Item, int, ItemType?> onSlotClicked;
    private bool isHighlighted = false;
    private bool isSelected = false;
    private static InventorySlotUI draggedSlot;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    void Awake()
    {
        if (slotButton != null)
            slotButton.onClick.AddListener(OnSlotClicked);

        if (newItemGlow != null)
            newItemGlow.SetActive(false);

        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(false);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetupSlot(Item item, int index, ItemType? type, Action<Item, int, ItemType?> clickCallback)
    {
        currentItem = item;
        slotIndex = index;
        slotType = type;
        onSlotClicked = clickCallback;

        UpdateSlotDisplay();
    }

    void UpdateSlotDisplay()
    {
        bool hasItem = currentItem != null;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(hasItem);
            if (hasItem && currentItem.icon != null)
            {
                itemIcon.sprite = currentItem.icon;
                Color iconColor = itemIcon.color;
                iconColor.a = 1f;
                itemIcon.color = iconColor;
            }
        }

        if (itemName != null)
        {
            itemName.gameObject.SetActive(hasItem);
            if (hasItem)
                itemName.text = currentItem.itemName;
        }

        if (itemQuantity != null)
            itemQuantity.gameObject.SetActive(false);

        if (slotBackground != null)
            slotBackground.color = hasItem ? filledSlotColor : emptySlotColor;

        if (rarityBorder != null)
        {
            rarityBorder.gameObject.SetActive(hasItem);
            if (hasItem)
                rarityBorder.color = GetRarityColor(currentItem.rarity);
        }

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(!hasItem);

        if (slotButton != null)
            slotButton.interactable = hasItem;

        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(hasItem && isSelected);
    }

    Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return commonColor;
            case ItemRarity.Uncommon:
                return uncommonColor;
            case ItemRarity.Rare:
                return rareColor;
            case ItemRarity.Epic:
                return epicColor;
            case ItemRarity.Legendary:
                return legendaryColor;
            default:
                return Color.white;
        }
    }

    void OnSlotClicked()
    {
        onSlotClicked?.Invoke(currentItem, slotIndex, slotType);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(isSelected && currentItem != null);
            if (isSelected)
                selectionBorder.color = selectedColor;
        }
        UpdateSlotDisplay();
    }

    public void HighlightAsNew(float duration)
    {
        if (currentItem != null)
            StartCoroutine(HighlightCoroutine(duration));
    }

    IEnumerator HighlightCoroutine(float duration)
    {
        if (newItemGlow != null)
        {
            newItemGlow.SetActive(true);
            isHighlighted = true;

            Image glowImage = newItemGlow.GetComponent<Image>();
            if (glowImage != null)
            {
                Color originalColor = glowImage.color;
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    elapsedTime += Time.unscaledDeltaTime;
                    float alpha = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(elapsedTime * 4f) + 1f) / 2f);
                    glowImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    yield return null;
                }

                glowImage.color = originalColor;
            }

            newItemGlow.SetActive(false);
            isHighlighted = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotBackground != null && !isHighlighted && !isSelected)
            slotBackground.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotBackground != null && !isHighlighted && !isSelected)
            slotBackground.color = currentItem != null ? filledSlotColor : emptySlotColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null)
            return;

        draggedSlot = this;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        originalPosition = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedSlot == this)
            transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedSlot == this)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            transform.position = originalPosition;
            draggedSlot = null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot == null || draggedSlot == this)
            return;

        if (CanEquipItem(draggedSlot.currentItem))
        {
            InventoryDisplay inventoryDisplay = FindAnyObjectByType<InventoryDisplay>();
            if (inventoryDisplay != null)
                inventoryDisplay.SwapSlots(draggedSlot, this);
        }
    }

    bool CanEquipItem(Item item)
    {
        if (item == null)
            return true;

        if (slotType == null)
            return true;

        return item.itemType == slotType;
    }

    public Item GetItem()
    {
        return currentItem;
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    public ItemType? GetSlotType()
    {
        return slotType;
    }
}
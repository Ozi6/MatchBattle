using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class InventoryDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text itemCountText;
    [SerializeField] private Transform inventoryGrid;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private Button mergeButton;

    [Header("Equipment toughness")]
    [SerializeField] private Transform offhandWeaponSlot;
    [SerializeField] private Transform charmSlot1;
    [SerializeField] private Transform charmSlot2;
    [SerializeField] private Transform helmetSlot;
    [SerializeField] private Transform chestGuardSlot;
    [SerializeField] private Transform bootsSlot;

    [Header("New Item Highlight")]
    [SerializeField] private GameObject newItemIndicator;
    [SerializeField] private Color newItemGlowColor = Color.yellow;
    [SerializeField] private float highlightDuration = 2f;

    [Header("Settings")]
    [SerializeField] private float showAnimationDuration = 0.5f;
    [SerializeField] private AudioClip inventoryOpenSound;
    [SerializeField] private AudioClip inventoryCloseSound;

    private List<InventorySlotUI> inventorySlots = new List<InventorySlotUI>();
    private Dictionary<ItemType, InventorySlotUI> equipmentSlots = new Dictionary<ItemType, InventorySlotUI>();
    private InventorySlotUI charmSlot1UI;
    private InventorySlotUI charmSlot2UI;
    public PlayerInventory playerInventory;
    private AudioSource audioSource;
    private Item lastAddedItem;
    private List<InventorySlotUI> selectedSlots = new List<InventorySlotUI>();

    public Action OnInventoryDisplayClosed;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerInventory = PlayerInventory.Instance;

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventoryDisplay);

        if (mergeButton != null)
            mergeButton.onClick.AddListener(OnMergeButtonClicked);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        InitializeEquipmentSlots();
    }

    void InitializeEquipmentSlots()
    {
        if (offhandWeaponSlot != null)
            CreateEquipmentSlot(offhandWeaponSlot, ItemType.OffhandWeapon);
        if (charmSlot1 != null)
        {
            charmSlot1UI = CreateEquipmentSlot(charmSlot1, ItemType.Charm);
            equipmentSlots[ItemType.Charm] = charmSlot1UI;
        }
        if (charmSlot2 != null)
            charmSlot2UI = CreateEquipmentSlot(charmSlot2, ItemType.Charm);
        if (helmetSlot != null)
            CreateEquipmentSlot(helmetSlot, ItemType.Helmet);
        if (chestGuardSlot != null)
            CreateEquipmentSlot(chestGuardSlot, ItemType.ChestGuard);
        if (bootsSlot != null)
            CreateEquipmentSlot(bootsSlot, ItemType.Boots);
    }

    InventorySlotUI CreateEquipmentSlot(Transform parent, ItemType type)
    {
        GameObject slotObj = Instantiate(inventorySlotPrefab, parent);
        InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
        if (slotUI != null)
        {
            if (playerInventory == null)
            {
                Debug.LogError($"PlayerInventory is null when creating equipment slot for {type}. Ensure PlayerInventory exists in the scene.");
                slotUI.SetupSlot(null, -1, type, OnInventorySlotClicked);
            }
            else
            {
                Item equippedItem = playerInventory.GetEquippedItem(type, type == ItemType.Charm ? (parent == charmSlot1 ? 1 : 2) : 0);
                slotUI.SetupSlot(equippedItem, -1, type, OnInventorySlotClicked);
                if (type != ItemType.Charm || parent == charmSlot1)
                    equipmentSlots[type] = slotUI;
            }
        }
        return slotUI;
    }

    public void ShowInventory(Item newlyAddedItem = null)
    {
        lastAddedItem = newlyAddedItem;

        if (playerInventory == null)
        {
            Debug.LogWarning("PlayerInventory not found!");
            return;
        }

        SetupUI();
        PopulateInventoryGrid();
        RefreshEquipmentSlots();
        ShowScreen();

        if (newlyAddedItem != null)
            StartCoroutine(HighlightNewItem(newlyAddedItem));
    }

    void SetupUI()
    {
        if (titleText != null)
            titleText.text = "Your Inventory";

        UpdateItemCount();
        UpdateMergeButton();
    }

    void UpdateItemCount()
    {
        if (itemCountText != null && playerInventory != null)
        {
            int currentCount = playerInventory.GetItemCount();
            int maxCount = playerInventory.GetMaxCapacity();
            itemCountText.text = $"Items: {currentCount}/{maxCount}";
        }
    }

    void PopulateInventoryGrid()
    {
        ClearInventorySlots();

        if (playerInventory == null)
            return;

        int maxSlots = playerInventory.GetMaxCapacity();

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGrid);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                Item itemForSlot = playerInventory.GetItemAtSlot(i);
                slotUI.SetupSlot(itemForSlot, i, null, OnInventorySlotClicked);
                inventorySlots.Add(slotUI);
            }
        }
    }

    void ClearInventorySlots()
    {
        foreach (InventorySlotUI slot in inventorySlots)
            if (slot != null)
                Destroy(slot.gameObject);
        inventorySlots.Clear();
        selectedSlots.Clear();
        UpdateMergeButton();
    }

    void RefreshEquipmentSlots()
    {
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory is null in RefreshEquipmentSlots. Cannot refresh equipment slots.");
            return;
        }

        foreach (var pair in equipmentSlots)
        {
            Item equippedItem = playerInventory.GetEquippedItem(pair.Key, pair.Key == ItemType.Charm ? 1 : 0);
            pair.Value.SetupSlot(equippedItem, -1, pair.Key, OnInventorySlotClicked);
        }
        if (charmSlot2UI != null)
        {
            Item charm2 = playerInventory.GetEquippedItem(ItemType.Charm, 2);
            charmSlot2UI.SetupSlot(charm2, -1, ItemType.Charm, OnInventorySlotClicked);
        }
    }

    void OnInventorySlotClicked(Item item, int slotIndex, ItemType? slotType)
    {
        if (item != null)
        {
            Debug.Log($"Clicked on item: {item.itemName} in slot {slotIndex} (Type: {slotType?.ToString() ?? "Inventory"})");
            ShowItemTooltip(item);

            if (slotType == null)
            {
                InventorySlotUI clickedSlot = inventorySlots.Find(slot => slot.GetItem() == item && slot.GetSlotIndex() == slotIndex);
                if (clickedSlot != null)
                {
                    if (selectedSlots.Contains(clickedSlot))
                    {
                        selectedSlots.Remove(clickedSlot);
                        clickedSlot.SetSelected(false);
                    }
                    else if (selectedSlots.Count < 3)
                    {
                        selectedSlots.Add(clickedSlot);
                        clickedSlot.SetSelected(true);
                    }
                    UpdateMergeButton();
                }
            }
        }
    }

    void UpdateMergeButton()
    {
        if (mergeButton != null)
            mergeButton.interactable = selectedSlots.Count == 3 && CanMergeSelectedItems();
    }

    bool CanMergeSelectedItems()
    {
        if (selectedSlots.Count != 3)
            return false;

        Item item1 = selectedSlots[0].GetItem();
        Item item2 = selectedSlots[1].GetItem();
        Item item3 = selectedSlots[2].GetItem();

        return item1 != null && item2 != null && item3 != null &&
               item1.itemID == item2.itemID && item2.itemID == item3.itemID &&
               item1.rarity == item2.rarity && item2.rarity == item3.rarity &&
               item1.rarity != ItemRarity.Legendary;
    }

    void OnMergeButtonClicked()
    {
        if (selectedSlots.Count == 3 && CanMergeSelectedItems())
        {
            Item item1 = selectedSlots[0].GetItem();
            Item item2 = selectedSlots[1].GetItem();
            Item item3 = selectedSlots[2].GetItem();

            if (playerInventory.MergeItems(item1, item2, item3))
            {
                selectedSlots.Clear();
                RefreshDisplay();
            }
        }
    }

    void ShowItemTooltip(Item item)
    {
        Debug.Log($"Item: {item.itemName}\nDescription: {item.description}\nRarity: {item.rarity}");
    }

    IEnumerator HighlightNewItem(Item newItem)
    {
        InventorySlotUI targetSlot = null;
        foreach (InventorySlotUI slot in inventorySlots)
        {
            if (slot.GetItem() == newItem)
            {
                targetSlot = slot;
                break;
            }
        }

        if (targetSlot != null)
        {
            targetSlot.HighlightAsNew(highlightDuration);
            if (scrollView != null)
                StartCoroutine(ScrollToItem(targetSlot));
        }

        yield return new WaitForSeconds(highlightDuration);
    }

    IEnumerator ScrollToItem(InventorySlotUI targetSlot)
    {
        yield return new WaitForSeconds(0.1f);

        if (scrollView != null && targetSlot != null)
        {
            RectTransform content = scrollView.content;
            RectTransform viewport = scrollView.viewport;
            RectTransform target = targetSlot.GetComponent<RectTransform>();

            Vector2 contentPos = content.anchoredPosition;
            Vector2 targetPos = target.anchoredPosition;

            float targetY = -targetPos.y - (target.rect.height / 2);
            float viewportHeight = viewport.rect.height;
            float contentHeight = content.rect.height;

            float normalizedY = Mathf.Clamp01(targetY / (contentHeight - viewportHeight));
            scrollView.verticalNormalizedPosition = 1f - normalizedY;
        }
    }

    void ShowScreen()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            Time.timeScale = 0f;
            StartCoroutine(AnimateScreenShow());
        }

        if (audioSource != null && inventoryOpenSound != null)
            audioSource.PlayOneShot(inventoryOpenSound);
    }

    public void CloseInventoryDisplay()
    {
        StartCoroutine(AnimateScreenHide());
    }

    IEnumerator AnimateScreenShow()
    {
        if (inventoryPanel != null)
        {
            Vector3 originalScale = inventoryPanel.transform.localScale;
            inventoryPanel.transform.localScale = Vector3.zero;

            float elapsedTime = 0f;
            while (elapsedTime < showAnimationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / showAnimationDuration;
                inventoryPanel.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, progress);
                yield return null;
            }

            inventoryPanel.transform.localScale = originalScale;
        }
    }

    IEnumerator AnimateScreenHide()
    {
        if (inventoryPanel != null)
        {
            Vector3 originalScale = inventoryPanel.transform.localScale;

            float elapsedTime = 0f;
            while (elapsedTime < showAnimationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / showAnimationDuration;
                inventoryPanel.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
                yield return null;
            }

            inventoryPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        if (audioSource != null && inventoryCloseSound != null)
            audioSource.PlayOneShot(inventoryCloseSound);

        OnInventoryDisplayClosed?.Invoke();
    }

    public void SwapSlots(InventorySlotUI source, InventorySlotUI target)
    {
        Item sourceItem = source.GetItem();
        Item targetItem = target.GetItem();
        ItemType? sourceType = source.GetSlotType();
        ItemType? targetType = target.GetSlotType();
        int sourceIndex = source.GetSlotIndex();
        int targetIndex = target.GetSlotIndex();

        if (sourceType == null && targetType != null)
        {
            int charmSlotIndex = target == charmSlot1UI ? 1 : (target == charmSlot2UI ? 2 : 0);
            if (playerInventory.EquipItem(sourceItem, targetType.Value, charmSlotIndex))
                RefreshDisplay();
        }
        else if (sourceType != null && targetType == null)
        {
            int charmSlotIndex = source == charmSlot1UI ? 1 : (source == charmSlot2UI ? 2 : 0);
            if (playerInventory.UnequipItem(sourceType.Value, charmSlotIndex, targetIndex))
                RefreshDisplay();
        }
        else if (sourceType == null && targetType == null)
        {
            playerInventory.SwapInventoryItems(sourceIndex, targetIndex);
            RefreshDisplay();
        }
        else if (sourceType != null && targetType != null)
        {
            int sourceCharmSlot = source == charmSlot1UI ? 1 : (source == charmSlot2UI ? 2 : 0);
            int targetCharmSlot = target == charmSlot1UI ? 1 : (target == charmSlot2UI ? 2 : 0);
            if (playerInventory.SwapEquipmentItems(sourceType.Value, sourceCharmSlot, targetType.Value, targetCharmSlot))
                RefreshDisplay();
        }
    }

    public void RefreshDisplay()
    {
        if (inventoryPanel.activeInHierarchy)
        {
            UpdateItemCount();
            PopulateInventoryGrid();
            RefreshEquipmentSlots();
            UpdateMergeButton();
        }
    }
}
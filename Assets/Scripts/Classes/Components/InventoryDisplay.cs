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
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button mergePopupButton;

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
    private InventorySlotUI currentSelectedSlot;

    public Action OnInventoryDisplayClosed;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerInventory = PlayerInventory.Instance;

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventoryDisplay);

        if (mergeButton != null)
            mergeButton.onClick.AddListener(OnMergeButtonClicked);

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (levelUpButton != null)
            levelUpButton.onClick.AddListener(OnLevelUpClicked);
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);
        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnUnequipClicked);
        if (mergePopupButton != null)
            mergePopupButton.onClick.AddListener(OnMergePopupClicked);

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

            currentSelectedSlot = null;
            if (slotType == null)
                currentSelectedSlot = inventorySlots.Find(slot => slot.GetItem() == item && slot.GetSlotIndex() == slotIndex);
            else
            {
                foreach (var slot in equipmentSlots.Values)
                {
                    if (slot.GetItem() == item && slot.GetSlotType() == slotType)
                    {
                        currentSelectedSlot = slot;
                        break;
                    }
                }
                if (currentSelectedSlot == null && slotType == ItemType.Charm && charmSlot2UI?.GetItem() == item)
                {
                    currentSelectedSlot = charmSlot2UI;
                }
            }

            if (currentSelectedSlot != null)
            {
                ShowPopup(slotType, item);
            }
        }
    }

    void ShowPopup(ItemType? slotType, Item item)
    {
        if (popupPanel == null)
            return;

        popupPanel.SetActive(true);

        if (levelUpButton != null)
            levelUpButton.gameObject.SetActive(true);

        if (equipButton != null)
            equipButton.gameObject.SetActive(slotType == null);

        if (unequipButton != null)
            unequipButton.gameObject.SetActive(slotType != null && playerInventory.GetItemCount() < playerInventory.GetMaxCapacity());

        if (mergePopupButton != null)
            mergePopupButton.gameObject.SetActive(slotType == null && item.rarity != ItemRarity.Legendary);
    }

    void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    void OnLevelUpClicked()
    {
        if (currentSelectedSlot != null && currentSelectedSlot.GetItem() != null)
        {
            Debug.Log($"Leveling up item: {currentSelectedSlot.GetItem().itemName}");
            HidePopup();
        }
    }

    void OnEquipClicked()
    {
        if (currentSelectedSlot != null && currentSelectedSlot.GetItem() != null)
        {
            Item item = currentSelectedSlot.GetItem();
            ItemType itemType = item.itemType;
            int charmSlotIndex = itemType == ItemType.Charm ? (equipmentSlots[ItemType.Charm].GetItem() == null ? 1 : 2) : 0;

            if (playerInventory.EquipItem(item, itemType, charmSlotIndex))
            {
                Debug.Log($"Equipped item: {item.itemName}");
                RefreshDisplay();
            }
            HidePopup();
        }
    }

    void OnUnequipClicked()
    {
        if (currentSelectedSlot != null && currentSelectedSlot.GetItem() != null)
        {
            ItemType? slotType = currentSelectedSlot.GetSlotType();
            int charmSlotIndex = currentSelectedSlot == charmSlot1UI ? 1 : (currentSelectedSlot == charmSlot2UI ? 2 : 0);
            int targetInventorySlot = playerInventory.GetFirstEmptySlot();

            if (targetInventorySlot == -1)
            {
                Debug.LogWarning("Cannot unequip: Inventory is full.");
                return;
            }

            if (playerInventory.UnequipItem(slotType.Value, charmSlotIndex, targetInventorySlot))
            {
                Debug.Log($"Unequipped item: {currentSelectedSlot.GetItem().itemName}");
                RefreshDisplay();
            }
            HidePopup();
        }
    }

    void OnMergePopupClicked()
    {
        if (currentSelectedSlot != null && currentSelectedSlot.GetItem() != null)
        {
            InventorySlotUI clickedSlot = currentSelectedSlot;
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
            HidePopup();
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
            PauseCombatForInventory();

            StartCoroutine(AnimateScreenShow());
        }

        if (audioSource != null && inventoryOpenSound != null)
            audioSource.PlayOneShot(inventoryOpenSound);
    }

    private void PauseCombatForInventory()
    {
        CombatManager combatManager = FindAnyObjectByType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.PauseInput(true);
            Debug.Log("Combat paused for inventory");
        }

        CombineManager combineManager = FindAnyObjectByType<CombineManager>();
        if (combineManager != null)
        {
            combineManager.PauseInput(true);
            Debug.Log("CombineManager paused for inventory");
        }
    }

    public void CloseInventoryDisplay()
    {
        HidePopup();
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

        ResumeCombatAfterInventory();

        OnInventoryDisplayClosed?.Invoke();
    }

    private void ResumeCombatAfterInventory()
    {
        CombatManager combatManager = FindAnyObjectByType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.PauseInput(false);
            combatManager.SetProcessingMatches(false);
            combatManager.SetIsInCombat(true);
            Debug.Log("Combat resumed after inventory closed");
        }

        CombineManager combineManager = FindAnyObjectByType<CombineManager>();
        if (combineManager != null)
        {
            combineManager.PauseInput(false);
            combineManager.SetProcessingMatches(false);
            Debug.Log("CombineManager resumed after inventory closed");
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
            HidePopup();
        }
    }
}
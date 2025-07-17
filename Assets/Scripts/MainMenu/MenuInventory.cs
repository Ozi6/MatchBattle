using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MenuInventory : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private ScrollRect inventoryScrollRect;
    [SerializeField] private GridLayoutGroup inventoryGrid;
    [SerializeField] private InventorySlotUI inventorySlotPrefab;
    [SerializeField] private InventorySlotUI weaponSlot;
    [SerializeField] private InventorySlotUI helmetSlot;
    [SerializeField] private InventorySlotUI bootsSlot;
    [SerializeField] private InventorySlotUI chestGuardSlot;
    [SerializeField] private InventorySlotUI charmSlot1;
    [SerializeField] private InventorySlotUI charmSlot2;
    [SerializeField] private GameObject itemDetailsPanel;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundOverlay;
    [SerializeField] private RawImage selectedCharacterDisplay;

    [Header("Audio")]
    [SerializeField] private AudioSource buttonClickSound;

    private PlayerInventory playerInventory;
    private InventorySlotUI selectedSlot;
    private List<InventorySlotUI> inventorySlots = new List<InventorySlotUI>();

    void Awake()
    {
        InitializeInventorySlots();
        SetupButtonListeners();
        playerInventory = PlayerInventory.Instance;
    }

    void OnEnable()
    {
        UpdateInventoryDisplay();
        playerInventory = PlayerInventory.Instance;
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(false);
        if (inventoryScrollRect != null)
            inventoryScrollRect.verticalNormalizedPosition = 1f;
        Character selectedCharacter = playerInventory?.GetSelectedCharacter();
        if (selectedCharacter != null && selectedCharacterDisplay != null && selectedCharacterDisplay.texture != selectedCharacter.characterRenderTexture)
            selectedCharacterDisplay.texture = selectedCharacter.characterRenderTexture;
    }

    private void InitializeInventorySlots()
    {
        if (inventoryGrid == null || inventorySlotPrefab == null)
        {
            Debug.LogError("Inventory Grid or Slot Prefab not assigned!");
            return;
        }

        foreach (Transform child in inventoryGrid.transform)
            Destroy(child.gameObject);

        inventorySlots.Clear();

        int maxCapacity = playerInventory != null ? playerInventory.GetMaxCapacity() : 20;
        for (int i = 0; i < maxCapacity; i++)
        {
            InventorySlotUI slot = Instantiate(inventorySlotPrefab, inventoryGrid.transform);
            slot.gameObject.name = $"InventorySlot_{i}";
            inventorySlots.Add(slot);
        }
    }

    private void SetupButtonListeners()
    {
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonClicked);

        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            int index = i;
            inventorySlots[i].SetupSlot(null, i, null, OnSlotClicked);
        }

        if (weaponSlot != null)
            weaponSlot.SetupSlot(null, 0, ItemType.OffhandWeapon, OnSlotClicked);

        if (helmetSlot != null)
            helmetSlot.SetupSlot(null, 0, ItemType.Helmet, OnSlotClicked);

        if (bootsSlot != null)
            bootsSlot.SetupSlot(null, 0, ItemType.Boots, OnSlotClicked);

        if (chestGuardSlot != null)
            chestGuardSlot.SetupSlot(null, 0, ItemType.ChestGuard, OnSlotClicked);

        if (charmSlot1 != null)
            charmSlot1.SetupSlot(null, 1, ItemType.Charm, OnSlotClicked);

        if (charmSlot2 != null)
            charmSlot2.SetupSlot(null, 2, ItemType.Charm, OnSlotClicked);
    }

    private void OnCloseButtonClicked()
    {
        PlayButtonSound();
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(false);
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }
    }

    private void OnSlotClicked(Item item, int slotIndex, ItemType? slotType)
    {
        PlayButtonSound();
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = FindSlotByIndexAndType(slotIndex, slotType);
        if (selectedSlot != null)
            selectedSlot.SetSelected(true);

        UpdateItemDetails(item);
    }

    private InventorySlotUI FindSlotByIndexAndType(int slotIndex, ItemType? slotType)
    {
        if (slotType.HasValue)
        {
            switch (slotType.Value)
            {
                case ItemType.OffhandWeapon:
                    return weaponSlot;
                case ItemType.Helmet:
                    return helmetSlot;
                case ItemType.Boots:
                    return bootsSlot;
                case ItemType.ChestGuard:
                    return chestGuardSlot;
                case ItemType.Charm:
                    return slotIndex == 1 ? charmSlot1 : charmSlot2;
            }
        }
        return inventorySlots[slotIndex];
    }

    private void OnEquipButtonClicked()
    {
        PlayButtonSound();
        if (selectedSlot != null && selectedSlot.GetItem() != null)
        {
            Item item = selectedSlot.GetItem();
            ItemType slotType = item.itemType;
            int charmSlot = slotType == ItemType.Charm ? (selectedSlot == charmSlot1 ? 1 : 2) : 0;

            if (PlayerInventory.Instance.EquipItem(item, slotType, charmSlot))
            {
                UpdateInventoryDisplay();
                if (itemDetailsPanel != null)
                    itemDetailsPanel.SetActive(false);
                if (backgroundOverlay != null)
                    backgroundOverlay.gameObject.SetActive(false);
                selectedSlot.SetSelected(false);
                selectedSlot = null;
            }
        }
    }

    private void OnUnequipButtonClicked()
    {
        PlayButtonSound();
        if (selectedSlot != null && selectedSlot.GetItem() != null)
        {
            ItemType slotType = selectedSlot.GetSlotType().Value;
            int charmSlot = slotType == ItemType.Charm ? (selectedSlot == charmSlot1 ? 1 : 2) : 0;
            int firstEmptySlot = playerInventory.GetFirstEmptySlot();

            if (playerInventory.UnequipItem(slotType, charmSlot, firstEmptySlot))
            {
                UpdateInventoryDisplay();
                if (itemDetailsPanel != null)
                    itemDetailsPanel.SetActive(false);
                if (backgroundOverlay != null)
                    backgroundOverlay.gameObject.SetActive(false);
                selectedSlot.SetSelected(false);
                selectedSlot = null;
            }
        }
    }

    private void UpdateInventoryDisplay()
    {
        if (PlayerInventory.Instance == null)
            return;
        List<Item> items = PlayerInventory.Instance.GetAllItems();
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            Item item = i < items.Count ? items[i] : null;
            inventorySlots[i].SetupSlot(item, i, null, OnSlotClicked);
        }

        UpdateEquippedSlots();
        UpdateCurrencyDisplay();
    }

    private void UpdateEquippedSlots()
    {
        playerInventory = PlayerInventory.Instance;
        if (weaponSlot != null)
            weaponSlot.SetupSlot(playerInventory.GetEquippedItem(ItemType.OffhandWeapon), 0, ItemType.OffhandWeapon, OnSlotClicked);

        if (helmetSlot != null)
            helmetSlot.SetupSlot(playerInventory.GetEquippedItem(ItemType.Helmet), 0, ItemType.Helmet, OnSlotClicked);

        if (bootsSlot != null)
            bootsSlot.SetupSlot(playerInventory.GetEquippedItem(ItemType.Boots), 0, ItemType.Boots, OnSlotClicked);

        if (chestGuardSlot != null)
            chestGuardSlot.SetupSlot(playerInventory.GetEquippedItem(ItemType.ChestGuard), 0, ItemType.ChestGuard, OnSlotClicked);

        if (charmSlot1 != null)
            charmSlot1.SetupSlot(playerInventory.GetEquippedItem(ItemType.Charm, 1), 1, ItemType.Charm, OnSlotClicked);

        if (charmSlot2 != null)
            charmSlot2.SetupSlot(playerInventory.GetEquippedItem(ItemType.Charm, 2), 2, ItemType.Charm, OnSlotClicked);
    }

    private void UpdateCurrencyDisplay()
    {
        if (currencyText != null)
            currencyText.text = $"{playerInventory.GetCurrency():F0}";
    }

    private void UpdateItemDetails(Item item)
    {
        if (itemDetailsPanel != null)
        {
            itemDetailsPanel.SetActive(item != null);
            if (backgroundOverlay != null)
                backgroundOverlay.gameObject.SetActive(item != null);

            if (item != null)
            {
                if (itemNameText != null)
                    itemNameText.text = item.itemName;

                if (itemDescriptionText != null)
                    itemDescriptionText.text = item.description;

                if (itemIconImage != null)
                    itemIconImage.sprite = item.icon;

                bool isEquippedSlot = selectedSlot == weaponSlot || selectedSlot == helmetSlot ||
                                     selectedSlot == bootsSlot || selectedSlot == chestGuardSlot ||
                                     selectedSlot == charmSlot1 || selectedSlot == charmSlot2;

                if (equipButton != null)
                    equipButton.gameObject.SetActive(!isEquippedSlot);

                if (unequipButton != null)
                    unequipButton.gameObject.SetActive(isEquippedSlot);
            }
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();
    }
}
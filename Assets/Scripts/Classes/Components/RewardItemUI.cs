using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Text itemName;
    [SerializeField] private Text itemDescription;
    [SerializeField] private Text itemStats;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject selectedIndicator;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.gray;
    [SerializeField] private Color uncommonColor = Color.green;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;

    private Item currentItem;
    private Action<Item> onItemClicked;

    void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnButtonClicked);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
    }

    public void SetupRewardItem(Item item, Action<Item> clickCallback)
    {
        currentItem = item;
        onItemClicked = clickCallback;

        UpdateItemDisplay();
    }

    void UpdateItemDisplay()
    {
        if (currentItem == null)
            return;

        if (itemIcon != null && currentItem.icon != null)
            itemIcon.sprite = currentItem.icon;

        if (itemName != null)
            itemName.text = currentItem.name;

        if (itemDescription != null)
            itemDescription.text = currentItem.description;

        if (itemStats != null)
            itemStats.text = GetStatsText();

        if (rarityBorder != null)
            rarityBorder.color = GetRarityColor(currentItem.rarity);
    }

    string GetStatsText()
    {
        List<string> stats = new List<string>();

        if (currentItem.healthBonus > 0)
            stats.Add($"+{currentItem.healthBonus} Health");

        if (currentItem.armorBonus > 0)
            stats.Add($"+{currentItem.armorBonus} Armor");

        if (currentItem.damageBonus > 0)
            stats.Add($"+{currentItem.damageBonus} Damage");

        return stats.Count > 0 ? string.Join("\n", stats) : "No stat bonuses";
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

    void OnButtonClicked()
    {
        Debug.Log($"Button clicked! currentItem: {(currentItem != null ? currentItem.name : "NULL")}, callback: {(onItemClicked != null ? "assigned" : "NULL")}");
        onItemClicked?.Invoke(currentItem);
    }

    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);

        if (selectButton != null)
            selectButton.interactable = !selected;
    }

    public Item GetItem()
    {
        return currentItem;
    }
}
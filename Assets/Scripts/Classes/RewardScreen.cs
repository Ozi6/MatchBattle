using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class RewardScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject rewardScreenPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Transform rewardItemsContainer;
    [SerializeField] private GameObject rewardItemPrefab;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button skipButton;

    [Header("Settings")]
    [SerializeField] private int maxRewardChoices = 3;
    [SerializeField] private float showAnimationDuration = 0.5f;
    [SerializeField] private AudioClip rewardShowSound;
    [SerializeField] private AudioClip itemSelectSound;

    private List<Item> availableRewards = new List<Item>();
    private List<RewardItemUI> rewardItemUIs = new List<RewardItemUI>();
    private PlayerInventory playerInventory;
    private AudioSource audioSource;
    private bool hasSelectedReward = false;
    private Item selectedReward;

    public Action<Item> OnRewardSelected;
    public Action OnRewardScreenClosed;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerInventory = FindAnyObjectByType<PlayerInventory>();

        if (continueButton != null)
            continueButton.onClick.AddListener(ConfirmSelection);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipReward);

        if (rewardScreenPanel != null)
            rewardScreenPanel.SetActive(false);
    }

    public void ShowRewardScreen(List<Item> rewards, string title = "Choose Your Reward!", string description = "Select one item to add to your inventory:")
    {
        if (rewards == null || rewards.Count == 0)
        {
            Debug.LogWarning("No rewards provided to show!");
            return;
        }

        availableRewards = new List<Item>(rewards);
        hasSelectedReward = false;
        selectedReward = null;

        if (availableRewards.Count > maxRewardChoices)
        {
            availableRewards = GetRandomRewards(availableRewards, maxRewardChoices);
        }

        SetupUI(title, description);
        CreateRewardItems();
        ShowScreen();
    }

    void SetupUI(string title, string description)
    {
        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (continueButton != null)
        {
            continueButton.interactable = false;
            continueButton.GetComponentInChildren<Text>().text = "Select a Reward";
        }
    }

    void CreateRewardItems()
    {
        ClearRewardItems();

        foreach (Item reward in availableRewards)
        {
            GameObject rewardObj = Instantiate(rewardItemPrefab, rewardItemsContainer);
            RewardItemUI rewardUI = rewardObj.GetComponent<RewardItemUI>();

            if (rewardUI != null)
            {
                rewardUI.SetupRewardItem(reward, OnRewardItemClicked);
                rewardItemUIs.Add(rewardUI);
            }
        }
    }

    void ClearRewardItems()
    {
        foreach (RewardItemUI rewardUI in rewardItemUIs)
        {
            if (rewardUI != null)
                Destroy(rewardUI.gameObject);
        }
        rewardItemUIs.Clear();
    }

    void OnRewardItemClicked(Item selectedItem)
    {
        if (hasSelectedReward)
            return;

        selectedReward = selectedItem;
        hasSelectedReward = true;

        foreach (RewardItemUI rewardUI in rewardItemUIs)
        {
            rewardUI.SetSelected(rewardUI.GetItem() == selectedItem);
        }

        if (continueButton != null)
        {
            continueButton.interactable = true;
            continueButton.GetComponentInChildren<Text>().text = "Confirm Selection";
        }

        if (audioSource != null && itemSelectSound != null)
            audioSource.PlayOneShot(itemSelectSound);

        Debug.Log($"Selected reward: {selectedItem.name}");
    }

    void ConfirmSelection()
    {
        if (selectedReward == null) return;

        if (playerInventory != null)
        {
            bool added = playerInventory.AddItem(selectedReward);
            if (added)
            {
                Debug.Log($"Added {selectedReward.name} to inventory!");
                OnRewardSelected?.Invoke(selectedReward);
            }
            else
            {
                Debug.LogWarning("Failed to add item to inventory - inventory might be full!");
            }
        }

        CloseRewardScreen();
    }

    void SkipReward()
    {
        Debug.Log("Player skipped reward selection");
        CloseRewardScreen();
    }

    void ShowScreen()
    {
        if (rewardScreenPanel != null)
        {
            rewardScreenPanel.SetActive(true);
            Time.timeScale = 0f;

            StartCoroutine(AnimateScreenShow());
        }

        if (audioSource != null && rewardShowSound != null)
            audioSource.PlayOneShot(rewardShowSound);
    }

    void CloseRewardScreen()
    {
        StartCoroutine(AnimateScreenHide());
    }

    IEnumerator AnimateScreenShow()
    {
        if (rewardScreenPanel != null)
        {
            Vector3 originalScale = rewardScreenPanel.transform.localScale;
            rewardScreenPanel.transform.localScale = Vector3.zero;

            float elapsedTime = 0f;
            while (elapsedTime < showAnimationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / showAnimationDuration;
                rewardScreenPanel.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, progress);
                yield return null;
            }

            rewardScreenPanel.transform.localScale = originalScale;
        }
    }

    IEnumerator AnimateScreenHide()
    {
        if (rewardScreenPanel != null)
        {
            Vector3 originalScale = rewardScreenPanel.transform.localScale;

            float elapsedTime = 0f;
            while (elapsedTime < showAnimationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / showAnimationDuration;
                rewardScreenPanel.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
                yield return null;
            }

            rewardScreenPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        OnRewardScreenClosed?.Invoke();
    }

    List<Item> GetRandomRewards(List<Item> allRewards, int count)
    {
        List<Item> shuffled = new List<Item>(allRewards);
        List<Item> result = new List<Item>();

        for (int i = 0; i < count && shuffled.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, shuffled.Count);
            result.Add(shuffled[randomIndex]);
            shuffled.RemoveAt(randomIndex);
        }

        return result;
    }

    public void SetMaxRewardChoices(int maxChoices)
    {
        maxRewardChoices = maxChoices;
    }
}

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
            case ItemRarity.Common: return commonColor;
            case ItemRarity.Uncommon: return uncommonColor;
            case ItemRarity.Rare: return rareColor;
            case ItemRarity.Epic: return epicColor;
            case ItemRarity.Legendary: return legendaryColor;
            default: return Color.white;
        }
    }

    void OnButtonClicked()
    {
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

public class RewardGenerator : MonoBehaviour
{
    [Header("Reward Pools")]
    [SerializeField] private List<Item> commonRewards = new List<Item>();
    [SerializeField] private List<Item> uncommonRewards = new List<Item>();
    [SerializeField] private List<Item> rareRewards = new List<Item>();
    [SerializeField] private List<Item> epicRewards = new List<Item>();
    [SerializeField] private List<Item> legendaryRewards = new List<Item>();

    [Header("Drop Rates (%)")]
    [SerializeField] private float commonDropRate = 50f;
    [SerializeField] private float uncommonDropRate = 30f;
    [SerializeField] private float rareDropRate = 15f;
    [SerializeField] private float epicDropRate = 4f;
    [SerializeField] private float legendaryDropRate = 1f;

    public List<Item> GenerateRewards(int count, bool guaranteeRarity = false, ItemRarity minRarity = ItemRarity.Common)
    {
        List<Item> rewards = new List<Item>();

        for (int i = 0; i < count; i++)
        {
            ItemRarity selectedRarity = guaranteeRarity && i == 0 ? minRarity : GetRandomRarity();
            Item reward = GetRandomItemOfRarity(selectedRarity);

            if (reward != null)
                rewards.Add(reward);
        }

        return rewards;
    }

    ItemRarity GetRandomRarity()
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        float cumulativeChance = 0f;

        cumulativeChance += legendaryDropRate;
        if (randomValue <= cumulativeChance) return ItemRarity.Legendary;

        cumulativeChance += epicDropRate;
        if (randomValue <= cumulativeChance) return ItemRarity.Epic;

        cumulativeChance += rareDropRate;
        if (randomValue <= cumulativeChance) return ItemRarity.Rare;

        cumulativeChance += uncommonDropRate;
        if (randomValue <= cumulativeChance) return ItemRarity.Uncommon;

        return ItemRarity.Common;
    }

    Item GetRandomItemOfRarity(ItemRarity rarity)
    {
        List<Item> pool = GetRewardPoolByRarity(rarity);

        if (pool.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, pool.Count);
        return pool[randomIndex];
    }

    List<Item> GetRewardPoolByRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return commonRewards;
            case ItemRarity.Uncommon: return uncommonRewards;
            case ItemRarity.Rare: return rareRewards;
            case ItemRarity.Epic: return epicRewards;
            case ItemRarity.Legendary: return legendaryRewards;
            default: return commonRewards;
        }
    }

    public void AddItemToPool(Item item, ItemRarity rarity)
    {
        List<Item> pool = GetRewardPoolByRarity(rarity);
        if (!pool.Contains(item))
            pool.Add(item);
    }
}

public class RewardTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RewardScreen rewardScreen;
    [SerializeField] private RewardGenerator rewardGenerator;

    void Start()
    {
        if (rewardScreen != null)
        {
            rewardScreen.OnRewardSelected += HandleRewardSelected;
            rewardScreen.OnRewardScreenClosed += HandleRewardScreenClosed;
        }
    }

    void OnDestroy()
    {
        if (rewardScreen != null)
        {
            rewardScreen.OnRewardSelected -= HandleRewardSelected;
            rewardScreen.OnRewardScreenClosed -= HandleRewardScreenClosed;
        }
    }

    public void TriggerLevelCompleteReward()
    {
        List<Item> rewards = rewardGenerator.GenerateRewards(3);
        rewardScreen.ShowRewardScreen(rewards, "Level Complete!", "Choose your reward for completing this level:");
    }

    public void TriggerBossDefeatReward()
    {
        List<Item> rewards = rewardGenerator.GenerateRewards(3, true, ItemRarity.Rare);
        rewardScreen.ShowRewardScreen(rewards, "Boss Defeated!", "Select a powerful reward for your victory:");
    }

    public void TriggerTreasureChestReward()
    {
        List<Item> rewards = rewardGenerator.GenerateRewards(2);
        rewardScreen.ShowRewardScreen(rewards, "Treasure Found!", "What's inside the chest?");
    }

    void HandleRewardSelected(Item selectedItem)
    {
        Debug.Log($"Player selected: {selectedItem.name}");
    }

    void HandleRewardScreenClosed()
    {
        Debug.Log("Reward screen closed");
    }
}
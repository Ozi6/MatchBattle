using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    private InventoryDisplay inventoryDisplay;
    private AudioSource audioSource;
    private bool hasSelectedReward = false;
    private Item selectedReward;

    public Action<Item> OnRewardSelected;
    public Action OnRewardScreenClosed;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerInventory = FindAnyObjectByType<PlayerInventory>();
        inventoryDisplay = FindAnyObjectByType<InventoryDisplay>();

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
            availableRewards = availableRewards.GetRange(0, maxRewardChoices);

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
        selectedReward = selectedItem;

        foreach (RewardItemUI rewardUI in rewardItemUIs)
            rewardUI.SetSelected(rewardUI.GetItem() == selectedItem);

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
        if (selectedReward == null)
            return;

        if (playerInventory != null)
        {
            bool added = playerInventory.AddItem(selectedReward);
            if (added)
            {
                Debug.Log($"Added {selectedReward.name} to inventory!");
                OnRewardSelected?.Invoke(selectedReward);
                if (inventoryDisplay != null)
                    inventoryDisplay.ShowInventory(selectedReward);
                else
                    CloseRewardScreen();
            }
            else
            {
                Debug.LogWarning("Failed to add item to inventory - inventory might be full!");
                CloseRewardScreen();
            }
        }
        else
        {
            CloseRewardScreen();
        }
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

        if (inventoryDisplay == null)
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
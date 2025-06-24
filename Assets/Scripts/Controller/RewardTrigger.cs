using System.Collections.Generic;
using UnityEngine;

public class RewardTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RewardScreen rewardScreen;
    [SerializeField] private RewardGenerator rewardGenerator;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private InventoryDisplay inventoryDisplay;
    public GameObject puzzleHalf;

    void Start()
    {
        if (rewardScreen != null)
            rewardScreen.OnRewardSelected += HandleRewardSelected;
        if (inventoryDisplay != null)
            inventoryDisplay.OnInventoryDisplayClosed += HandleInventoryDisplayClosed;
        else
        {
            inventoryDisplay = FindAnyObjectByType<InventoryDisplay>();
            if (inventoryDisplay != null)
                inventoryDisplay.OnInventoryDisplayClosed += HandleInventoryDisplayClosed;
            else
                Debug.LogWarning("InventoryDisplay not found in scene!");
        }
        if (levelManager == null)
            levelManager = LevelManager.Instance;
    }

    void OnDestroy()
    {
        if (rewardScreen != null)
            rewardScreen.OnRewardSelected -= HandleRewardSelected;
        if (inventoryDisplay != null)
            inventoryDisplay.OnInventoryDisplayClosed -= HandleInventoryDisplayClosed;
    }

    public void TriggerLevelCompleteReward()
    {
        LevelData currentLevel = levelManager?.GetCurrentLevel();
        List<Item> rewards = rewardGenerator.GenerateRewards(currentLevel);
        rewardScreen.ShowRewardScreen(rewards, "Level Complete!", "Choose your reward for completing this level:");
    }

    void HandleRewardSelected(Item selectedItem)
    {
        Debug.Log($"Player selected: {selectedItem.name}");
    }

    void HandleInventoryDisplayClosed()
    {
        Debug.Log("Inventory display closed");
        puzzleHalf.SetActive(true);
    }
}
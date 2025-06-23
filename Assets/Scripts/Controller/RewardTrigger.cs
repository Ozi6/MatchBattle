using System.Collections.Generic;
using UnityEngine;

public class RewardTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RewardScreen rewardScreen;
    [SerializeField] private RewardGenerator rewardGenerator;
    [SerializeField] private LevelManager levelManager;
    public GameObject puzzleHalf;

    void Start()
    {
        if (rewardScreen != null)
        {
            rewardScreen.OnRewardSelected += HandleRewardSelected;
            rewardScreen.OnRewardScreenClosed += HandleRewardScreenClosed;
        }
        if (levelManager == null)
            levelManager = LevelManager.Instance;
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
        LevelData currentLevel = levelManager?.GetCurrentLevel();
        List<Item> rewards = rewardGenerator.GenerateRewards(currentLevel);
        rewardScreen.ShowRewardScreen(rewards, "Level Complete!", "Choose your reward for completing this level:");
    }

    void HandleRewardSelected(Item selectedItem)
    {
        Debug.Log($"Player selected: {selectedItem.name}");
    }

    void HandleRewardScreenClosed()
    {
        Debug.Log("Reward screen closed");
        puzzleHalf.SetActive(true);
    }
}
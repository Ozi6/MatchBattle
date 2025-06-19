using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RewardGenerator : MonoBehaviour
{
    [Header("Reward Pools (Optional Fallback)")]
    [SerializeField] private List<Item> commonRewards = new List<Item>();
    [SerializeField] private List<Item> uncommonRewards = new List<Item>();
    [SerializeField] private List<Item> rareRewards = new List<Item>();
    [SerializeField] private List<Item> epicRewards = new List<Item>();
    [SerializeField] private List<Item> legendaryRewards = new List<Item>();

    public List<Item> GenerateRewards(LevelData levelData, int waveNumber = 0, bool isBossDefeat = false, bool isTreasureChest = false)
    {
        List<Item> rewards = new List<Item>();

        if (isBossDefeat)
            rewards.AddRange(GetBossRewards(levelData));
        else if (isTreasureChest)
            rewards.AddRange(GetTreasureChestRewards(levelData));
        else if (waveNumber > 0)
            rewards.AddRange(GetWaveRewards(levelData, waveNumber));
        else
            rewards.AddRange(GetLevelRewards(levelData));

        if (rewards.Count == 0)
        {
            Debug.LogWarning("No deterministic rewards defined, using fallback pool.");
            rewards.AddRange(GetFallbackRewards(3, ItemRarity.Common));
        }

        return rewards;
    }

    private Item[] GetLevelRewards(LevelData levelData)
    {
        if (levelData != null && levelData.levelCompletionRewards != null && levelData.levelCompletionRewards.rewards != null)
            return levelData.levelCompletionRewards.rewards;
        return new Item[0];
    }

    private Item[] GetWaveRewards(LevelData levelData, int waveNumber)
    {
        if (levelData != null && levelData.waveCompletionRewards != null)
        {
            WaveRewardData waveReward = levelData.waveCompletionRewards.FirstOrDefault(w => w.waveNumber == waveNumber);
            if (waveReward != null && waveReward.rewards != null)
                return waveReward.rewards;
        }
        return new Item[0];
    }

    private Item[] GetBossRewards(LevelData levelData)
    {
        if (rareRewards.Count > 0)
            return rareRewards.GetRange(0, Mathf.Min(3, rareRewards.Count)).ToArray();
        return new Item[0];
    }

    private Item[] GetTreasureChestRewards(LevelData levelData)
    {
        if (uncommonRewards.Count > 0)
            return uncommonRewards.GetRange(0, Mathf.Min(2, uncommonRewards.Count)).ToArray();
        return new Item[0];
    }

    private List<Item> GetFallbackRewards(int count, ItemRarity minRarity)
    {
        List<Item> rewards = new List<Item>();
        List<Item> pool = GetRewardPoolByRarity(minRarity);
        for (int i = 0; i < count && i < pool.Count; i++)
            rewards.Add(pool[i]);
        return rewards;
    }

    private List<Item> GetRewardPoolByRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return commonRewards;
            case ItemRarity.Uncommon:
                return uncommonRewards;
            case ItemRarity.Rare:
                return rareRewards;
            case ItemRarity.Epic:
                return epicRewards;
            case ItemRarity.Legendary:
                return legendaryRewards;
            default:
                return commonRewards;
        }
    }

    public void AddItemToPool(Item item, ItemRarity rarity)
    {
        List<Item> pool = GetRewardPoolByRarity(rarity);
        if (!pool.Contains(item))
            pool.Add(item);
    }
}
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

    public List<Item> GenerateRewards(LevelData levelData)
    {
        List<Item> rewards = new List<Item>();

        Item[] levelRewards = GetLevelRewards(levelData);
        rewards.AddRange(levelRewards);

        if (rewards.Count == 0)
        {
            Debug.LogWarning("no level-specific rewards found, using fallback rewards");
            rewards.AddRange(GetFallbackRewards(3, ItemRarity.Common));
        }

        if (rewards.Count == 0)
            Debug.LogError("no rewards available to generate");

        return rewards.Take(3).ToList();
    }

    private Item[] GetLevelRewards(LevelData levelData)
    {
        if (levelData != null && levelData.levelCompletionRewards != null && levelData.levelCompletionRewards.rewards != null)
            return levelData.levelCompletionRewards.rewards.Where(item => item != null).ToArray();
        return new Item[0];
    }

    private List<Item> GetFallbackRewards(int count, ItemRarity minRarity)
    {
        List<Item> rewards = new List<Item>();
        List<Item> pool = GetRewardPoolByRarity(minRarity);

        pool = pool.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < count && i < pool.Count; i++)
            if (pool[i] != null)
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
        if (item == null)
            return;
        List<Item> pool = GetRewardPoolByRarity(rarity);
        if (!pool.Contains(item))
            pool.Add(item);
    }
}
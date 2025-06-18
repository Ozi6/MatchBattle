using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Combat System/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName;
    public string description;
    public Sprite levelIcon;
    public int levelNumber;

    [Header("Level Requirements")]
    public int requiredPreviousLevel = -1;
    public bool isUnlocked = false;

    [Header("Combat Settings")]
    public int totalWaves = 5;
    public WaveData[] waves;
    public RewardData[] availableRewards;

    [Header("Level Modifiers")]
    public float globalDifficultyMultiplier = 1f;
    public float playerHealthModifier = 1f;
    public float playerDefenseModifier = 1f;

    [Header("Special Rules")]
    public BlockType[] allowedBlocks;
    public BlockType[] bannedBlocks;
    public bool hasTimeLimit = false;
    public float timeLimitSeconds = 300f;
}
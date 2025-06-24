using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

[System.Serializable]
public class CombatAction
{
    public BlockType blockType;
    public float baseDamage;
    public float baseDefense;
    public bool isDefensive;
    public string actionName;

    [Header("Scaling")]
    public float comboScaling = 1.2f;
    public int maxComboBonus = 5;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 5f;
    public DebuffType debuffType = DebuffType.Bleed;
    public float debuffDuration = 0f;
    public float debuffIntensity = 0f;
    public bool isPiercing = false;
    public bool hasAreaEffect = false;
    public float areaRadius = 1f;
    public float baseKnockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    [Header("Specialized Projectile Behavior")]
    public ProjectileType projectileType = ProjectileType.Direct;
    public int baseProjectileCount = 1;
    public int projectileCountPerCombo = 1;
    public bool usesArcTrajectory = false;
    public float arcHeight = 2f;
    public float explosionDelay = 2f;
    public bool explodesOnTimer = false;
    public bool explodesOnContact = true;
    public bool isHoming = false;
    public float homingStrength = 2f;
    public float homingRange = 3f;
}

public enum ProjectileType
{
    Direct,
    Bow,
    Bomb,
    Magic,
    Axe
}

[Serializable]
public class WaveData
{
    [Header("Wave Settings")]
    public int waveNumber;
    public int totalEnemies = 10;
    public float spawnInterval = 2f;
    public GameObject[] enemyPrefabs;

    [Header("Wave Modifiers")]
    public float enemyHealthMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
}

[Serializable]
public class RewardData
{
    public string rewardName;
    public string description;
    public BlockType blockType;
    public float damageBonus;
    public float defenseBonus;
    public Sprite icon;
    public bool isUpgrade;
}

public class CombatManager : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private float playerHealth = 100f;
    [SerializeField] private float maxPlayerHealth = 100f;
    [SerializeField] private float playerDefense = 0f;

    [Header("Combat Actions")]
    [SerializeField] public CombatAction[] combatActions;

    [Header("Wave Management")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int totalWaves = 5;
    [SerializeField] private float waveClearDelay = 2f;
    [SerializeField] private WaveData[] waveDataArray;

    [Header("Rewards")]
    [SerializeField] private GameObject rewardSelectionUI;
    [SerializeField] private Transform rewardButtonContainer;
    [SerializeField] private GameObject rewardButtonPrefab;

    [Header("UI References")]
    [SerializeField] private Slider playerHealthBar;
    [SerializeField] private Text waveText;
    [SerializeField] private Text enemyCountText;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject victoryUI;

    [Header("Player Deck")]
    [SerializeField] private List<BlockType> playerDeck = new List<BlockType>();
    public Player player;

    [Header("Level Integration")]
    [SerializeField] private LevelData currentLevelData;
    private bool levelWasLoadedExternally = false;

    [Header("Reward System")]
    [SerializeField] private RewardScreen rewardScreen;
    [SerializeField] private RewardGenerator rewardGenerator;
    [Header("Reward Trigger")]
    [SerializeField] private RewardTrigger rewardTrigger;


    public GameObject puzzleHalf;
    public GameObject rewardScreenHalf;

    [HideInInspector]
    public Dictionary<BlockType, CombatAction> combatActionDict;
    private Dictionary<BlockType, float> blockUpgrades = new Dictionary<BlockType, float>();
    private List<Enemy> activeEnemies = new List<Enemy>();
    private CombineManager combineManager;
    private bool isInCombat = false;
    private WaveData currentWaveData;
    private int enemiesSpawned = 0;
    private int enemiesKilled = 0;
    private float lastSpawnTime;
    private float waveStartTime;
    private bool waveCompleted = false;

    public Action<int> OnWaveStarted;
    public Action<int> OnWaveCompleted;
    public Action OnLevelCompleted;
    public Action OnPlayerDeath;

    private Queue<(CombatAction action, ProjectileData projectileData, Enemy target, Vector3 spawnPosition, Vector2 direction, bool isArcProjectile)> projectileQueue = new Queue<(CombatAction, ProjectileData, Enemy, Vector3, Vector2, bool)>();
    private float lastProjectileLaunchTime = 0f;
    private const float PROJECTILE_LAUNCH_DELAY = 0.1f;

    void Awake()
    {
        combineManager = FindAnyObjectByType<CombineManager>();
        if (combineManager == null)
            return;

        if (rewardScreen != null)
        {
            rewardScreen.OnRewardSelected += HandleRewardSelected;
            rewardScreen.OnRewardScreenClosed += HandleRewardScreenClosed;
        }

        if (rewardTrigger == null)
            rewardTrigger = FindAnyObjectByType<RewardTrigger>();

        InitializeCombatActions();
        InitializeUI();
        InitializeBlockUpgrades();
    }

    void Start()
    {
        if (LevelManager.Instance != null)
        {
            LevelData selectedLevel = LevelManager.Instance.GetCurrentLevel();
            if (selectedLevel != null)
                InitializeWithLevel(selectedLevel);
            LevelManager.Instance.OnLevelSelected += InitializeWithLevel;
        }

        if (!levelWasLoadedExternally && currentLevelData == null)
        {
            Debug.LogWarning("No level data found! Using default settings.");
            if (LevelManager.Instance != null)
            {
                LevelData defaultLevel = LevelManager.Instance.GetLevel(0);
                if (defaultLevel != null)
                    InitializeWithLevel(defaultLevel);
            }
        }

        if (combineManager != null)
        {
            combineManager.OnComboExecuted += HandleComboExecuted;
            combineManager.OnBlocksMatched += HandleBlocksMatched;
            combineManager.OnCombatActionTriggered += HandlePuzzleMatch;
        }

        if (playerDeck.Count == 0)
        {
            playerDeck.AddRange(new BlockType[] {
            BlockType.Sword, BlockType.Shield, BlockType.Potion,
            BlockType.Bow, BlockType.Magic, BlockType.Axe
        });
        }

        if (currentLevelData != null)
            StartWave(currentWave);
    }

    public void InitializeWithLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("Cannot initialize CombatManager with null level data!");
            return;
        }

        currentLevelData = levelData;
        levelWasLoadedExternally = true;

        totalWaves = levelData.totalWaves;
        waveDataArray = levelData.waves;

        maxPlayerHealth *= levelData.playerHealthModifier;
        playerHealth = maxPlayerHealth;
        playerDefense *= levelData.playerDefenseModifier;

        InitializeUI();

        Debug.Log($"Initialized combat with level: {levelData.levelName}");
    }

    void Update()
    {
        if (isInCombat && !waveCompleted)
        {
            UpdateWaveProgress();
            HandleEnemySpawning();
        }
        ProcessProjectileQueue();
        UpdateUI();
    }

    void InitializeCombatActions()
    {
        combatActionDict = new Dictionary<BlockType, CombatAction>();
        foreach (CombatAction action in combatActions)
            combatActionDict[action.blockType] = action;
    }

    void InitializeUI()
    {
        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = maxPlayerHealth;
            playerHealthBar.value = playerHealth;
        }
    }

    void InitializeBlockUpgrades()
    {
        blockUpgrades = new Dictionary<BlockType, float>();
        foreach (BlockType blockType in System.Enum.GetValues(typeof(BlockType)))
        {
            if (blockType != BlockType.Empty)
                blockUpgrades[blockType] = 0f;
        }
    }

    void UpdateWaveProgress()
    {
        if (currentWaveData == null)
            return;

        activeEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead());
    }

    public void HandlePuzzleMatch(BlockType blockType, int comboSize, List<PuzzleBlock> matchedBlocks)
    {
        if (!combatActionDict.ContainsKey(blockType))
            return;

        CombatAction action = combatActionDict[blockType];

        if (action.isDefensive)
            HandleDefensiveAction(action, comboSize);
        else
            HandleOffensiveAction(action, comboSize, matchedBlocks);
    }

    void HandleDefensiveAction(CombatAction action, int comboSize)
    {
        float actionValue = CalculateActionValue(action.baseDefense, comboSize, action.comboScaling, action.maxComboBonus);

        if (blockUpgrades.ContainsKey(action.blockType))
            actionValue *= (1f + blockUpgrades[action.blockType]);

        switch (action.blockType)
        {
            case BlockType.Shield:
                playerDefense += actionValue;
                Debug.Log($"Player gained {actionValue} defense! Total defense: {playerDefense}");
                break;

            case BlockType.Potion:
                HealPlayer(actionValue);
                Debug.Log($"Player healed for {actionValue} HP!");
                break;
        }
    }

    void HandleOffensiveAction(CombatAction action, int comboSize, List<PuzzleBlock> matchedBlocks)
    {
        float damage = CalculateActionValue(action.baseDamage, comboSize, action.comboScaling, action.maxComboBonus);

        if (blockUpgrades.ContainsKey(action.blockType))
            damage *= (1f + blockUpgrades[action.blockType]);

        ProjectileData projectileData = new ProjectileData
        {
            damage = damage,
            speed = action.projectileSpeed,
            lifetime = 5f,
            debuffType = action.debuffType,
            debuffDuration = action.debuffDuration,
            debuffIntensity = action.debuffIntensity,
            piercing = action.isPiercing,
            hasAreaEffect = action.hasAreaEffect,
            areaRadius = action.areaRadius,
            hasKnockback = action.baseKnockbackForce > 0f,
            knockbackForce = action.baseKnockbackForce,
            knockbackDuration = action.knockbackDuration,
            usesArc = action.usesArcTrajectory,
            arcHeight = action.arcHeight,
            explodesOnContact = action.explodesOnContact,
            explosionTimer = action.explosionDelay,
            isHoming = action.isHoming,
            homingStrength = action.homingStrength,
            homingRange = action.homingRange
        };

        switch (action.projectileType)
        {
            case ProjectileType.Bow:
                LaunchBowProjectiles(action, projectileData, comboSize);
                break;
            case ProjectileType.Bomb:
                LaunchBombProjectiles(action, projectileData, comboSize);
                break;
            default:
                LaunchStandardProjectiles(action, projectileData, comboSize);
                break;
        }
    }

    void LaunchBowProjectiles(CombatAction action, ProjectileData projectileData, int comboSize)
    {
        if (activeEnemies.Count == 0)
            return;

        int extraCombos = Mathf.Max(0, comboSize - 3);
        int projectileCount = action.baseProjectileCount + (extraCombos * action.projectileCountPerCombo);

        Vector3 spawnPosition = playerTransform.position + Vector3.up * 0.5f;

        float totalSpreadAngle = Mathf.Min(60f, projectileCount * 10f);
        float angleStep = projectileCount > 1 ? totalSpreadAngle / (projectileCount - 1) : 0f;
        float startAngle = -totalSpreadAngle / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            Enemy targetEnemy = GetNextTarget(i % activeEnemies.Count);
            if (targetEnemy != null)
            {
                Vector2 baseDirection = (targetEnemy.transform.position - spawnPosition).normalized;

                float currentAngle = startAngle + (i * angleStep);
                Vector2 direction = RotateVector2(baseDirection, currentAngle);

                projectileQueue.Enqueue((action, projectileData.Clone(), targetEnemy, spawnPosition, direction, action.usesArcTrajectory));
            }
        }
    }

    void LaunchBombProjectiles(CombatAction action, ProjectileData projectileData, int comboSize)
    {
        if (activeEnemies.Count == 0)
            return;

        int projectileCount = action.baseProjectileCount + Mathf.Max(0, comboSize - 3) * action.projectileCountPerCombo;

        Vector3 spawnPosition = playerTransform.position + Vector3.up * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            Enemy targetEnemy = GetNextTarget(i % activeEnemies.Count);
            if (targetEnemy != null)
            {
                ProjectileData bombData = projectileData.Clone();
                bombData.usesArc = action.usesArcTrajectory;
                bombData.arcHeight = action.arcHeight;
                bombData.explodesOnContact = action.explodesOnContact;
                bombData.explosionTimer = action.explosionDelay;
                bombData.isHoming = action.isHoming;

                Vector2 direction = (targetEnemy.transform.position - spawnPosition).normalized;

                projectileQueue.Enqueue((action, bombData, targetEnemy, spawnPosition, direction, action.usesArcTrajectory));
            }
        }
    }

    void LaunchStandardProjectiles(CombatAction action, ProjectileData projectileData, int comboSize)
    {
        if (activeEnemies.Count == 0)
            return;

        int projectileCount = action.baseProjectileCount + Mathf.Max(0, comboSize - 3) * action.projectileCountPerCombo;

        float[] knockbackMultipliers = new float[Mathf.Max(projectileCount, 3)];
        knockbackMultipliers[0] = 1f;
        if (projectileCount > 1)
            knockbackMultipliers[1] = 0.5f;
        if (projectileCount > 2)
            knockbackMultipliers[2] = 0.25f;
        for (int i = 3; i < projectileCount; i++)
            knockbackMultipliers[i] = 0f;

        Vector3 spawnPosition = playerTransform.position + Vector3.up * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            Enemy targetEnemy = GetNextTarget(i % activeEnemies.Count);
            if (targetEnemy != null)
            {
                ProjectileData modifiedData = projectileData.Clone();
                modifiedData.hasKnockback = knockbackMultipliers[i] > 0;
                modifiedData.knockbackForce *= knockbackMultipliers[i];

                Vector2 direction = (targetEnemy.transform.position - spawnPosition).normalized;

                if (projectileCount > 1)
                {
                    float spreadAngle = (i - (projectileCount - 1) / 2f) * 15f;
                    direction = RotateVector2(direction, spreadAngle);
                }

                projectileQueue.Enqueue((action, modifiedData, targetEnemy, spawnPosition, direction, action.usesArcTrajectory));
            }
        }
    }

    void ProcessProjectileQueue()
    {
        if (projectileQueue.Count == 0)
            return;

        if (Time.time >= lastProjectileLaunchTime + PROJECTILE_LAUNCH_DELAY)
        {
            var (action, projectileData, targetEnemy, spawnPosition, direction, isArcProjectile) = projectileQueue.Dequeue();

            if (targetEnemy != null && !targetEnemy.IsDead())
            {
                if (ProjectileManager.Instance != null)
                {
                    GameObject projectileObj = ProjectileManager.Instance.SpawnProjectile(
                        spawnPosition, direction, projectileData,
                        action.projectilePrefab, targetEnemy
                    );

                    if (isArcProjectile && projectileObj != null)
                    {
                        Projectile projectile = projectileObj.GetComponent<Projectile>();
                        if (projectile != null)
                            projectile.SetTargetPosition(targetEnemy.transform.position);
                    }
                }
            }

            lastProjectileLaunchTime = Time.time;
        }
    }

    Vector2 RotateVector2(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    Enemy GetNextTarget(int index = 0)
    {
        activeEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead());

        if (activeEnemies.Count == 0)
            return null;

        activeEnemies.Sort((a, b) =>
            Vector3.Distance(a.transform.position, playerTransform.position)
            .CompareTo(Vector3.Distance(b.transform.position, playerTransform.position)));

        return activeEnemies[index];
    }

    float CalculateActionValue(float baseValue, int comboSize, float comboScaling, int maxComboBonus)
    {
        int bonusBlocks = Mathf.Max(0, Mathf.Min(comboSize - 3, maxComboBonus));
        float multiplier = 1f + (bonusBlocks * (comboScaling - 1f));
        float result = baseValue * multiplier;

        return result;
    }

    void HandleComboExecuted(BlockType blockType, int comboSize)
    {

    }

    void HandleBlocksMatched(List<PuzzleBlock> matchedBlocks)
    {

    }

    void HandleEnemySpawning()
    {
        if (currentWaveData == null || enemiesSpawned >= currentWaveData.totalEnemies)
            return;

        if (Time.time >= lastSpawnTime + currentWaveData.spawnInterval)
        {
            SpawnEnemy();
            lastSpawnTime = Time.time;
        }
    }

    void SpawnEnemy()
    {
        if (currentWaveData.enemyPrefabs.Length == 0)
            return;

        GameObject enemyPrefab = currentWaveData.enemyPrefabs[UnityEngine.Random.Range(0, currentWaveData.enemyPrefabs.Length)];

        Vector3 spawnPos = enemySpawnPoint.position + new Vector3(
            UnityEngine.Random.Range(-2f, 2f),
            0f,
            0f
        );

        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, enemySpawnPoint);
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy != null)
        {
            float healthMultiplier = currentWaveData.enemyHealthMultiplier * (1f + (currentWave - 1) * 0.3f);
            float damageMultiplier = currentWaveData.enemyDamageMultiplier * (1f + (currentWave - 1) * 0.2f);

            enemy.SetMaxHealth(enemy.GetMaxHealth() * healthMultiplier);
            enemy.SetAttackDamage(enemy.GetAttackDamage() * damageMultiplier);

            enemy.OnEnemyDeath += OnEnemyKilled;
            enemy.OnEnemyAttackPlayer += OnEnemyAttackPlayer;

            activeEnemies.Add(enemy);
        }

        enemiesSpawned++;
    }

    void OnEnemyKilled(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);

        enemiesKilled++;

        CheckWaveCompletion();
    }

    void OnEnemyAttackPlayer(Enemy enemy, float damage)
    {
        TakePlayerDamage(damage);
    }

    void TakePlayerDamage(float damage)
    {
        float actualDamage = Mathf.Max(0, damage - playerDefense);
        playerHealth -= actualDamage;
        playerHealth = Mathf.Max(0, playerHealth);

        playerDefense = Mathf.Max(0, playerDefense - damage * 0.5f);

        if (playerHealth <= 0)
        {
            OnPlayerDeath?.Invoke();
            GameOver();
        }
    }

    void HealPlayer(float healAmount)
    {
        player.Heal(healAmount);
    }

    void CheckWaveCompletion()
    {
        if (enemiesSpawned >= currentWaveData.totalEnemies && activeEnemies.Count == 0 && !waveCompleted)
            CompleteWave();
    }

    void CompleteWave()
    {
        waveCompleted = true;
        isInCombat = false;
        OnWaveCompleted?.Invoke(currentWave);

        Debug.Log($"Wave {currentWave} completed!");

        if (currentWave >= totalWaves)
            CompleteLevel();
        else
            StartCoroutine(StartNextWave());
    }

    IEnumerator StartNextWave()
    {
        yield return new WaitForSeconds(waveClearDelay);
        StartWave(currentWave + 1);
    }

    void HandleRewardSelected(Item selectedItem)
    {
        Debug.Log($"Player selected reward: {selectedItem.name}");

        if (selectedItem.armorBonus > 0)
            playerDefense += selectedItem.armorBonus;

        if (selectedItem.damageBonus > 0)
        {
            foreach (var blockType in blockUpgrades.Keys.ToArray())
                blockUpgrades[blockType] += selectedItem.damageBonus / 100f;
        }
    }

    void HandleRewardScreenClosed()
    {
        if (rewardScreenHalf != null)
            rewardScreenHalf.SetActive(false);
        if (puzzleHalf != null)
            puzzleHalf.SetActive(true);

        PauseInput(false);

        if (currentWave < totalWaves)
            StartWave(currentWave + 1);
        else
            CompleteLevel();
    }

    void GenerateRandomRewards()
    {
        string[] possibleRewards =
        {
            "Damage Boost: +20% damage for next wave",
            "Health Potion: Restore 30 health",
            "Shield Upgrade: +15 defense",
            "Speed Boost: +50% projectile speed",
            "Multi-Shot: Fire additional projectiles"
        };

        foreach (Transform child in rewardButtonContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < 3; i++)
        {
            string reward = possibleRewards[UnityEngine.Random.Range(0, possibleRewards.Length)];
            CreateRewardButton(reward, i);
        }
    }

    void CreateRewardButton(string rewardText, int index)
    {
        if (rewardButtonPrefab == null || rewardButtonContainer == null)
            return;

        GameObject button = Instantiate(rewardButtonPrefab, rewardButtonContainer);
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
            buttonText.text = rewardText;

        Button buttonComponent = button.GetComponent<Button>();
        if (buttonComponent != null)
            buttonComponent.onClick.AddListener(() => SelectReward(rewardText));
    }

    void SelectReward(string reward)
    {
        Debug.Log($"Selected reward: {reward}");

        if (rewardScreenHalf != null)
        {
            //puzzleHalf.SetActive(true);
            rewardScreenHalf.SetActive(false);
        }

        StartWave(currentWave + 1);
    }

    void StartWave(int waveNumber)
    {
        currentWave = waveNumber;
        waveCompleted = false;

        if (waveNumber > waveDataArray.Length)
        {
            Debug.LogError($"No wave data for wave {waveNumber}!");
            CompleteLevel();
            return;
        }

        currentWaveData = waveDataArray[waveNumber - 1];
        enemiesSpawned = 0;
        enemiesKilled = 0;
        lastSpawnTime = Time.time;
        waveStartTime = Time.time;
        isInCombat = true;

        OnWaveStarted?.Invoke(currentWave);
        Debug.Log($"Started wave {currentWave}");
    }

    void CompleteLevel()
    {
        OnLevelCompleted?.Invoke();
        Debug.Log("Level completed!");

        if (currentLevelData != null && LevelManager.Instance != null)
        {
            int levelIndex = LevelManager.Instance.GetLevelIndex(currentLevelData);
            LevelManager.Instance.CompleteLevel(levelIndex);
        }

        if (rewardTrigger != null)
        {
            puzzleHalf.SetActive(false);
            rewardTrigger.TriggerLevelCompleteReward();
        }
        else
        {
            if (rewardScreenHalf != null)
            {
                puzzleHalf.SetActive(false);
                rewardScreenHalf.SetActive(true);
            }
        }
    }

    void GameOver()
    {
        isInCombat = false;
        Debug.Log("Game Over!");

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        // Handle game over logic
        // - Show game over screen
        // - Option to restart wave/level
        // - Return to main menu
    }

    void UpdateUI()
    {
        if (playerHealthBar != null)
            playerHealthBar.value = playerHealth;

        if (waveText != null)
            waveText.text = $"Wave {currentWave}/{totalWaves}";

        if (enemyCountText != null && currentWaveData != null)
        {
            int remainingEnemies = Mathf.Max(0, currentWaveData.totalEnemies - enemiesKilled);
            enemyCountText.text = $"Enemies: {remainingEnemies}";
        }
    }

    public bool IsProcessingMatches()
    {
        return combineManager != null ? combineManager.IsProcessingMatches() : false;
    }

    public void SetProcessingMatches(bool processing)
    {
        if (combineManager != null)
            combineManager.SetProcessingMatches(processing);
    }

    public void PauseInput(bool pause)
    {
        if (combineManager != null)
            combineManager.PauseInput(pause);
    }

    public void RegenerateGridWithDeck()
    {
        if (combineManager != null)
            combineManager.RegenerateGridWithDeck(playerDeck);
    }

    public float GetPlayerHealth() => playerHealth;
    public float GetMaxPlayerHealth() => maxPlayerHealth;
    public float GetPlayerDefense() => playerDefense;
    public int GetCurrentWave() => currentWave;
    public bool IsInCombat() => isInCombat;
    public List<Enemy> GetActiveEnemies() => new List<Enemy>(activeEnemies);

    void OnDestroy()
    {
        if (combineManager != null)
        {
            combineManager.OnComboExecuted -= HandleComboExecuted;
            combineManager.OnBlocksMatched -= HandleBlocksMatched;
            combineManager.OnCombatActionTriggered -= HandlePuzzleMatch;
        }

        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelSelected -= InitializeWithLevel;

        if (rewardScreen != null)
        {
            rewardScreen.OnRewardSelected -= HandleRewardSelected;
            rewardScreen.OnRewardScreenClosed -= HandleRewardScreenClosed;
        }
    }
}
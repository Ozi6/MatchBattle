using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private LevelData[] allLevels;
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Save Data")]
    [SerializeField] private string saveKey = "LevelProgress";

    private static LevelManager instance;
    public static LevelManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<LevelManager>();
            return instance;
        }
    }

    public System.Action<LevelData> OnLevelSelected;
    public System.Action<int> OnLevelCompleted;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else if (instance != this)
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateLevelUnlockStatus();
    }

    public LevelData GetCurrentLevel()
    {
        LevelData selected = GetSelectedLevel();
        if (selected != null)
            return selected;

        if (currentLevelIndex >= 0 && currentLevelIndex < allLevels.Length)
            return allLevels[currentLevelIndex];
        return null;
    }

    public LevelData GetSelectedLevel()
    {
        int selectedIndex = PlayerPrefs.GetInt("SelectedLevelIndex", 0);
        if (selectedIndex >= 0 && selectedIndex < allLevels.Length)
        {
            currentLevelIndex = selectedIndex;
            return allLevels[selectedIndex];
        }
        return null;
    }

    public LevelData GetLevel(int index)
    {
        if (index >= 0 && index < allLevels.Length)
            return allLevels[index];
        return null;
    }

    public LevelData[] GetAllLevels()
    {
        return allLevels;
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= allLevels.Length)
            return false;

        LevelData level = allLevels[levelIndex];

        if (level.requiredPreviousLevel == -1)
            return true;

        return IsLevelCompleted(level.requiredPreviousLevel);
    }

    public bool IsLevelCompleted(int levelIndex)
    {
        string key = $"Level_{levelIndex}_Completed";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public void SelectLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < allLevels.Length && IsLevelUnlocked(levelIndex))
        {
            currentLevelIndex = levelIndex;
            Debug.Log($"Selected level {levelIndex}: {allLevels[levelIndex].levelName}");

            PlayerPrefs.SetInt("SelectedLevelIndex", levelIndex);
            PlayerPrefs.Save();

            OnLevelSelected?.Invoke(allLevels[levelIndex]);
        }
    }

    public void CompleteLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < allLevels.Length)
        {
            string key = $"Level_{levelIndex}_Completed";
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            if (levelIndex + 1 < allLevels.Length)
                allLevels[levelIndex + 1].isUnlocked = true;

            OnLevelCompleted?.Invoke(levelIndex);
            PerkManager.Instance.OnLevelCompleted(levelIndex + 1);
        }
    }

    private void UpdateLevelUnlockStatus()
    {
        foreach (LevelData level in allLevels)
        {
            int index = System.Array.IndexOf(allLevels, level);
            level.isUnlocked = IsLevelUnlocked(index);
        }
    }

    public void LoadProgress()
    {
        UpdateLevelUnlockStatus();
    }

    public int GetLevelIndex(LevelData levelData)
    {
        return System.Array.IndexOf(allLevels, levelData);
    }
}
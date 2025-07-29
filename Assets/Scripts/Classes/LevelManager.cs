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
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
        LoadProgress();
    }

    void Start()
    {
        UpdateLevelUnlockStatus();
    }

    public LevelData GetCurrentLevel()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < allLevels.Length)
            return allLevels[currentLevelIndex];

        LevelData selected = GetSelectedLevel();
        if (selected != null)
            return selected;

        return null;
    }

    public void NextLevel()
    {
        currentLevelIndex++;
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
            PlayerPrefs.SetInt("SelectedLevelIndex", levelIndex);
            PlayerPrefs.SetInt("CurrentLevelIndex", levelIndex);
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

            if (levelIndex + 1 < allLevels.Length)
            {
                allLevels[levelIndex + 1].isUnlocked = true;
                currentLevelIndex = levelIndex + 1;
                PlayerPrefs.SetInt("CurrentLevelIndex", currentLevelIndex);
            }

            PlayerPrefs.Save();

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
        currentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 0);

        if (currentLevelIndex >= allLevels.Length || !IsLevelUnlocked(currentLevelIndex))
        {
            for (int i = allLevels.Length - 1; i >= 0; i--)
            {
                if (IsLevelUnlocked(i))
                {
                    currentLevelIndex = i;
                    PlayerPrefs.SetInt("CurrentLevelIndex", currentLevelIndex);
                    PlayerPrefs.Save();
                    break;
                }
            }
        }

        UpdateLevelUnlockStatus();
    }

    public int GetLevelIndex(LevelData levelData)
    {
        return System.Array.IndexOf(allLevels, levelData);
    }
}
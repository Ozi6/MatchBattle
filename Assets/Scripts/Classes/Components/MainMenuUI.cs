using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private LevelSelectionUI levelSelectionUI;
    [SerializeField] private string combatSceneName = "CombatScene";

    [Header("Audio")]
    [SerializeField] private AudioSource buttonClickSound;

    void Start()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        if (levelSelectionUI != null)
            levelSelectionUI.HideLevelSelection();

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        if (levelSelectButton != null)
            levelSelectButton.onClick.AddListener(OnLevelSelectButtonClicked);
    }

    void OnPlayButtonClicked()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();

        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelManager not found!");
            return;
        }

        int latestIncompleteLevel = FindLatestIncompleteLevel();
        if (latestIncompleteLevel >= 0)
        {
            LevelManager.Instance.SelectLevel(latestIncompleteLevel);
            SceneManager.LoadScene(combatSceneName);
        }
        else
        {
            Debug.LogWarning("No incomplete or unlocked levels found. Loading combat scene with default level.");
            SceneManager.LoadScene(combatSceneName);
        }
    }

    void OnLevelSelectButtonClicked()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (levelSelectionUI != null)
            levelSelectionUI.ShowLevelSelection();
    }

    private int FindLatestIncompleteLevel()
    {
        LevelData[] levels = LevelManager.Instance.GetAllLevels();
        int latestIndex = -1;
        for (int i = 0; i < levels.Length; i++)
        {
            if (LevelManager.Instance.IsLevelUnlocked(i) && !LevelManager.Instance.IsLevelCompleted(i))
                latestIndex = i;
        }
        if (latestIndex == -1)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (LevelManager.Instance.IsLevelUnlocked(i))
                {
                    latestIndex = i;
                    break;
                }
            }
        }
        return latestIndex;
    }
}
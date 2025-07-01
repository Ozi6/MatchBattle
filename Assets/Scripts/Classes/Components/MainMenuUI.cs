using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private LevelSelectionUI levelSelectionUI;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private string combatSceneName = "CombatScene";

    [Header("Audio")]
    [SerializeField] private AudioSource buttonClickSound;

    void Start()
    {
        InitializeUI();
        SetupButtonListeners();
    }

    private void InitializeUI()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (levelSelectionUI != null)
            levelSelectionUI.HideLevelSelection();

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void SetupButtonListeners()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (levelSelectButton != null)
            levelSelectButton.onClick.AddListener(OnLevelSelectButtonClicked);

        if (shopButton != null)
            shopButton.onClick.AddListener(OnShopButtonClicked);

        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
    }

    void OnPlayButtonClicked()
    {
        PlayButtonSound();

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
        PlayButtonSound();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (levelSelectionUI != null)
        {
            levelSelectionUI.gameObject.SetActive(true);
            levelSelectionUI.ShowLevelSelection();
        }
    }

    void OnShopButtonClicked()
    {
        PlayButtonSound();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (shopPanel != null)
            shopPanel.SetActive(true);
    }

    void OnInventoryButtonClicked()
    {
        PlayButtonSound();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (levelSelectionUI != null)
            levelSelectionUI.HideLevelSelection();

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();
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
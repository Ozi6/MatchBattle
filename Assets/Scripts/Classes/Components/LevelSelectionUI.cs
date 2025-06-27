using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class LevelSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button backButton;
    [SerializeField] private Text progressText;
    [SerializeField] private Text titleText;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem unlockParticles;
    [SerializeField] private AudioSource buttonClickSound;
    [SerializeField] private AudioSource levelUnlockSound;

    [Header("Animation Settings")]
    [SerializeField] private float buttonAnimationDuration = 0.3f;
    [SerializeField] private float staggerDelay = 0.1f;
    [SerializeField] private AnimationCurve buttonScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Color Scheme")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Scene Management")]
    [SerializeField] private string combatSceneName = "CombatScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private List<LevelButtonData> levelButtons = new List<LevelButtonData>();
    private int selectedLevelIndex = -1;
    private bool isAnimating = false;

    [System.Serializable]
    private class LevelButtonData
    {
        public GameObject buttonObject;
        public LevelData levelData;
        public int levelIndex;
        public Button button;
        public Image backgroundImage;
        public Image iconImage;
        public Image lockIcon;
        public Image completedIcon;
        public Text nameText;
        public Text descriptionText;
        public Text progressText;
        public GameObject starContainer;
        public bool isUnlocked;
        public bool isCompleted;
    }

    void Start()
    {
        InitializeUI();
        CreateLevelButtons();
        StartCoroutine(AnimateButtonsIn());

        if (backButton != null)
            backButton.onClick.AddListener(GoBackWithAnimation);

        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelCompleted += OnLevelCompleted;
    }

    void InitializeUI()
    {
        if (titleText != null)
            titleText.text = "SELECT LEVEL";

        UpdateProgressText();
    }

    void UpdateProgressText()
    {
        if (progressText != null && LevelManager.Instance != null)
        {
            var levels = LevelManager.Instance.GetAllLevels();
            int completedCount = 0;

            for (int i = 0; i < levels.Length; i++)
            {
                if (LevelManager.Instance.IsLevelCompleted(i))
                    completedCount++;
            }

            progressText.text = $"{completedCount}/{levels.Length} LEVELS COMPLETED";
        }
    }

    void CreateLevelButtons()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelManager not found!");
            return;
        }

        LevelData[] levels = LevelManager.Instance.GetAllLevels();

        ClearLevelButtons();
        for (int i = 0; i < levels.Length; i++)
            CreateLevelButton(levels[i], i);
    }

    void ClearLevelButtons()
    {
        foreach (var buttonData in levelButtons)
        {
            if (buttonData.buttonObject != null)
                Destroy(buttonData.buttonObject);
        }
        levelButtons.Clear();
    }

    void CreateLevelButton(LevelData levelData, int levelIndex)
    {
        if (levelButtonPrefab == null || levelButtonContainer == null)
            return;

        GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);

        LevelButtonData buttonData = new LevelButtonData
        {
            buttonObject = buttonObj,
            levelData = levelData,
            levelIndex = levelIndex,
            isUnlocked = LevelManager.Instance.IsLevelUnlocked(levelIndex),
            isCompleted = LevelManager.Instance.IsLevelCompleted(levelIndex)
        };

        SetupButtonComponents(buttonData);

        ConfigureButton(buttonData);

        levelButtons.Add(buttonData);

        buttonObj.transform.localScale = Vector3.zero;
        buttonObj.SetActive(true);
    }

    void SetupButtonComponents(LevelButtonData buttonData)
    {
        var buttonObj = buttonData.buttonObject;

        buttonData.button = buttonObj.GetComponent<Button>();
        buttonData.backgroundImage = buttonObj.GetComponent<Image>();

        buttonData.iconImage = FindChildComponent<Image>(buttonObj, "LevelIcon");
        buttonData.lockIcon = FindChildComponent<Image>(buttonObj, "LockIcon");
        buttonData.completedIcon = FindChildComponent<Image>(buttonObj, "CompletedCheckmark");
        buttonData.nameText = FindChildComponent<Text>(buttonObj, "LevelInfo/LevelName");
        buttonData.descriptionText = FindChildComponent<Text>(buttonObj, "LevelInfo/Description");
        buttonData.progressText = FindChildComponent<Text>(buttonObj, "LevelInfo/ProgressInfo");
        buttonData.starContainer = FindChildObject(buttonObj, "StarRating");
    }

    void ConfigureButton(LevelButtonData buttonData)
    {
        var levelData = buttonData.levelData;

        if (buttonData.nameText != null)
            buttonData.nameText.text = levelData.levelName;

        if (buttonData.descriptionText != null)
            buttonData.descriptionText.text = levelData.description;

        if (buttonData.iconImage != null && levelData.levelIcon != null)
            buttonData.iconImage.sprite = levelData.levelIcon;

        ConfigureButtonState(buttonData);

        if (buttonData.button != null && buttonData.isUnlocked)
        {
            int capturedIndex = buttonData.levelIndex;
            buttonData.button.onClick.AddListener(() => SelectLevelWithAnimation(capturedIndex));
        }

        UpdateButtonProgressText(buttonData);
    }

    void ConfigureButtonState(LevelButtonData buttonData)
    {
        Color targetColor;
        bool isInteractable = false;

        if (buttonData.isCompleted)
        {
            targetColor = completedColor;
            isInteractable = true;
            if (buttonData.completedIcon != null)
                buttonData.completedIcon.gameObject.SetActive(true);
            if (buttonData.lockIcon != null)
                buttonData.lockIcon.gameObject.SetActive(false);
        }
        else if (buttonData.isUnlocked)
        {
            targetColor = unlockedColor;
            isInteractable = true;
            if (buttonData.completedIcon != null)
                buttonData.completedIcon.gameObject.SetActive(false);
            if (buttonData.lockIcon != null)
                buttonData.lockIcon.gameObject.SetActive(false);
        }
        else
        {
            targetColor = lockedColor;
            isInteractable = false;
            if (buttonData.completedIcon != null)
                buttonData.completedIcon.gameObject.SetActive(false);
            if (buttonData.lockIcon != null)
                buttonData.lockIcon.gameObject.SetActive(true);
        }

        if (buttonData.backgroundImage != null)
            buttonData.backgroundImage.color = targetColor;

        if (buttonData.button != null)
            buttonData.button.interactable = isInteractable;

        ConfigureStarRating(buttonData);
    }

    void ConfigureStarRating(LevelButtonData buttonData)
    {
        if (buttonData.starContainer == null || !buttonData.isCompleted)
        {
            if (buttonData.starContainer != null)
                buttonData.starContainer.SetActive(false);
            return;
        }

        buttonData.starContainer.SetActive(true);

        for (int i = 0; i < buttonData.starContainer.transform.childCount; i++)
        {
            var star = buttonData.starContainer.transform.GetChild(i);
            var starImage = star.GetComponent<Image>();
            if (starImage != null)
                starImage.color = Color.yellow;
        }
    }

    void UpdateButtonProgressText(LevelButtonData buttonData)
    {
        if (buttonData.progressText == null)
            return;

        if (buttonData.isCompleted)
        {
            buttonData.progressText.text = "COMPLETED";
            buttonData.progressText.color = Color.green;
        }
        else if (buttonData.isUnlocked)
        {
            buttonData.progressText.text = $"{buttonData.levelData.totalWaves} WAVES";
            buttonData.progressText.color = Color.white;
        }
        else
        {
            buttonData.progressText.text = "LOCKED";
            buttonData.progressText.color = Color.gray;
        }
    }

    IEnumerator AnimateButtonsIn()
    {
        isAnimating = true;

        for (int i = 0; i < levelButtons.Count; i++)
        {
            var buttonData = levelButtons[i];
            StartCoroutine(AnimateButtonScale(buttonData.buttonObject, Vector3.zero, Vector3.one, buttonAnimationDuration));

            if (buttonClickSound != null && buttonData.isUnlocked)
                buttonClickSound.Play();

            yield return new WaitForSeconds(staggerDelay);
        }

        isAnimating = false;
    }

    IEnumerator AnimateButtonScale(GameObject buttonObj, Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = buttonScaleCurve.Evaluate(progress);

            buttonObj.transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, curveValue);
            yield return null;
        }

        buttonObj.transform.localScale = toScale;
    }

    void SelectLevelWithAnimation(int levelIndex)
    {
        if (isAnimating)
            return;

        selectedLevelIndex = levelIndex;

        HighlightSelectedButton(levelIndex);

        if (buttonClickSound != null)
            buttonClickSound.Play();

        StartCoroutine(LoadLevelWithDelay(0.5f));
    }

    void HighlightSelectedButton(int levelIndex)
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            var buttonData = levelButtons[i];
            if (buttonData.backgroundImage != null)
            {
                if (i == levelIndex)
                {
                    buttonData.backgroundImage.color = selectedColor;
                    StartCoroutine(AnimateButtonScale(buttonData.buttonObject, Vector3.one, Vector3.one * 1.1f, 0.2f));
                }
                else
                    ConfigureButtonState(buttonData);
            }
        }
    }

    IEnumerator LoadLevelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (selectedLevelIndex >= 0)
        {
            LevelManager.Instance.SelectLevel(selectedLevelIndex);
            SceneManager.LoadScene(combatSceneName);
        }
    }

    void GoBackWithAnimation()
    {
        if (isAnimating)
            return;

        StartCoroutine(AnimateButtonsOut());
    }

    IEnumerator AnimateButtonsOut()
    {
        isAnimating = true;

        for (int i = levelButtons.Count - 1; i >= 0; i--)
        {
            var buttonData = levelButtons[i];
            StartCoroutine(AnimateButtonScale(buttonData.buttonObject, Vector3.one, Vector3.zero, buttonAnimationDuration * 0.5f));
            yield return new WaitForSeconds(staggerDelay * 0.5f);
        }

        yield return new WaitForSeconds(buttonAnimationDuration * 0.5f);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnLevelCompleted(int levelIndex)
    {
        UpdateProgressText();

        var buttonData = levelButtons.Find(b => b.levelIndex == levelIndex);
        if (buttonData != null)
        {
            buttonData.isCompleted = true;
            ConfigureButtonState(buttonData);
            UpdateButtonProgressText(buttonData);

            if (unlockParticles != null)
            {
                unlockParticles.transform.position = buttonData.buttonObject.transform.position;
                unlockParticles.Play();
            }

            if (levelUnlockSound != null)
                levelUnlockSound.Play();
        }

        RefreshLevelButtons();
    }

    void RefreshLevelButtons()
    {
        foreach (var buttonData in levelButtons)
        {
            bool wasUnlocked = buttonData.isUnlocked;
            buttonData.isUnlocked = LevelManager.Instance.IsLevelUnlocked(buttonData.levelIndex);
            buttonData.isCompleted = LevelManager.Instance.IsLevelCompleted(buttonData.levelIndex);

            ConfigureButton(buttonData);

            if (!wasUnlocked && buttonData.isUnlocked)
            {
                StartCoroutine(AnimateNewlyUnlocked(buttonData));
            }
        }
    }

    IEnumerator AnimateNewlyUnlocked(LevelButtonData buttonData)
    {
        yield return StartCoroutine(AnimateButtonScale(buttonData.buttonObject, Vector3.one, Vector3.one * 1.3f, 0.2f));
        yield return StartCoroutine(AnimateButtonScale(buttonData.buttonObject, Vector3.one * 1.3f, Vector3.one, 0.2f));

        if (levelUnlockSound != null)
            levelUnlockSound.Play();
    }

    T FindChildComponent<T>(GameObject parent, string path) where T : Component
    {
        Transform child = parent.transform.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

    GameObject FindChildObject(GameObject parent, string childName)
    {
        Transform child = parent.transform.Find(childName);
        return child != null ? child.gameObject : null;
    }

    void OnDestroy()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelCompleted -= OnLevelCompleted;
    }

    public void ScrollToLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levelButtons.Count && scrollRect != null)
        {
            var buttonData = levelButtons[levelIndex];
            var buttonRect = buttonData.buttonObject.GetComponent<RectTransform>();

            Canvas.ForceUpdateCanvases();

            Vector2 targetPosition = (Vector2)scrollRect.transform.InverseTransformPoint(buttonRect.position);
            targetPosition.x = 0;

            scrollRect.content.anchoredPosition = targetPosition;
        }
    }

    public void RefreshUI()
    {
        CreateLevelButtons();
        UpdateProgressText();
    }
}
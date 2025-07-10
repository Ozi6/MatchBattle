using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FooterUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button shopButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject inventoryPanel;

    [Header("Sprites")]
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unselectedSprite;

    [Header("Audio")]
    [SerializeField] private AudioSource buttonClickSound;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Color defaultColor = new Color32(0, 28, 71, 255); // #003A92
    [SerializeField] private Color selectedColor = new Color32(75, 110, 175, 255); // #4B6EAF

    private Button currentSelectedButton;
    private Vector2[] originalSizes;
    private Vector3[] originalPositions;

    void Awake()
    {
        InitializeButtonStates();
    }

    void Start()
    {
        InitializeUI();
        SetupButtonListeners();
    }

    private void InitializeButtonStates()
    {
        Button[] buttons = new Button[] { shopButton, inventoryButton, mainMenuButton };
        originalSizes = new Vector2[buttons.Length];
        originalPositions = new Vector3[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                RectTransform rect = buttons[i].GetComponent<RectTransform>();
                originalSizes[i] = rect.sizeDelta;
                originalPositions[i] = rect.anchoredPosition;
                SetButtonColor(buttons[i], defaultColor);
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.sprite = unselectedSprite;
            }
        }
    }

    private void InitializeUI()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (mainMenuButton != null)
        {
            currentSelectedButton = mainMenuButton;
            RectTransform rect = mainMenuButton.GetComponent<RectTransform>();
            Image buttonImage = mainMenuButton.GetComponent<Image>();
            int index = GetButtonIndex(mainMenuButton);

            rect.sizeDelta = originalSizes[index] + new Vector2(5f, 20f);
            rect.anchoredPosition = originalPositions[index] + new Vector3(0f, 8f, 0f);
            if (buttonImage != null)
            {
                buttonImage.color = selectedColor;
                buttonImage.sprite = selectedSprite;
            }
        }
    }

    private void SetupButtonListeners()
    {
        if (shopButton != null)
            shopButton.onClick.AddListener(() => OnShopButtonClicked());

        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(() => OnInventoryButtonClicked());

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => OnMainMenuButtonClicked());
    }

    void OnShopButtonClicked()
    {
        PlayButtonSound();
        HandleButtonSelection(shopButton);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (shopPanel != null)
            shopPanel.SetActive(true);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void OnInventoryButtonClicked()
    {
        PlayButtonSound();
        HandleButtonSelection(inventoryButton);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
    }

    void OnMainMenuButtonClicked()
    {
        PlayButtonSound();
        HandleButtonSelection(mainMenuButton);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void HandleButtonSelection(Button selectedButton)
    {
        if (currentSelectedButton != null && currentSelectedButton != selectedButton)
            StartCoroutine(AnimateButton(currentSelectedButton, false));

        if (selectedButton != null)
        {
            StartCoroutine(AnimateButton(selectedButton, true));
            currentSelectedButton = selectedButton;
        }
    }

    private IEnumerator AnimateButton(Button button, bool isSelected)
    {
        if (button == null)
            yield break;

        RectTransform rect = button.GetComponent<RectTransform>();
        Image buttonImage = button.GetComponent<Image>();
        int index = GetButtonIndex(button);

        Vector2 targetSize = isSelected ? originalSizes[index] + new Vector2(5f, 20f) : originalSizes[index];
        Vector3 targetPosition = isSelected ? originalPositions[index] + new Vector3(0f, 8f, 0f) : originalPositions[index];
        Color targetColor = isSelected ? selectedColor : defaultColor;

        float elapsedTime = 0f;
        Vector2 startSize = rect.sizeDelta;
        Vector3 startPosition = rect.anchoredPosition;
        Color startColor = buttonImage != null ? buttonImage.color : defaultColor;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            float easeOutQuad = 1f - (1f - t) * (1f - t);

            rect.sizeDelta = Vector2.Lerp(startSize, targetSize, easeOutQuad);
            rect.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, easeOutQuad);
            if (buttonImage != null)
                buttonImage.color = Color.Lerp(startColor, targetColor, easeOutQuad);

            yield return null;
        }

        rect.sizeDelta = targetSize;
        rect.anchoredPosition = targetPosition;
        if (buttonImage != null)
        {
            buttonImage.color = targetColor;
            buttonImage.sprite = isSelected ? selectedSprite : unselectedSprite;
        }
    }

    private int GetButtonIndex(Button button)
    {
        if (button == shopButton)
            return 0;
        if (button == inventoryButton)
            return 1;
        if (button == mainMenuButton)
            return 2;
        return -1;
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = color;
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();
    }
}
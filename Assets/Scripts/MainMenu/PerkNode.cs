using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerkNode : MonoBehaviour
{
    [Header("UI Components")]
    public Image nodeIcon;
    public TextMeshProUGUI perkName;
    public TextMeshProUGUI perkDescription;
    public Button perkButton;
    public Image connectionLine;

    [Header("Perk Data")]
    public Perk perk;
    public int requiredLevel;
    public string perkTitle;
    public string description;
    public Sprite unlockedIcon;
    public Sprite lockedIcon;

    [Header("Visual States")]
    public Color unlockedColor = Color.white;
    public Color lockedColor = Color.gray;
    public Color ownedColor = Color.yellow;
    public Color connectionUnlockedColor = Color.green;
    public Color connectionLockedColor = Color.gray;

    [Header("Confirmation")]
    [SerializeField] private PerkConfirmationPanel confirmationPanel;
    [SerializeField] private GameObject confirmationPanelHolder;

    private bool isUnlocked = false;
    private bool isOwned = false;

    void Start()
    {
        perkButton.onClick.AddListener(OnPerkClicked);
        CheckOwnership();
        UpdateVisuals();
        UpdateConnectionLine();
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        CheckOwnership();
        UpdateVisuals();
        UpdateConnectionLine();
    }

    void UpdateVisuals()
    {
        CheckOwnership();

        nodeIcon.sprite = isUnlocked ? (perk?.icon ?? unlockedIcon) : lockedIcon;
        nodeIcon.color = isOwned ? ownedColor : (isUnlocked ? unlockedColor : lockedColor);

        if (perkName != null)
        {
            if (perk != null && !string.IsNullOrEmpty(perk.perkName))
                perkName.text = perk.perkName;
            else
                perkName.text = perkTitle;
        }

        if (perkName != null)
            perkName.color = isUnlocked ? unlockedColor : lockedColor;

        if (perkDescription != null)
        {
            if (isUnlocked)
                perkDescription.text = !string.IsNullOrEmpty(perk?.description) ? perk.description : description;
            else
                perkDescription.text = $"Reach level {requiredLevel}";

            perkDescription.color = isUnlocked ? unlockedColor : lockedColor;
        }

        if (connectionLine != null)
            connectionLine.color = isUnlocked ? connectionUnlockedColor : connectionLockedColor;

        if (perkButton != null)
            perkButton.interactable = isUnlocked && !isOwned;
    }

    void UpdateConnectionLine()
    {
        if (connectionLine == null)
            return;

        RectTransform rect = connectionLine.GetComponent<RectTransform>();
        RectTransform nodeRect = GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -nodeRect.rect.height / 2);

        float spacing = nodeRect.parent.GetComponent<VerticalLayoutGroup>().spacing;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, spacing);
    }

    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    void OnPerkClicked()
    {
        if (isUnlocked && !isOwned)
        {
            if (confirmationPanel != null)
            {
                confirmationPanelHolder.SetActive(true);
                confirmationPanel.ShowConfirmation(perk, (confirmedPerk) =>
                {
                    PerkManager.Instance.OnPerkSelected(this);
                });
            }
            else
                PerkManager.Instance.OnPerkSelected(this);
        }
    }

    private void CheckOwnership()
    {
        if (perk != null && PlayerInventory.Instance != null)
            isOwned = PlayerInventory.Instance.HasPerk(perk);
    }
}
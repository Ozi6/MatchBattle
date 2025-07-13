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
    public Color connectionUnlockedColor = Color.green;
    public Color connectionLockedColor = Color.gray;

    private bool isUnlocked = false;

    void Start()
    {
        perkButton.onClick.AddListener(OnPerkClicked);
        UpdateVisuals();
        UpdateConnectionLine();
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        UpdateVisuals();
        UpdateConnectionLine();
    }

    void UpdateVisuals()
    {
        nodeIcon.sprite = isUnlocked ? (perk?.icon ?? unlockedIcon) : lockedIcon;
        nodeIcon.color = isUnlocked ? unlockedColor : lockedColor;

        perkName.text = perk?.perkName ?? perkTitle;
        perkName.color = isUnlocked ? unlockedColor : lockedColor;

        perkDescription.text = isUnlocked ? (perk?.description ?? description) : $"Reach level {requiredLevel}";
        perkDescription.color = isUnlocked ? unlockedColor : lockedColor;

        if (connectionLine != null)
            connectionLine.color = isUnlocked ? connectionUnlockedColor : connectionLockedColor;
        perkButton.interactable = isUnlocked;
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

    void OnPerkClicked()
    {
        if (isUnlocked)
            PerkManager.Instance.OnPerkSelected(this);
    }

    public bool IsUnlocked()
    {
        return isUnlocked;
    }
}
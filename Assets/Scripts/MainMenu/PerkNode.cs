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
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        nodeIcon.sprite = isUnlocked ? unlockedIcon : lockedIcon;
        nodeIcon.color = isUnlocked ? unlockedColor : lockedColor;

        perkName.text = perkTitle;
        perkName.color = isUnlocked ? unlockedColor : lockedColor;

        perkDescription.text = isUnlocked ? description : "Reach level " + requiredLevel;
        perkDescription.color = isUnlocked ? unlockedColor : lockedColor;

        if (connectionLine != null)
            connectionLine.color = isUnlocked ? connectionUnlockedColor : connectionLockedColor;
        perkButton.interactable = isUnlocked;
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
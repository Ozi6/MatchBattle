using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerkConfirmationPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image perkIcon;
    [SerializeField] private TextMeshProUGUI perkNameText;
    [SerializeField] private TextMeshProUGUI perkDescriptionText;
    [SerializeField] private TextMeshProUGUI perkCostText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Perk currentPerk;
    private System.Action<Perk> confirmCallback;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
        panel.SetActive(false);
    }

    public void ShowConfirmation(Perk perk, System.Action<Perk> callback)
    {
        currentPerk = perk;
        confirmCallback = callback;

        perkIcon.sprite = perk.icon;
        perkNameText.text = perk.perkName;
        perkDescriptionText.text = perk.description;
        perkCostText.text = $"Cost: {perk.requiredLevel}";

        panel.SetActive(true);
    }

    private void OnConfirm()
    {
        confirmCallback?.Invoke(currentPerk);
        panel.SetActive(false);
    }

    private void OnCancel()
    {
        panel.SetActive(false);
    }
}
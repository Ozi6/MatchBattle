using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerkUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject perkNodePrefab;
    [SerializeField] private Transform perkNodeContainer;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Audio")]
    [SerializeField] private AudioSource buttonClickSound;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 1000f;
    [SerializeField] private float autoScrollDuration = 1f;

    private PerkManager perkManager;

    void Awake()
    {
        perkManager = PerkManager.Instance;
        if (perkManager == null)
        {
            Debug.LogError("PerkManager not found!");
            gameObject.SetActive(false);
            return;
        }
    }

    void Start()
    {
        InitializeUI();
        PopulatePerkNodes();
        UpdateLevelText();
    }

    private void InitializeUI()
    {
        gameObject.SetActive(false);
    }

    private void PopulatePerkNodes()
    {
        if (perkNodePrefab == null || perkNodeContainer == null)
        {
            Debug.LogError("PerkNode prefab or container not assigned!");
            return;
        }

        foreach (Transform child in perkNodeContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < perkManager.perkNodeData.Count; i++)
        {
            PerkNodeData nodeData = perkManager.perkNodeData[i];
            GameObject nodeObj = Instantiate(perkNodePrefab, perkNodeContainer);
            PerkNode node = nodeObj.GetComponent<PerkNode>();
            if (node != null)
            {
                node.perk = nodeData.perk;
                node.requiredLevel = nodeData.requiredLevel;
                node.perkTitle = nodeData.perkTitle;
                node.description = nodeData.description;
                node.unlockedIcon = nodeData.unlockedIcon;
                node.lockedIcon = nodeData.lockedIcon;
                node.SetUnlocked(perkManager.currentLevel >= node.requiredLevel);

                if (node.connectionLine != null && i == perkManager.perkNodeData.Count - 1)
                    node.connectionLine.gameObject.SetActive(false);

                perkManager.RegisterPerkNode(node);
            }
        }

        perkManager.scrollRect = scrollRect;
        perkManager.scrollSpeed = scrollSpeed;
        perkManager.autoScrollDuration = autoScrollDuration;
        perkManager.ScrollToLatestUnlocked();
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = $"Level: {perkManager.currentLevel}";
    }

    public void ShowPerkUI()
    {
        gameObject.SetActive(true);
        UpdateLevelText();
        PopulatePerkNodes();
    }
}
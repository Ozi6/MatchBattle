using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[System.Serializable]
public struct PerkNodeData
{
    public Perk perk;
    public int requiredLevel;
    public string perkTitle;
    public string description;
    public Sprite unlockedIcon;
    public Sprite lockedIcon;
}

public class PerkManager : MonoBehaviour
{
    public static PerkManager Instance { get; private set; }

    [Header("Scroll Configuration")]
    public ScrollRect scrollRect;
    public float scrollSpeed = 1000f;
    public float autoScrollDuration = 1f;

    [Header("Perk Nodes")]
    public List<PerkNodeData> perkNodeData = new List<PerkNodeData>();
    private List<PerkNode> instantiatedPerkNodes = new List<PerkNode>();

    [Header("Game Data")]
    public int currentLevel = 1;

    private bool isAutoScrolling = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdatePerksBasedOnLevel();
        ScrollToLatestUnlocked();
    }

    public void RegisterPerkNode(PerkNode node)
    {
        if (!instantiatedPerkNodes.Contains(node))
            instantiatedPerkNodes.Add(node);
    }

    public void UpdatePerksBasedOnLevel()
    {
        foreach (PerkNode perk in instantiatedPerkNodes)
        {
            bool shouldUnlock = currentLevel >= perk.requiredLevel;
            perk.SetUnlocked(shouldUnlock);
        }
    }

    public void OnLevelCompleted(int newLevel)
    {
        int oldLevel = currentLevel;
        currentLevel = newLevel;

        List<PerkNode> newlyUnlocked = new List<PerkNode>();

        foreach (PerkNode perk in instantiatedPerkNodes)
        {
            if (perk.requiredLevel > oldLevel && perk.requiredLevel <= newLevel)
            {
                perk.SetUnlocked(true);
                newlyUnlocked.Add(perk);
            }
        }
        if (newlyUnlocked.Count > 0)
            ScrollToPerk(newlyUnlocked.Last());
    }

    public void ScrollToPerk(PerkNode targetPerk)
    {
        if (isAutoScrolling || targetPerk == null)
            return;

        StartCoroutine(SmoothScrollToPerk(targetPerk));
    }

    public void ScrollToLatestUnlocked()
    {
        PerkNode latestUnlocked = instantiatedPerkNodes.LastOrDefault(perk => perk.IsUnlocked());
        ScrollToPerk(latestUnlocked);
    }

    private IEnumerator SmoothScrollToPerk(PerkNode targetPerk)
    {
        isAutoScrolling = true;

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;
        RectTransform targetRect = targetPerk.GetComponent<RectTransform>();

        Vector2 targetPos = (Vector2)scrollRect.transform.InverseTransformPoint(content.position)
                           - (Vector2)scrollRect.transform.InverseTransformPoint(targetRect.position);

        float targetNormalizedY = Mathf.Clamp01(targetPos.y / (content.rect.height - viewport.rect.height));

        float startY = scrollRect.verticalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < autoScrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / autoScrollDuration;
            t = t * t * (3f - 2f * t);

            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startY, 1f - targetNormalizedY, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = 1f - targetNormalizedY;
        isAutoScrolling = false;
    }

    public void OnPerkSelected(PerkNode selectedPerk)
    {
        if (selectedPerk.perk != null)
        {
            bool added = PlayerInventory.Instance.AddPerk(selectedPerk.perk);
            if (added)
                Debug.Log($"Perk applied: {selectedPerk.perk.perkName}");
        }
    }

    public void LevelCompleted()
    {
        OnLevelCompleted(currentLevel + 1);
    }

    [ContextMenu("Simulate Level Complete")]
    void SimulateLevelComplete()
    {
        LevelCompleted();
    }
}
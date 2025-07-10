using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PerkManager : MonoBehaviour
{
    public static PerkManager Instance { get; private set; }

    [Header("Scroll Configuration")]
    public ScrollRect scrollRect;
    public float scrollSpeed = 1000f;
    public float autoScrollDuration = 1f;

    [Header("Perk Nodes")]
    public List<PerkNode> perkNodes = new List<PerkNode>();

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

    public void UpdatePerksBasedOnLevel()
    {
        foreach (PerkNode perk in perkNodes)
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

        foreach (PerkNode perk in perkNodes)
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
        if (isAutoScrolling)
            return;

        StartCoroutine(SmoothScrollToPerk(targetPerk));
    }

    public void ScrollToLatestUnlocked()
    {
        PerkNode latestUnlocked = null;

        foreach (PerkNode perk in perkNodes)
            if (perk.IsUnlocked())
                latestUnlocked = perk;

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
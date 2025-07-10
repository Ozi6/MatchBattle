using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PuzzleBlock : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Block Settings")]
    public BlockType blockType;
    public int gridX;
    public int gridY;
    public Image blockImage;
    public bool isMatched = false;
    public bool isAnimating = false;

    [Header("Visual Feedback")]
    public GameObject matchEffect;
    public Color highlightColor = Color.yellow;
    private Color originalColor;
    private CombineManager puzzleController;
    private Button blockButton;
    private Material originalMaterial;
    private Coroutine rotationCoroutine;

    private static bool isDragging = false;

    void Awake()
    {
        blockButton = GetComponent<Button>();
        puzzleController = FindAnyObjectByType<CombineManager>();
        if (blockImage == null)
        {
            blockImage = GetComponentInChildren<Image>();
            Image[] images = GetComponentsInChildren<Image>();
            if (images.Length > 1)
                blockImage = images[1];
        }
        blockButton.onClick.AddListener(OnBlockClicked);
        originalMaterial = blockImage.material;
    }

    void OnBlockClicked()
    {
        if (isAnimating || isMatched)
            return;
        puzzleController.OnBlockSelected(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isAnimating || isMatched)
            return;

        isDragging = true;
        puzzleController.OnBlockTouchStart(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isAnimating || isMatched)
            return;

        if (isDragging)
            puzzleController.OnBlockTouchContinue(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;
            puzzleController.OnBlockTouchEnd(this);
        }
    }

    public void SetBlockType(BlockType type)
    {
        blockType = type;
        UpdateVisuals();
    }

    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
        gameObject.name = $"Block_{x}_{y}";
    }

    public void UpdateVisuals()
    {
        if (blockImage != null && puzzleController != null)
        {
            BlockData blockData = puzzleController.GetBlockData(blockType);
            if (blockData != null)
            {
                blockImage.sprite = blockData.sprite;
                blockImage.color = blockData.color;
                Color newColor = blockData.color;
                newColor.a = 1f;
                blockImage.color = newColor;
                originalColor = newColor;
            }
        }
    }

    public void Highlight(bool highlight)
    {

    }

    public void SetSelected(bool isSelected, Material selectedMaterial)
    {
        if (blockImage != null)
        {
            blockImage.material = isSelected ? selectedMaterial : originalMaterial;

            if (rotationCoroutine != null)
            {
                StopCoroutine(rotationCoroutine);
                transform.localRotation = Quaternion.identity;
            }

            StartCoroutine(AnimateScale(isSelected ? Vector3.one * 1.2f : Vector3.one));

            if (isSelected)
                rotationCoroutine = StartCoroutine(AnimateRotation());
        }
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        isAnimating = true;
        Vector3 startScale = transform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
        isAnimating = false;
    }

    private IEnumerator AnimateRotation()
    {
        float time = 0f;
        while (true)
        {
            float angle = Mathf.Sin(time * 4f) * 4f;
            transform.localRotation = Quaternion.Euler(0, 0, angle);
            time += Time.deltaTime;
            yield return null;
        }
    }

    public void PlayMatchEffect()
    {
        if (matchEffect != null)
            matchEffect.SetActive(true);
    }

    public void DestroyBlock()
    {
        isAnimating = true;
        Destroy(gameObject, 0.5f);
    }
}
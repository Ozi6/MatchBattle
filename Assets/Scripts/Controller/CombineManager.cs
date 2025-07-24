using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

public class CombineManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int minimumMatchSize = 3;
    public float matchDelay = 0.5f;
    public BlockData[] blockDataArray;

    [Header("References")]
    public PuzzleGrid puzzleGrid;
    public Camera uiCamera;
    public CombatManager combatManager;
    public LineRenderer selectionLineRenderer;
    public GameObject[] matchParticleSystemPrefabs;
    public Material selectedBlockMaterial;

    private Dictionary<BlockType, BlockData> blockDataDict;
    private List<PuzzleBlock> currentSelection = new List<PuzzleBlock>();
    private List<PuzzleBlock> matchedBlocks = new List<PuzzleBlock>();
    private bool isProcessingMatches = false;
    private bool isTouchingGrid = false;
    private Vector2 mouseWorldPosition;

    public System.Action<List<PuzzleBlock>> OnBlocksMatched;
    public System.Action<BlockType, int> OnComboExecuted;
    public System.Action<BlockType, int, List<PuzzleBlock>> OnCombatActionTriggered;

    void Start()
    {
        InitializeBlockData();
        GenerateInitialGrid();

        if (combatManager == null)
            combatManager = FindAnyObjectByType<CombatManager>();

        if (selectionLineRenderer != null)
        {
            selectionLineRenderer.positionCount = 0;
            selectionLineRenderer.startWidth = 0.1f;
            selectionLineRenderer.endWidth = 0.1f;
            selectionLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            selectionLineRenderer.startColor = Color.yellow;
            selectionLineRenderer.endColor = Color.yellow;
        }
    }

    void Update()
    {
        UpdateSelectionEffect();
    }

    void InitializeBlockData()
    {
        blockDataDict = new Dictionary<BlockType, BlockData>();
        foreach (BlockData data in blockDataArray)
            blockDataDict[data.type] = data;
    }

    void GenerateInitialGrid()
    {
        List<BlockType> characterBlockTypes = PlayerInventory.Instance.GetSelectedCharacterBlockTypes();

        if (characterBlockTypes == null || characterBlockTypes.Count == 0)
        {
            characterBlockTypes = new List<BlockType>();
            foreach (BlockType type in System.Enum.GetValues(typeof(BlockType)))
                if (type != BlockType.Empty && type != BlockType.Multiplier2x && type != BlockType.Multiplier3x)
                    characterBlockTypes.Add(type);
        }

        for (int x = 0; x < puzzleGrid.gridWidth; x++)
        {
            for (int y = 0; y < puzzleGrid.gridHeight; y++)
            {
                PuzzleBlock block = puzzleGrid.GetBlock(x, y);
                if (block != null)
                {
                    float randomValue = Random.value;
                    BlockType selectedType;
                    if (randomValue < 0.05f)
                        selectedType = BlockType.Multiplier2x;
                    else if (randomValue < 0.1f)
                        selectedType = BlockType.Multiplier3x;
                    else
                        selectedType = characterBlockTypes[Random.Range(0, characterBlockTypes.Count)];

                    block.SetBlockType(selectedType);
                }
            }
        }
    }

    public void OnBlockTouchStart(PuzzleBlock block)
    {
        if (isProcessingMatches || block.isMatched || block.isAnimating)
            return;

        isTouchingGrid = true;
        currentSelection.Clear();
        currentSelection.Add(block);
        block.Highlight(true);
        block.SetSelected(true, selectedBlockMaterial);
        mouseWorldPosition = uiCamera.ScreenToWorldPoint(Input.mousePosition);

        UpdateSelectionEffect();
        Debug.Log($"Started selection with block at ({block.gridX}, {block.gridY}) of type {block.blockType}");
    }

    public void OnBlockTouchContinue(PuzzleBlock block)
    {
        if (!isTouchingGrid || currentSelection.Count == 0 || block.isMatched || block.isAnimating)
            return;

        mouseWorldPosition = uiCamera.ScreenToWorldPoint(Input.mousePosition);
        UpdateSelectionEffect();

        if (currentSelection.Contains(block))
        {
            int blockIndex = currentSelection.IndexOf(block);
            if (blockIndex < currentSelection.Count - 1)
            {
                for (int i = currentSelection.Count - 1; i > blockIndex; i--)
                {
                    currentSelection[i].Highlight(false);
                    currentSelection[i].SetSelected(false, null);
                    currentSelection.RemoveAt(i);
                }
                Debug.Log($"Backtracked to block at ({block.gridX}, {block.gridY})");
            }
            UpdateSelectionEffect();
            return;
        }

        PuzzleBlock lastBlock = currentSelection[currentSelection.Count - 1];
        if (!IsAdjacent(lastBlock, block))
            return;

        BlockType selectionType = currentSelection[0].blockType;
        if (block.blockType == BlockType.Multiplier2x || block.blockType == BlockType.Multiplier3x ||
            (block.blockType == selectionType && selectionType != BlockType.Multiplier2x && selectionType != BlockType.Multiplier3x))
        {
            currentSelection.Add(block);
            block.Highlight(true);
            block.SetSelected(true, selectedBlockMaterial);
            UpdateSelectionEffect();
        }
    }

    public void OnBlockTouchEnd(PuzzleBlock block)
    {
        if (!isTouchingGrid)
            return;

        isTouchingGrid = false;

        if (currentSelection.Count >= minimumMatchSize)
        {
            Debug.Log($"Valid match found! {currentSelection.Count} blocks of type {currentSelection[0].blockType}");
            ProcessMatch(new List<PuzzleBlock>(currentSelection));
        }
        else
        {
            Debug.Log($"Not enough blocks for match. Need {minimumMatchSize}, got {currentSelection.Count}");
            ClearSelection();
        }

        if (selectionLineRenderer != null)
            selectionLineRenderer.positionCount = 0;
    }

    private void UpdateSelectionEffect()
    {
        if (!isTouchingGrid || currentSelection.Count == 0)
            return;

        if (selectionLineRenderer != null)
        {
            selectionLineRenderer.positionCount = currentSelection.Count + 1;
            for (int i = 0; i < currentSelection.Count; i++)
            {
                Vector3 blockPos = currentSelection[i].transform.position;
                blockPos.z = -1;
                selectionLineRenderer.SetPosition(i, blockPos);
            }
            selectionLineRenderer.SetPosition(currentSelection.Count, new Vector3(mouseWorldPosition.x, mouseWorldPosition.y, -1));
        }
    }

    void ProcessMatch(List<PuzzleBlock> matchedBlocks)
    {
        isProcessingMatches = true;

        BlockType comboType = BlockType.Empty;
        foreach (PuzzleBlock block in matchedBlocks)
        {
            if (block.blockType != BlockType.Multiplier2x && block.blockType != BlockType.Multiplier3x)
            {
                comboType = block.blockType;
                break;
            }
        }

        if (comboType == BlockType.Empty)
        {
            Debug.Log("No valid primary block type in chain, clearing selection.");
            ClearSelection();
            isProcessingMatches = false;
            return;
        }

        int comboSize = matchedBlocks.Count;
        float multiplier = CalculateChainMultiplier(matchedBlocks);

        if (matchParticleSystemPrefabs != null)
        {
            foreach (PuzzleBlock block in matchedBlocks)
            {
                GameObject particlePrefab = matchParticleSystemPrefabs[UnityEngine.Random.Range(0, matchParticleSystemPrefabs.Length)];
                if (particlePrefab != null)
                {
                    GameObject effectInstance = Instantiate(particlePrefab, block.transform.position, Quaternion.identity);
                    ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Play();
                        float duration = ps.main.duration;
                        Destroy(effectInstance, duration);
                    }
                    else
                    {
                        Destroy(effectInstance, 1f);
                    }
                }
            }
        }

        foreach (PuzzleBlock block in matchedBlocks)
        {
            block.isMatched = true;
            block.PlayMatchEffect();
            block.SetSelected(false, null);
        }

        OnBlocksMatched?.Invoke(matchedBlocks);
        OnComboExecuted?.Invoke(comboType, comboSize);
        OnCombatActionTriggered?.Invoke(comboType, Mathf.RoundToInt(comboSize * multiplier), matchedBlocks);

        StartCoroutine(DestroyMatchedBlocks(matchedBlocks));
    }

    IEnumerator DestroyMatchedBlocks(List<PuzzleBlock> blocks)
    {
        yield return new WaitForSeconds(matchDelay);

        foreach (PuzzleBlock block in blocks)
        {
            if (block != null)
            {
                puzzleGrid.SetBlock(block.gridX, block.gridY, null);
                block.DestroyBlock();
            }
        }

        yield return new WaitForSeconds(0.5f);

        ApplyGravity();
        yield return new WaitForSeconds(0.3f);

        RefillEmptySpaces();
        yield return new WaitForSeconds(0.3f);

        isProcessingMatches = false;
        ClearSelection();
    }

    void ApplyGravity()
    {
        for (int x = 0; x < puzzleGrid.gridWidth; x++)
        {
            List<PuzzleBlock> columnBlocks = new List<PuzzleBlock>();
            for (int y = puzzleGrid.gridHeight - 1; y >= 0; y--)
            {
                PuzzleBlock block = puzzleGrid.GetBlock(x, y);
                if (block != null)
                {
                    columnBlocks.Add(block);
                    puzzleGrid.SetBlock(x, y, null);
                }
            }

            for (int i = 0; i < columnBlocks.Count; i++)
            {
                int newY = puzzleGrid.gridHeight - 1 - i;
                puzzleGrid.SetBlock(x, newY, columnBlocks[i]);
                columnBlocks[i].SetGridPosition(x, newY);
                StartCoroutine(AnimateBlockFall(columnBlocks[i], x, newY));
            }
        }
    }

    IEnumerator AnimateBlockFall(PuzzleBlock block, int newX, int newY)
    {
        if (block == null) yield break;

        block.isAnimating = true;
        RectTransform rect = block.GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition;

        float cellSize = puzzleGrid.cellSize;
        float spacing = puzzleGrid.spacing;
        Vector2 targetPos = new Vector2(
            (newX * (cellSize + spacing)) - (((puzzleGrid.gridWidth - 1) * (cellSize + spacing)) / 2f),
            -(newY * (cellSize + spacing)) + (((puzzleGrid.gridHeight - 1) * (cellSize + spacing)) / 2f)
        );

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration && block != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (rect != null)
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (rect != null)
            rect.anchoredPosition = targetPos;

        if (block != null)
            block.isAnimating = false;
    }

    void RefillEmptySpaces()
    {
        List<BlockType> characterBlockTypes = PlayerInventory.Instance.GetSelectedCharacterBlockTypes();

        if (characterBlockTypes == null || characterBlockTypes.Count == 0)
        {
            characterBlockTypes = new List<BlockType>();
            foreach (BlockType type in System.Enum.GetValues(typeof(BlockType)))
                if (type != BlockType.Empty && type != BlockType.Multiplier2x && type != BlockType.Multiplier3x)
                    characterBlockTypes.Add(type);
        }

        for (int x = 0; x < puzzleGrid.gridWidth; x++)
        {
            int emptyCount = 0;
            for (int y = puzzleGrid.gridHeight - 1; y >= 0; y--)
                if (puzzleGrid.GetBlock(x, y) == null)
                    emptyCount++;

            int currentEmptyIndex = 0;
            for (int y = puzzleGrid.gridHeight - 1; y >= 0; y--)
            {
                if (puzzleGrid.GetBlock(x, y) == null)
                {
                    GameObject blockObj = Instantiate(puzzleGrid.blockPrefab, puzzleGrid.transform);
                    PuzzleBlock newBlock = blockObj.GetComponent<PuzzleBlock>();

                    float randomValue = Random.value;
                    BlockType selectedType;
                    if (randomValue < 0.05f)
                        selectedType = BlockType.Multiplier2x;
                    else if (randomValue < 0.1f)
                        selectedType = BlockType.Multiplier3x;
                    else
                        selectedType = characterBlockTypes[Random.Range(0, characterBlockTypes.Count)];

                    newBlock.SetBlockType(selectedType);
                    newBlock.SetGridPosition(x, y);
                    puzzleGrid.SetBlock(x, y, newBlock);

                    RectTransform rect = blockObj.GetComponent<RectTransform>();
                    float cellSize = puzzleGrid.cellSize;
                    float spacing = puzzleGrid.spacing;

                    Vector2 finalPos = new Vector2(
                        (x * (cellSize + spacing)) - (((puzzleGrid.gridWidth - 1) * (cellSize + spacing)) / 2f),
                        -(y * (cellSize + spacing)) + (((puzzleGrid.gridHeight - 1) * (cellSize + spacing)) / 2f)
                    );

                    Vector2 startPos = new Vector2(
                        finalPos.x,
                        finalPos.y + (cellSize + spacing) * (emptyCount - currentEmptyIndex + 1)
                    );

                    rect.anchoredPosition = startPos;
                    rect.sizeDelta = new Vector2(cellSize, cellSize);

                    StartCoroutine(AnimateBlockRefill(newBlock, startPos, finalPos, currentEmptyIndex * 0.1f));

                    currentEmptyIndex++;
                }
            }
        }
    }

    IEnumerator AnimateBlockRefill(PuzzleBlock block, Vector2 startPos, Vector2 finalPos, float delay)
    {
        if (block == null)
            yield break;

        block.isAnimating = true;
        RectTransform rect = block.GetComponent<RectTransform>();

        yield return new WaitForSeconds(delay);

        rect.anchoredPosition = startPos;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration && block != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float easedT = EaseOutBounce(t);

            if (rect != null)
                rect.anchoredPosition = Vector2.Lerp(startPos, finalPos, easedT);
            yield return null;
        }

        if (rect != null)
            rect.anchoredPosition = finalPos;

        if (block != null)
            block.isAnimating = false;
    }

    private float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f)
            return 7.5625f * t * t;
        else if (t < 2f / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }

    public void OnBlockSelected(PuzzleBlock selectedBlock)
    {
        if (isProcessingMatches)
            return;

        Debug.Log($"Block selected via button: ({selectedBlock.gridX}, {selectedBlock.gridY})");
    }

    bool IsAdjacent(PuzzleBlock block1, PuzzleBlock block2)
    {
        int dx = Mathf.Abs(block1.gridX - block2.gridX);
        int dy = Mathf.Abs(block1.gridY - block2.gridY);

        return (dx <= 1 && dy <= 1) && (dx + dy > 0);
    }

    void ClearSelection()
    {
        foreach (PuzzleBlock block in currentSelection)
        {
            if (block != null)
            {
                block.Highlight(false);
                block.SetSelected(false, null);
            }
        }
        currentSelection.Clear();

        if (selectionLineRenderer != null)
            selectionLineRenderer.positionCount = 0;
    }

    public BlockData GetBlockData(BlockType type)
    {
        return blockDataDict.ContainsKey(type) ? blockDataDict[type] : null;
    }

    public bool IsProcessingMatches()
    {
        return isProcessingMatches;
    }

    public void SetProcessingMatches(bool processing)
    {
        isProcessingMatches = processing;
    }

    public void PauseInput(bool pause)
    {
        isProcessingMatches = pause;
    }

    public void RegenerateGridWithDeck(List<BlockType> deckBlocks)
    {
        for (int x = 0; x < puzzleGrid.gridWidth; x++)
        {
            for (int y = 0; y < puzzleGrid.gridHeight; y++)
            {
                PuzzleBlock block = puzzleGrid.GetBlock(x, y);
                if (block != null)
                {
                    BlockType randomType = deckBlocks[UnityEngine.Random.Range(0, deckBlocks.Count)];
                    block.SetBlockType(randomType);
                }
            }
        }
    }

    public BlockType[,] GetGridState()
    {
        BlockType[,] gridState = new BlockType[puzzleGrid.gridWidth, puzzleGrid.gridHeight];

        for (int x = 0; x < puzzleGrid.gridWidth; x++)
        {
            for (int y = 0; y < puzzleGrid.gridHeight; y++)
            {
                PuzzleBlock block = puzzleGrid.GetBlock(x, y);
                gridState[x, y] = block != null ? block.blockType : BlockType.Empty;
            }
        }

        return gridState;
    }

    private float CalculateChainMultiplier(List<PuzzleBlock> blocks)
    {
        float totalMultiplier = 1f;
        foreach (PuzzleBlock block in blocks)
        {
            if (block.blockType == BlockType.Multiplier2x)
                totalMultiplier *= 2f;
            else if (block.blockType == BlockType.Multiplier3x)
                totalMultiplier *= 3f;
        }
        return totalMultiplier;
    }
}

public enum BlockType
{
    Sword,
    Shield,
    Potion,
    Bow,
    Magic,
    Axe,
    Dynamite,
    Multiplier2x,
    Multiplier3x,
    Empty
}

[System.Serializable]
public class BlockData
{
    public BlockType type;
    public Sprite sprite;
    public Color color = Color.white;
    public float multiplier = 1f;

    [Header("Combat Properties")]
    public bool isOffensive = true;
    public DebuffType debuffType = DebuffType.Bleed;
    public float debuffDuration = 0f;
    public float debuffIntensity = 0f;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 5f;
    public bool isPiercing = false;
    public bool hasAreaEffect = false;
    public float areaRadius = 1f;
}
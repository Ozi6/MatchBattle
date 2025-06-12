using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using System;

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

    private Dictionary<BlockType, BlockData> blockDataDict;
    private List<PuzzleBlock> currentSelection = new List<PuzzleBlock>();
    private List<PuzzleBlock> matchedBlocks = new List<PuzzleBlock>();
    private bool isProcessingMatches = false;
    private bool isTouchingGrid = false;

    public Action<List<PuzzleBlock>> OnBlocksMatched;
    public Action<BlockType, int> OnComboExecuted;
    public Action<BlockType, int, List<PuzzleBlock>> OnCombatActionTriggered;

    void Start()
    {
        InitializeBlockData();
        GenerateInitialGrid();

        if (combatManager == null)
            combatManager = FindAnyObjectByType<CombatManager>();
    }

    void Update()
    {

    }

    void InitializeBlockData()
    {
        blockDataDict = new Dictionary<BlockType, BlockData>();
        foreach (BlockData data in blockDataArray)
            blockDataDict[data.type] = data;
    }

    void GenerateInitialGrid()
    {
        for (int x = 0; x < puzzleGrid.gridWidth; x++)
        {
            for (int y = 0; y < puzzleGrid.gridHeight; y++)
            {
                PuzzleBlock block = puzzleGrid.GetBlock(x, y);
                if (block != null)
                {
                    BlockType randomType = (BlockType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(BlockType)).Length - 1);
                    block.SetBlockType(randomType);
                }
            }
        }
    }

    /*
    private void HandleTouchInput()
    {
        if (isProcessingMatches)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            StartTouch(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isTouchingGrid)
        {
            ContinueTouch(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndTouch(Input.mousePosition);
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartTouch(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isTouchingGrid)
                        ContinueTouch(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndTouch(touch.position);
                    break;
            }
        }
    }

    private void StartTouch(Vector2 screenPosition)
    {
        PuzzleBlock block = GetBlockAtScreenPosition(screenPosition);
        if (block != null && !block.isMatched && !block.isAnimating)
        {
            isTouchingGrid = true;
            currentSelection.Clear();
            currentSelection.Add(block);
            block.Highlight(true);
            Debug.Log($"Started selection with block at ({block.gridX}, {block.gridY}) of type {block.blockType}");
        }
    }

    private void ContinueTouch(Vector2 screenPosition)
    {
        if (currentSelection.Count == 0)
            return;

        PuzzleBlock block = GetBlockAtScreenPosition(screenPosition);
        if (block != null && !block.isMatched && !block.isAnimating)
        {
            if (currentSelection.Contains(block))
            {
                int blockIndex = currentSelection.IndexOf(block);
                if (blockIndex < currentSelection.Count - 1)
                {
                    for (int i = currentSelection.Count - 1; i > blockIndex; i--)
                    {
                        currentSelection[i].Highlight(false);
                        currentSelection.RemoveAt(i);
                    }
                    Debug.Log($"Backtracked to block at ({block.gridX}, {block.gridY})");
                }
                return;
            }

            BlockType selectionType = currentSelection[0].blockType;
            if (block.blockType == selectionType)
            {
                PuzzleBlock lastBlock = currentSelection[currentSelection.Count - 1];
                if (IsAdjacent(lastBlock, block))
                {
                    currentSelection.Add(block);
                    block.Highlight(true);
                    Debug.Log($"Added block at ({block.gridX}, {block.gridY}) to selection. Total: {currentSelection.Count}");
                }
            }
        }
    }

    private void EndTouch(Vector2 screenPosition)
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
    }
    */

    private PuzzleBlock GetBlockAtScreenPosition(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            PuzzleBlock block = result.gameObject.GetComponent<PuzzleBlock>();
            if (block != null)
                return block;
        }

        return GetBlockAtScreenPositionUI(screenPosition);
    }

    private PuzzleBlock GetBlockAtScreenPositionUI(Vector2 screenPosition)
    {
        RectTransform gridRect = puzzleGrid.GetComponent<RectTransform>();
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect, screenPosition, uiCamera, out localPoint))
        {
            float cellSize = puzzleGrid.cellSize;
            float spacing = puzzleGrid.spacing;

            float totalWidth = (puzzleGrid.gridWidth - 1) * (cellSize + spacing);
            float totalHeight = (puzzleGrid.gridHeight - 1) * (cellSize + spacing);

            float adjustedX = localPoint.x + totalWidth / 2f;
            float adjustedY = -localPoint.y + totalHeight / 2f;

            int gridX = Mathf.RoundToInt(adjustedX / (cellSize + spacing));
            int gridY = Mathf.RoundToInt(adjustedY / (cellSize + spacing));

            if (gridX >= 0 && gridX < puzzleGrid.gridWidth &&
                gridY >= 0 && gridY < puzzleGrid.gridHeight)
            {
                return puzzleGrid.GetBlock(gridX, gridY);
            }
        }

        return null;
    }

    public void OnBlockTouchStart(PuzzleBlock block)
    {
        if (isProcessingMatches || block.isMatched || block.isAnimating)
            return;

        isTouchingGrid = true;
        currentSelection.Clear();
        currentSelection.Add(block);
        block.Highlight(true);
        Debug.Log($"Started selection with block at ({block.gridX}, {block.gridY}) of type {block.blockType}");
    }

    public void OnBlockTouchContinue(PuzzleBlock block)
    {
        if (!isTouchingGrid || currentSelection.Count == 0 || block.isMatched || block.isAnimating)
            return;

        if (currentSelection.Contains(block))
        {
            int blockIndex = currentSelection.IndexOf(block);
            if (blockIndex < currentSelection.Count - 1)
            {
                for (int i = currentSelection.Count - 1; i > blockIndex; i--)
                {
                    currentSelection[i].Highlight(false);
                    currentSelection.RemoveAt(i);
                }
                Debug.Log($"Backtracked to block at ({block.gridX}, {block.gridY})");
            }
            return;
        }

        BlockType selectionType = currentSelection[0].blockType;
        if (block.blockType == selectionType)
        {
            PuzzleBlock lastBlock = currentSelection[currentSelection.Count - 1];
            if (IsAdjacent(lastBlock, block))
            {
                currentSelection.Add(block);
                block.Highlight(true);
                Debug.Log($"Added block at ({block.gridX}, {block.gridY}) to selection. Total: {currentSelection.Count}");
            }
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
    }

    void ProcessMatch(List<PuzzleBlock> matchedBlocks)
    {
        isProcessingMatches = true;

        BlockType comboType = matchedBlocks[0].blockType;
        int comboSize = matchedBlocks.Count;

        foreach (PuzzleBlock block in matchedBlocks)
        {
            block.isMatched = true;
            block.PlayMatchEffect();
        }
            
        OnBlocksMatched?.Invoke(matchedBlocks);
        OnComboExecuted?.Invoke(comboType, comboSize);

        if (combatManager != null)
            combatManager.HandlePuzzleMatch(comboType, comboSize, matchedBlocks);

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

        CheckForAutoMatches();

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
        for (int x = 0; x < puzzleGrid.gridWidth; x++)
        {
            for (int y = 0; y < puzzleGrid.gridHeight; y++)
            {
                if (puzzleGrid.GetBlock(x, y) == null)
                {
                    GameObject blockObj = Instantiate(puzzleGrid.blockPrefab, puzzleGrid.transform);
                    PuzzleBlock newBlock = blockObj.GetComponent<PuzzleBlock>();

                    BlockType randomType = (BlockType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(BlockType)).Length - 1);
                    newBlock.SetBlockType(randomType);
                    newBlock.SetGridPosition(x, y);
                    puzzleGrid.SetBlock(x, y, newBlock);

                    RectTransform rect = blockObj.GetComponent<RectTransform>();
                    float cellSize = puzzleGrid.cellSize;
                    float spacing = puzzleGrid.spacing;
                    Vector2 pos = new Vector2(
                        (x * (cellSize + spacing)) - (((puzzleGrid.gridWidth - 1) * (cellSize + spacing)) / 2f),
                        -(y * (cellSize + spacing)) + (((puzzleGrid.gridHeight - 1) * (cellSize + spacing)) / 2f)
                    );
                    rect.anchoredPosition = pos;
                    rect.sizeDelta = new Vector2(cellSize, cellSize);
                }
            }
        }
    }

    void CheckForAutoMatches()
    {

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
            if (block != null)
                block.Highlight(false);
        currentSelection.Clear();
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
    Empty
}

[System.Serializable]
public class BlockData
{
    public BlockType type;
    public Sprite sprite;
    public Color color = Color.white;

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
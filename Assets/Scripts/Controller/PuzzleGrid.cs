using UnityEngine;
using System.Collections.Generic;

public class PuzzleGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 20f;
    public float spacing = 5f;

    [Header("Prefabs")]
    public GameObject blockPrefab;

    private PuzzleBlock[,] grid;
    private RectTransform gridTransform;

    void Awake()
    {
        gridTransform = GetComponent<RectTransform>();
        InitializeGrid();
    }

    void InitializeGrid()
    {
        grid = new PuzzleBlock[gridWidth, gridHeight];

        float totalWidth = (gridWidth * cellSize) + ((gridWidth - 1) * spacing);
        float totalHeight = (gridHeight * cellSize) + ((gridHeight - 1) * spacing);

        gridTransform.sizeDelta = new Vector2(totalWidth, totalHeight);

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                CreateBlock(x, y);
    }

    void CreateBlock(int x, int y)
    {
        GameObject blockObj = Instantiate(blockPrefab, transform);
        PuzzleBlock block = blockObj.GetComponent<PuzzleBlock>();

        RectTransform blockRect = blockObj.GetComponent<RectTransform>();
        Vector2 pos = new Vector2(
            (x * (cellSize + spacing)) - (((gridWidth - 1) * (cellSize + spacing)) / 2f),
            -(y * (cellSize + spacing)) + (((gridHeight - 1) * (cellSize + spacing)) / 2f)
        );
        blockRect.anchoredPosition = pos;
        blockRect.sizeDelta = new Vector2(cellSize, cellSize);

        block.SetGridPosition(x, y);
        grid[x, y] = block;
    }

    public PuzzleBlock GetBlock(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            return grid[x, y];
        return null;
    }

    public void SetBlock(int x, int y, PuzzleBlock block)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
        {
            grid[x, y] = block;
            if (block != null)
                block.SetGridPosition(x, y);
        }
    }

    public List<PuzzleBlock> GetNeighbors(int x, int y, bool includeDiagonals = true)
    {
        List<PuzzleBlock> neighbors = new List<PuzzleBlock>();

        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        if (includeDiagonals)
        {
            dx = new int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
            dy = new int[] { -1, 0, 1, -1, 1, -1, 0, 1 };
        }

        for (int i = 0; i < dx.Length; i++)
        {
            int newX = x + dx[i];
            int newY = y + dy[i];

            PuzzleBlock neighbor = GetBlock(newX, newY);
            if (neighbor != null)
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    public void ClearGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
        }
    }

    public void RefillGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == null)
                    CreateBlock(x, y);
            }
        }
    }
}
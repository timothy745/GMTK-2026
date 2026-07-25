using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MazeRenderer : MonoBehaviour
{
    private RectTransform mazeContainer;
    private float cellSize = 48f;
    private float wallThickness = 6f;
    private List<Image> whiteTiles = new List<Image>();

    public Vector2Int ExitPosition { get; private set; }

    public List<Image> WhiteTiles => whiteTiles;

    public void BuildMaze(MazeGenerator.Cell[,] grid, RectTransform container, int mazeWidth, int mazeHeight)
    {
        mazeContainer = container;
        whiteTiles.Clear();

        foreach (Transform child in mazeContainer)
            Destroy(child.gameObject);

        // Center the container on the canvas
        mazeContainer.anchorMin = new Vector2(0.5f, 0.5f);
        mazeContainer.anchorMax = new Vector2(0.5f, 0.5f);
        mazeContainer.pivot = new Vector2(0.5f, 0.5f);
        mazeContainer.anchoredPosition = Vector2.zero;

        // Scale cell size to fit within the screen
        Canvas canvas = mazeContainer.GetComponentInParent<Canvas>();
        float maxW = canvas.pixelRect.width * 0.85f;
        float maxH = canvas.pixelRect.height * 0.85f;
        float fitW = maxW / mazeWidth;
        float fitH = maxH / mazeHeight;
        cellSize = Mathf.Min(fitW, fitH, 48f);

        float totalWidth = mazeWidth * cellSize;
        float totalHeight = mazeHeight * cellSize;
        mazeContainer.sizeDelta = new Vector2(totalWidth + wallThickness, totalHeight + wallThickness);

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                MazeGenerator.Cell cell = grid[x, y];
                Vector2 cellPos = GetCellPosition(x, y, mazeWidth, mazeHeight);

                // Floor tile (black background)
                CreateTile(cellPos, new Vector2(cellSize, cellSize), new Color(0.1f, 0.1f, 0.1f), "Floor_" + x + "_" + y);

                // Top wall
                if (cell.top)
                    CreateTile(cellPos + Vector2.up * (cellSize * 0.5f), new Vector2(cellSize + wallThickness, wallThickness), Color.white, "Wall_Top_" + x + "_" + y);

                // Bottom wall
                if (cell.bottom)
                    CreateTile(cellPos + Vector2.down * (cellSize * 0.5f), new Vector2(cellSize + wallThickness, wallThickness), Color.white, "Wall_Bot_" + x + "_" + y);

                // Left wall
                if (cell.left)
                    CreateTile(cellPos + Vector2.left * (cellSize * 0.5f), new Vector2(wallThickness, cellSize + wallThickness), Color.white, "Wall_L_" + x + "_" + y);

                // Right wall
                if (cell.right)
                    CreateTile(cellPos + Vector2.right * (cellSize * 0.5f), new Vector2(wallThickness, cellSize + wallThickness), Color.white, "Wall_R_" + x + "_" + y);
            }
        }

        // Start marker (green)
        CreateTile(GetCellPosition(0, 0, mazeWidth, mazeHeight), new Vector2(cellSize * 0.3f, cellSize * 0.3f), Color.green, "Start");

        // Exit marker (gold)
        ExitPosition = new Vector2Int(mazeWidth - 1, mazeHeight - 1);
        CreateTile(GetCellPosition(ExitPosition.x, ExitPosition.y, mazeWidth, mazeHeight), new Vector2(cellSize * 0.3f, cellSize * 0.3f), Color.yellow, "Exit");
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    private Vector2 GetCellPosition(int x, int y, int mazeWidth, int mazeHeight)
    {
        float offsetX = -(mazeWidth * cellSize) / 2f + cellSize / 2f;
        float offsetY = -(mazeHeight * cellSize) / 2f + cellSize / 2f;
        return new Vector2(x * cellSize + offsetX, y * cellSize + offsetY);
    }

    private void CreateTile(Vector2 position, Vector2 size, Color color, string tileName)
    {
        GameObject tile = new GameObject(tileName);
        tile.transform.SetParent(mazeContainer, false);

        RectTransform rect = tile.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = tile.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        if (color == Color.white)
            whiteTiles.Add(img);
    }
}

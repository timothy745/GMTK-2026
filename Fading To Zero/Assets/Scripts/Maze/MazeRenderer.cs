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

        // 1. Bersihkan tile lama
        foreach (Transform child in mazeContainer)
            Destroy(child.gameObject);

        // 2. Set anchor & pivot container ke tengah
        mazeContainer.anchorMin = new Vector2(0.5f, 0.5f);
        mazeContainer.anchorMax = new Vector2(0.5f, 0.5f);
        mazeContainer.pivot = new Vector2(0.5f, 0.5f);
        mazeContainer.anchoredPosition = Vector2.zero;

        // 3. Hitung skala cellSize agar muat di Canvas
        Canvas canvas = mazeContainer.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            float maxW = canvas.pixelRect.width * 0.85f;
            float maxH = canvas.pixelRect.height * 0.85f;
            float fitW = maxW / mazeWidth;
            float fitH = maxH / mazeHeight;
            cellSize = Mathf.Min(fitW, fitH, 48f);
        }

        float totalWidth = mazeWidth * cellSize;
        float totalHeight = mazeHeight * cellSize;
        mazeContainer.sizeDelta = new Vector2(totalWidth + wallThickness, totalHeight + wallThickness);

        // 4. Generate lantai dan tembok
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

        // Exit marker (yellow)
        ExitPosition = new Vector2Int(mazeWidth - 1, mazeHeight - 1);
        CreateTile(GetCellPosition(ExitPosition.x, ExitPosition.y, mazeWidth, mazeHeight), new Vector2(cellSize * 0.3f, cellSize * 0.3f), Color.yellow, "Exit");
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    // ========================================================================
    // FUNGSI PERBAIKAN POSISI (AGAR MASUK PAS DI DALAM FRAME)
    // ========================================================================

    public Vector2 GetCellPosition(int x, int y, int width, int height)
    {
        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;

        // Offset ini yang menarik titik (0,0) ke pojok kiri-bawah container
        // Sehingga seluruh maze berada presisi di tengah-tengah frame container
        float startX = -totalWidth / 2f + cellSize / 2f;
        float startY = -totalHeight / 2f + cellSize / 2f;

        return new Vector2(startX + (x * cellSize), startY + (y * cellSize));
    }

    private Image CreateTile(Vector2 anchoredPos, Vector2 size, Color color, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        
        // Wajib set worldPositionStays = false agar posisinya terkunci di UI
        go.transform.SetParent(mazeContainer, false); 

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos; // Gunakan anchoredPosition khusus UI

        Image img = go.GetComponent<Image>();
        img.color = color;

        if (color == Color.white)
        {
            whiteTiles.Add(img);
        }

        return img;
    }
}
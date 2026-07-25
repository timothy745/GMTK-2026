using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ColorRenderer : MonoBehaviour
{
    private RectTransform colorContainer;
    private int gridSize = 16;
    private float cellSize = 30f;
    private float cellGap = 0f;

    private Image[,] gridCells;
    private Image[] paletteButtons;
    private RectTransform[] paletteRects;
    private Image refImage;
    private GameObject selectionRing;
    private Sprite referenceSpriteCached;

    private Color[] currentColors;
    private Color[] targetColors;
    private Color[] palette;
    private int selectedColorIndex = 0;

    public int SelectedColorIndex => selectedColorIndex;
    public Color[] CurrentColors => currentColors;
    public int GridSize => gridSize;
    public Image[,] GridCells => gridCells;
    public RectTransform[] PaletteRects { get; private set; }

    public void BuildGrid(Sprite referenceSprite, Color[] paletteColors, RectTransform container, Sprite tableSprite)
    {
        colorContainer = container;
        palette = paletteColors;
        referenceSpriteCached = referenceSprite;

        foreach (Transform child in colorContainer)
            Destroy(child.gameObject);

        colorContainer.anchorMin = new Vector2(0.5f, 0.5f);
        colorContainer.anchorMax = new Vector2(0.5f, 0.5f);
        colorContainer.pivot = new Vector2(0.5f, 0.5f);
        colorContainer.anchoredPosition = Vector2.zero;
        colorContainer.sizeDelta = new Vector2(1200f, 700f);

        CreateTableBackground(tableSprite);
        targetColors = ExtractTargetColors(referenceSprite);
        currentColors = new Color[gridSize * gridSize];
        for (int i = 0; i < currentColors.Length; i++)
            currentColors[i] = palette.Length > 0 ? palette[0] : Color.white;

        CreateGrid();
        CreatePalette();
    }

    private void CreateTableBackground(Sprite tableSprite)
    {
        GameObject table = new GameObject("TableBackground");
        table.transform.SetParent(colorContainer, false);

        RectTransform rect = table.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(colorContainer.sizeDelta.x + 200f, colorContainer.sizeDelta.y + 200f);

        Image img = table.AddComponent<Image>();
        img.raycastTarget = false;

        if (tableSprite != null)
        {
            img.sprite = tableSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }
        else
        {
            img.color = new Color(0f, 0f, 0f, 0.8f);
        }
    }

    private Color[] ExtractTargetColors(Sprite referenceSprite)
    {
        Color[] colors = new Color[gridSize * gridSize];

        if (referenceSprite == null || referenceSprite.texture == null)
        {
            for (int i = 0; i < colors.Length; i++)
                colors[i] = GetDefaultTargetColor(i);
            return colors;
        }

        Texture2D tex = referenceSprite.texture;
        Rect rect = referenceSprite.rect;

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                float sampleX = rect.x + (x + 0.5f) / gridSize * rect.width;
                float sampleY = rect.y + (y + 0.5f) / gridSize * rect.height;
                Color pixel = tex.GetPixel((int)sampleX, (int)sampleY);
                colors[y * gridSize + x] = pixel;
            }
        }

        return colors;
    }

    private Color GetDefaultTargetColor(int index)
    {
        int x = index % gridSize;
        int y = index / gridSize;
        if (x < 4 && y < 4) return Color.red;
        if (x >= 4 && y < 4) return Color.blue;
        if (x < 4 && y >= 4) return Color.green;
        return Color.yellow;
    }

    private void CreateGrid()
    {
        float totalW = gridSize * cellSize + (gridSize - 1) * cellGap;
        float totalH = gridSize * cellSize + (gridSize - 1) * cellGap;
        float startX = -225f;
        float startY = totalH / 2f - cellSize / 2f + 30f;

        GameObject refBg = new GameObject("ReferenceGuide");
        refBg.transform.SetParent(colorContainer, false);

        RectTransform refBgRect = refBg.AddComponent<RectTransform>();
        refBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        refBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        refBgRect.pivot = new Vector2(0.5f, 0.5f);
        refBgRect.anchoredPosition = new Vector2(startX + totalW / 2f - cellSize / 2f, startY - totalH / 2f + cellSize / 2f);
        refBgRect.sizeDelta = new Vector2(totalW, totalH);

        Image refBgImg = refBg.AddComponent<Image>();
        refBgImg.raycastTarget = false;

        if (referenceSpriteCached != null)
        {
            refBgImg.sprite = referenceSpriteCached;
            refBgImg.type = Image.Type.Simple;
            refBgImg.preserveAspect = true;
        }
        refBgImg.color = new Color(1f, 1f, 1f, 0.4f);

        gridCells = new Image[gridSize, gridSize];

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                GameObject cell = new GameObject("Cell_" + x + "_" + y);
                cell.transform.SetParent(colorContainer, false);

                RectTransform rect = cell.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(
                    startX + x * (cellSize + cellGap),
                    startY - y * (cellSize + cellGap)
                );
                rect.sizeDelta = new Vector2(cellSize, cellSize);

                Image img = cell.AddComponent<Image>();
                img.raycastTarget = true;
                img.color = new Color(currentColors[y * gridSize + x].r, currentColors[y * gridSize + x].g, currentColors[y * gridSize + x].b, 0.2f);

                gridCells[x, y] = img;
            }
        }

        CreateGridLines(startX, startY);
    }

private void CreateGridLines(float startX, float startY)
{
    float totalW = gridSize * cellSize;
    float totalH = gridSize * cellSize;
    Color lineColor = new Color(0.4f, 0.4f, 0.4f);

    float leftEdge = startX - cellSize / 2f;
    float topEdge = startY + cellSize / 2f;
    float lineThickness = 3f;
    float overshoot = lineThickness;

    // Calculate the actual center point of the entire grid
    float gridCenterX = startX + (totalW / 2f) - (cellSize / 2f);
    float gridCenterY = startY - (totalH / 2f) + (cellSize / 2f);

    for (int i = 0; i <= gridSize; i++)
    {
        // 1. Vertical Lines
        float vX = leftEdge + i * cellSize;

        GameObject vLine = new GameObject("VLine_" + i);
        vLine.transform.SetParent(colorContainer, false);

        RectTransform vRect = vLine.AddComponent<RectTransform>();
        vRect.anchorMin = new Vector2(0.5f, 0.5f);
        vRect.anchorMax = new Vector2(0.5f, 0.5f);
        vRect.pivot = new Vector2(0.5f, 0.5f);
        // Position X at grid column division, Y at the grid's center Y
        vRect.anchoredPosition = new Vector2(vX, gridCenterY); 
        vRect.sizeDelta = new Vector2(lineThickness, totalH + overshoot * 2f);

        Image vImg = vLine.AddComponent<Image>();
        vImg.color = lineColor;
        vImg.raycastTarget = false;

        // 2. Horizontal Lines
        float hY = topEdge - i * cellSize;

        GameObject hLine = new GameObject("HLine_" + i);
        hLine.transform.SetParent(colorContainer, false);

        RectTransform hRect = hLine.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.5f, 0.5f);
        hRect.anchorMax = new Vector2(0.5f, 0.5f);
        hRect.pivot = new Vector2(0.5f, 0.5f);
        // Position X at the grid's center X, Y at grid row division
        hRect.anchoredPosition = new Vector2(gridCenterX, hY); 
        hRect.sizeDelta = new Vector2(totalW + overshoot * 2f, lineThickness);

        Image hImg = hLine.AddComponent<Image>();
        hImg.color = lineColor;
        hImg.raycastTarget = false;
    }
}

    private void CreatePalette()
    {
        float btnSize = 50f;
        float btnGap = 14f;
        float totalW = palette.Length * btnSize + (palette.Length - 1) * btnGap;
        float startX = -totalW / 2f + btnSize / 2f;
        float posY = -280f;

        paletteButtons = new Image[palette.Length];
        PaletteRects = new RectTransform[palette.Length];
        paletteRects = new RectTransform[palette.Length];

        for (int i = 0; i < palette.Length; i++)
        {
            GameObject btn = new GameObject("Palette_" + i);
            btn.transform.SetParent(colorContainer, false);

            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(startX + i * (btnSize + btnGap), posY);
            rect.sizeDelta = new Vector2(btnSize, btnSize);

            Image img = btn.AddComponent<Image>();
            img.color = palette[i];
            img.raycastTarget = false;

            paletteButtons[i] = img;
            PaletteRects[i] = rect;
            paletteRects[i] = rect;
        }

        UpdatePaletteSelection();
    }

    private void CreateReferencePicture(Sprite referenceSprite)
    {
        float refSize = 482f;
        float posX = 422f;
        float posY = 50f;

        GameObject refObj = new GameObject("ReferencePicture");
        refObj.transform.SetParent(colorContainer, false);

        RectTransform rect = refObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(posX, posY);
        rect.sizeDelta = new Vector2(refSize, refSize);

        refImage = refObj.AddComponent<Image>();
        refImage.raycastTarget = false;

        if (referenceSprite != null)
        {
            refImage.sprite = referenceSprite;
            refImage.type = Image.Type.Simple;
            refImage.preserveAspect = true;
        }
        else
        {
            refImage.color = new Color(0.3f, 0.3f, 0.3f);
        }

        CreateRefGridLines(posX, posY, refSize);
    }

    private void CreateRefGridLines(float posX, float posY, float refSize)
    {
        Color lineColor = new Color(0.4f, 0.4f, 0.4f);
        float cellW = refSize / gridSize;
        float cellH = refSize / gridSize;
        float leftEdge = posX - refSize / 2f;
        float topEdge = posY + refSize / 2f;
        float lineThickness = 2f;

        for (int i = 0; i <= gridSize; i++)
        {
            float vX = leftEdge + i * cellW;

            GameObject vLine = new GameObject("RefVLine_" + i);
            vLine.transform.SetParent(colorContainer, false);

            RectTransform vRect = vLine.AddComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.5f, 0.5f);
            vRect.anchorMax = new Vector2(0.5f, 0.5f);
            vRect.pivot = new Vector2(0.5f, 0.5f);
            vRect.anchoredPosition = new Vector2(vX, posY);
            vRect.sizeDelta = new Vector2(lineThickness, refSize + lineThickness * 2f);

            Image vImg = vLine.AddComponent<Image>();
            vImg.color = lineColor;
            vImg.raycastTarget = false;

            float hY = topEdge - i * cellH;

            GameObject hLine = new GameObject("RefHLine_" + i);
            hLine.transform.SetParent(colorContainer, false);

            RectTransform hRect = hLine.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 0.5f);
            hRect.anchorMax = new Vector2(0.5f, 0.5f);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.anchoredPosition = new Vector2(posX, hY);
            hRect.sizeDelta = new Vector2(refSize + lineThickness * 2f, lineThickness);

            Image hImg = hLine.AddComponent<Image>();
            hImg.color = lineColor;
            hImg.raycastTarget = false;
        }
    }

    public void SelectPaletteColor(int index)
    {
        selectedColorIndex = index;
        UpdatePaletteSelection();
    }

    private void UpdatePaletteSelection()
    {
        for (int i = 0; i < paletteRects.Length; i++)
        {
            float scale = (i == selectedColorIndex) ? 1.4f : 1f;
            paletteRects[i].localScale = Vector3.one * scale;
        }
    }

    public void PaintCell(int gridX, int gridY)
    {
        if (gridX < 0 || gridX >= gridSize || gridY < 0 || gridY >= gridSize) return;
        if (selectedColorIndex < 0 || selectedColorIndex >= palette.Length) return;

        int idx = gridY * gridSize + gridX;
        Color paintColor = palette[selectedColorIndex];

        if (paintColor == Color.white)
        {
            currentColors[idx] = Color.clear;
            gridCells[gridX, gridY].color = new Color(1f, 1f, 1f, 0f);
        }
        else
        {
            currentColors[idx] = paintColor;
            gridCells[gridX, gridY].color = paintColor;
        }
    }

    public float GetMatchPercentage()
    {
        if (targetColors == null || currentColors == null) return 0f;
        if (targetColors.Length != currentColors.Length) return 0f;

        int matchCount = 0;
        int checkedCount = 0;
        for (int i = 0; i < currentColors.Length; i++)
        {
            if (IsBlack(targetColors[i]))
            {
                checkedCount++;
                if (ColorClose(currentColors[i], targetColors[i]))
                    matchCount++;
            }
        }

        if (checkedCount == 0) return 1f;
        return (float)matchCount / checkedCount;
    }

    private bool IsBlack(Color c)
    {
        return c.r < 0.2f && c.g < 0.2f && c.b < 0.2f;
    }

    private bool ColorClose(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.15f &&
               Mathf.Abs(a.g - b.g) < 0.15f &&
               Mathf.Abs(a.b - b.b) < 0.15f;
    }
}

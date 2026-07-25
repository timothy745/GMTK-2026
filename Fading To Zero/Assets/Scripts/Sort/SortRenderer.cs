using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SortRenderer : MonoBehaviour
{
    private RectTransform sortContainer;
    private float shapeSize = 65f;
    private float holeSize = 320f;
    private float holeSpacing = 380f;

    private List<Image> holeImages = new List<Image>();
    private List<Image> shapeImages = new List<Image>();
    private List<RectTransform> shapeRects = new List<RectTransform>();
    private List<int> shapeType = new List<int>();
    private int[] holeOccupancy = new int[4];
    private Dictionary<int, int> shapeToHole = new Dictionary<int, int>();

    public Vector2[] HolePositions => GetHolePositions();
    public List<RectTransform> ShapeRects => shapeRects;
    public List<int> ShapeType => shapeType;
    public Dictionary<int, int> ShapeToHole => shapeToHole;

    public void BuildBoard(Sprite[] holeSprites, Sprite[] shapeSprites, RectTransform container, Sprite tableSprite)
    {
        sortContainer = container;
        holeImages.Clear();
        shapeImages.Clear();
        shapeRects.Clear();
        shapeType.Clear();
        holeOccupancy = new int[4];
        shapeToHole.Clear();

        foreach (Transform child in sortContainer)
            Destroy(child.gameObject);

        sortContainer.anchorMin = new Vector2(0.5f, 0.5f);
        sortContainer.anchorMax = new Vector2(0.5f, 0.5f);
        sortContainer.pivot = new Vector2(0.5f, 0.5f);
        sortContainer.anchoredPosition = Vector2.zero;
        sortContainer.sizeDelta = new Vector2(1200f, 700f);

        CreateTableBackground(tableSprite);
        CreateHoles(holeSprites);
        CreateShapes(shapeSprites);
    }

    private void CreateTableBackground(Sprite tableSprite)
    {
        GameObject table = new GameObject("TableBackground");
        table.transform.SetParent(sortContainer, false);

        RectTransform rect = table.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(sortContainer.sizeDelta.x + 200f, sortContainer.sizeDelta.y + 200f);

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
            img.color = new Color(0.35f, 0.25f, 0.15f);
        }
    }

    private void CreateHoles(Sprite[] holeSprites)
    {
        Vector2[] positions = GetHolePositions();

        for (int i = 0; i < 4; i++)
        {
            GameObject hole = new GameObject("Hole_" + i);
            hole.transform.SetParent(sortContainer, false);

            RectTransform rect = hole.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = positions[i];
            rect.sizeDelta = new Vector2(holeSize, holeSize);

            Image img = hole.AddComponent<Image>();
            img.raycastTarget = false;

            if (holeSprites != null && i < holeSprites.Length && holeSprites[i] != null)
            {
                img.sprite = holeSprites[i];
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.15f, 0.15f, 0.15f);
            }

            holeImages.Add(img);
        }
    }

    private void CreateShapes(Sprite[] shapeSprites)
    {
        float areaMinX = 200f;
        float areaMaxX = 520f;
        float areaMinY = -280f;
        float areaMaxY = 280f;

        List<int> spawnOrder = new List<int>();
        for (int shape = 0; shape < 4; shape++)
            for (int copy = 0; copy < 6; copy++)
                spawnOrder.Add(shape);

        for (int i = spawnOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = spawnOrder[i];
            spawnOrder[i] = spawnOrder[j];
            spawnOrder[j] = temp;
        }

        for (int i = 0; i < spawnOrder.Count; i++)
        {
            int type = spawnOrder[i];
            shapeType.Add(type);

            float posX = Random.Range(areaMinX, areaMaxX);
            float posY = Random.Range(areaMinY, areaMaxY);

            GameObject shape = new GameObject("Shape_" + type + "_" + i);
            shape.transform.SetParent(sortContainer, false);

            RectTransform rect = shape.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(posX, posY);
            rect.sizeDelta = new Vector2(shapeSize, shapeSize);
            rect.rotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));

            Image img = shape.AddComponent<Image>();
            img.raycastTarget = true;

            if (shapeSprites != null && type < shapeSprites.Length && shapeSprites[type] != null)
            {
                img.sprite = shapeSprites[type];
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            else
            {
                img.color = Color.white;
            }

            shapeImages.Add(img);
            shapeRects.Add(rect);
        }
    }

    public int GetHoleIndex(Vector2 localPos)
    {
        Vector2[] positions = GetHolePositions();
        float matchRadius = holeSize * 0.6f;

        for (int i = 0; i < positions.Length; i++)
        {
            if (Vector2.Distance(localPos, positions[i]) < matchRadius)
                return i;
        }
        return -1;
    }

    public bool CheckAllSorted(List<Vector2Int> placements)
    {
        if (placements.Count < shapeType.Count) return false;

        for (int i = 0; i < placements.Count; i++)
        {
            int shapeIdx = placements[i].x;
            int holeIdx = placements[i].y;
            if (shapeType[shapeIdx] != holeIdx)
                return false;
        }
        return true;
    }

    public void SnapShapeToHole(int shapeIndex, int holeIndex)
    {
        if (shapeIndex < 0 || shapeIndex >= shapeRects.Count) return;
        if (holeIndex < 0 || holeIndex >= holeImages.Count) return;

        if (shapeToHole.ContainsKey(shapeIndex))
        {
            int prevHole = shapeToHole[shapeIndex];
            holeOccupancy[prevHole]--;
            shapeToHole.Remove(shapeIndex);
        }

        holeOccupancy[holeIndex]++;
        shapeToHole[shapeIndex] = holeIndex;

        float[][] offsets = new float[][] {
            new float[] { -35f, 25f },
            new float[] { 30f, -20f },
            new float[] { -15f, -35f },
            new float[] { 35f, 20f },
            new float[] { -40f, 5f },
            new float[] { 20f, 35f }
        };

        int slot = Mathf.Min(holeOccupancy[holeIndex] - 1, offsets.Length - 1);

        float slotX = offsets[slot][0] + Random.Range(-12f, 12f);
        float slotY = offsets[slot][1] + Random.Range(-12f, 12f);

        Vector2[] positions = GetHolePositions();
        shapeRects[shapeIndex].anchoredPosition = positions[holeIndex] + new Vector2(slotX, slotY);
        shapeRects[shapeIndex].rotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f));
    }

    public void RemoveShapeFromHole(int shapeIndex)
    {
        if (!shapeToHole.ContainsKey(shapeIndex)) return;

        int holeIdx = shapeToHole[shapeIndex];
        holeOccupancy[holeIdx]--;
        shapeToHole.Remove(shapeIndex);
    }

    public int GetRandomPlacedShape()
    {
        List<int> placed = new List<int>(shapeToHole.Keys);
        if (placed.Count == 0) return -1;
        return placed[Random.Range(0, placed.Count)];
    }

    public bool IsAnyKnockingOut { get; set; }

    public void SetHoleWarningColor(int holeIndex, Color color)
    {
        if (holeIndex < 0 || holeIndex >= holeImages.Count) return;
        holeImages[holeIndex].color = color;
    }

    public void SetHoleGlow(int holeIndex, bool active)
    {
        if (holeIndex < 0 || holeIndex >= holeImages.Count) return;

        if (active)
            holeImages[holeIndex].color = new Color(0.5f, 1f, 0.5f);
        else
            holeImages[holeIndex].color = Color.white;
    }

    public void SetTileColors(Color color)
    {
        foreach (var img in holeImages)
        {
            if (img != null)
                img.color = color;
        }
    }

    private Vector2[] GetHolePositions()
    {
        float offsetX = -220f;
        float offsetY = 20f;

        return new Vector2[]
        {
            new Vector2(offsetX - holeSpacing * 0.5f, offsetY + holeSpacing * 0.5f),
            new Vector2(offsetX + holeSpacing * 0.5f, offsetY + holeSpacing * 0.5f),
            new Vector2(offsetX - holeSpacing * 0.5f, offsetY - holeSpacing * 0.5f),
            new Vector2(offsetX + holeSpacing * 0.5f, offsetY - holeSpacing * 0.5f)
        };
    }
}

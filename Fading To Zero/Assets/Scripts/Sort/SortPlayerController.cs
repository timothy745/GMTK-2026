using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class SortPlayerController : MonoBehaviour
{
    private SortRenderer sortRenderer;
    private SortManager sortManager;
    private RectTransform playerRect;
    private bool isActive;

    private int draggedShapeIndex = -1;
    private Vector2 dragOffset;
    private Camera mainCam;

    private List<Vector2Int> placements = new List<Vector2Int>();

    public void Initialize(SortRenderer renderer, SortManager manager)
    {
        sortRenderer = renderer;
        sortManager = manager;
        isActive = true;
        placements.Clear();

        mainCam = Camera.main;

        playerRect = GetComponent<RectTransform>();
        if (playerRect == null)
            playerRect = gameObject.AddComponent<RectTransform>();

        playerRect.sizeDelta = Vector2.zero;
        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.pivot = new Vector2(0.5f, 0.5f);
        playerRect.anchoredPosition = Vector2.zero;

        Image img = GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }

    public void Deactivate()
    {
        isActive = false;
        draggedShapeIndex = -1;
    }

    public void RemovePlacement(int shapeIndex)
    {
        placements.RemoveAll(e => e.x == shapeIndex);
    }

    void Update()
    {
        if (!isActive) return;
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        bool mouseDown = Mouse.current.leftButton.wasPressedThisFrame;
        bool mouseHeld = Mouse.current.leftButton.isPressed;
        bool mouseUp = Mouse.current.leftButton.wasReleasedThisFrame;

        if (mouseDown)
            TryStartDrag(mousePos);

        if (mouseHeld && draggedShapeIndex >= 0)
            ContinueDrag(mousePos);

        if (mouseUp && draggedShapeIndex >= 0)
            EndDrag(mousePos);
    }

    private void TryStartDrag(Vector2 mousePos)
    {
        Vector2 localPos = ScreenToLocal(mousePos);

        for (int i = sortRenderer.ShapeRects.Count - 1; i >= 0; i--)
        {
            RectTransform rect = sortRenderer.ShapeRects[i];
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
            {
                draggedShapeIndex = i;
                dragOffset = rect.anchoredPosition - localPos;
                rect.SetAsLastSibling();
                break;
            }
        }
    }

    private void ContinueDrag(Vector2 mousePos)
    {
        Vector2 localPos = ScreenToLocal(mousePos);
        sortRenderer.ShapeRects[draggedShapeIndex].anchoredPosition = localPos + dragOffset;

        Vector2[] holePositions = sortRenderer.HolePositions;
        for (int i = 0; i < holePositions.Length; i++)
        {
            float dist = Vector2.Distance(localPos + dragOffset, holePositions[i]);
            sortRenderer.SetHoleGlow(i, dist < sortRenderer.MatchRadius);
        }
    }

    private void EndDrag(Vector2 mousePos)
    {
        Vector2 localPos = ScreenToLocal(mousePos);
        Vector2 shapePos = localPos + dragOffset;

        int closestHole = -1;
        float closestDist = float.MaxValue;

        Vector2[] holePositions = sortRenderer.HolePositions;
        for (int i = 0; i < holePositions.Length; i++)
        {
            float dist = Vector2.Distance(shapePos, holePositions[i]);
            if (dist < sortRenderer.MatchRadius && dist < closestDist)
            {
                closestDist = dist;
                closestHole = i;
            }
        }

        for (int i = 0; i < holePositions.Length; i++)
            sortRenderer.SetHoleGlow(i, false);

        if (closestHole >= 0)
        {
            placements.RemoveAll(e => e.x == draggedShapeIndex);
            placements.Add(new Vector2Int(draggedShapeIndex, closestHole));
            sortRenderer.SnapShapeToHole(draggedShapeIndex, closestHole);

            if (sortRenderer.CheckAllSorted(placements))
            {
                sortManager.SortCompleted();
            }
        }
        else
        {
            sortRenderer.RemoveShapeFromHole(draggedShapeIndex);
            placements.RemoveAll(e => e.x == draggedShapeIndex);
        }

        draggedShapeIndex = -1;
    }

    private Vector2 ScreenToLocal(Vector2 screenPos)
    {
        RectTransform container = sortRenderer.ShapeRects[0].parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPos, null, out localPoint);
        return localPoint;
    }
}

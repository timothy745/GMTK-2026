using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MazePlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 600f;

    private MazeGenerator generator;
    private MazeManager mazeManager;
    private RectTransform playerRect;
    private int gridX, gridY;
    private int mazeWidth = 16;
    private int mazeHeight = 16;
    private bool isActive;
    private bool reachedExit;
    private float cellSize = 48f;

    private Stack<Vector2Int> visitOrder = new Stack<Vector2Int>();
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private Stack<GameObject> lineSegments = new Stack<GameObject>();
    public RectTransform TrailContainer { get; private set; }

    private float playerDotSize = 24f;
    private float trailThickness = 12f;

    private float moveTimer;
    private float moveDelay = 0.08f;
    private float repeatDelay = 0.12f;
    private bool holdingKey;
    private Vector2Int lastDir;

    public void Initialize(MazeGenerator gen, MazeManager manager, Vector2Int startPos, int width, int height)
    {
        generator = gen;
        mazeManager = manager;
        mazeWidth = width;
        mazeHeight = height;

        playerRect = GetComponent<RectTransform>();
        if (playerRect == null)
            playerRect = gameObject.AddComponent<RectTransform>();

        playerRect.sizeDelta = new Vector2(playerDotSize, playerDotSize);
        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.pivot = new Vector2(0.5f, 0.5f);

        Image playerImg = GetComponent<Image>();
        if (playerImg != null) playerImg.color = Color.white;

        ResetToStart();
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public void ResetMaze()
    {
        ResetToStart();
    }

    private void ResetToStart()
    {
        gridX = 0;
        gridY = 0;
        isActive = true;
        reachedExit = false;
        moveTimer = 0f;
        holdingKey = false;
        lastDir = Vector2Int.zero;

        ClearTrail();
        CreateTrailContainer();

        Vector2Int start = new Vector2Int(0, 0);
        visitOrder.Push(start);
        visitedCells.Add(start);

        UpdateVisualPosition();
    }

    public bool HasReachedExit()
    {
        return reachedExit;
    }

    void Update()
    {
        if (!isActive) return;

        Vector2Int dir = GetInputDirection();

        if (dir == Vector2Int.zero)
        {
            holdingKey = false;
            moveTimer = 0f;
            return;
        }

        if (dir == lastDir && holdingKey)
        {
            moveTimer -= Time.deltaTime;
            if (moveTimer > 0f) return;
        }

        lastDir = dir;
        holdingKey = true;
        moveTimer = repeatDelay;

        Vector2Int newPos = new Vector2Int(gridX + dir.x, gridY + dir.y);

        if (newPos.x == gridX && newPos.y == gridY) return;

        if (IsPassage(gridX, gridY, newPos.x, newPos.y))
        {
            Vector2Int from = new Vector2Int(gridX, gridY);
            gridX = newPos.x;
            gridY = newPos.y;
            UpdateVisualPosition();

            Vector2Int current = new Vector2Int(gridX, gridY);

            if (visitedCells.Contains(current))
            {
                while (visitOrder.Count > 0 && visitOrder.Peek() != current)
                {
                    Vector2Int top = visitOrder.Pop();
                    visitedCells.Remove(top);
                    DestroyLineSegment();
                }
            }
            else
            {
                visitedCells.Add(current);
                visitOrder.Push(current);
                CreateLineSegment(from, current);
            }

            if (gridX == mazeManager.GetExitPosition().x && gridY == mazeManager.GetExitPosition().y)
            {
                reachedExit = true;
                mazeManager.MazeCompleted();
            }
        }
        else
        {
            moveTimer = moveDelay;
        }
    }

    private Vector2Int GetInputDirection()
    {
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        if (up && !down) return Vector2Int.up;
        if (down && !up) return Vector2Int.down;
        if (left && !right) return Vector2Int.left;
        if (right && !left) return Vector2Int.right;

        return Vector2Int.zero;
    }

    private bool IsPassage(int x1, int y1, int x2, int y2)
    {
        if (x2 < 0 || x2 >= mazeWidth || y2 < 0 || y2 >= mazeHeight)
            return false;

        return !generator.IsWallBetween(x1, y1, x2, y2);
    }

    private void UpdateVisualPosition()
    {
        float offsetX = -(mazeWidth * cellSize) / 2f + cellSize / 2f;
        float offsetY = -(mazeHeight * cellSize) / 2f + cellSize / 2f;
        playerRect.anchoredPosition = new Vector2(gridX * cellSize + offsetX, gridY * cellSize + offsetY);
    }

    private Vector2 GetCellAnchoredPosition(int x, int y)
    {
        float offsetX = -(mazeWidth * cellSize) / 2f + cellSize / 2f;
        float offsetY = -(mazeHeight * cellSize) / 2f + cellSize / 2f;
        return new Vector2(x * cellSize + offsetX, y * cellSize + offsetY);
    }

    // --- Trail Lines ---

    private void CreateTrailContainer()
    {
        GameObject container = new GameObject("TrailContainer");
        container.transform.SetParent(playerRect.parent, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        TrailContainer = rect;
    }

    private void CreateLineSegment(Vector2Int from, Vector2Int to)
    {
        Vector2 fromPos = GetCellAnchoredPosition(from.x, from.y);
        Vector2 toPos = GetCellAnchoredPosition(to.x, to.y);
        Vector2 mid = (fromPos + toPos) / 2f;

        GameObject line = new GameObject("Line_" + from.x + "_" + from.y + "_to_" + to.x + "_" + to.y);
        line.transform.SetParent(TrailContainer, false);

        RectTransform rect = line.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = mid;

        float dx = Mathf.Abs(toPos.x - fromPos.x);
        float dy = Mathf.Abs(toPos.y - fromPos.y);

        if (dx > dy)
            rect.sizeDelta = new Vector2(cellSize + trailThickness, trailThickness);
        else
            rect.sizeDelta = new Vector2(trailThickness, cellSize + trailThickness);

        Image img = line.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;

        lineSegments.Push(line);
    }

    private void DestroyLineSegment()
    {
        if (lineSegments.Count > 0)
        {
            GameObject line = lineSegments.Pop();
            if (line != null)
                Destroy(line);
        }
    }

    public void ClearTrail()
    {
        visitOrder.Clear();
        visitedCells.Clear();

        while (lineSegments.Count > 0)
        {
            GameObject line = lineSegments.Pop();
            if (line != null)
                Destroy(line);
        }

        if (TrailContainer != null)
        {
            Destroy(TrailContainer.gameObject);
            TrailContainer = null;
        }
    }
}

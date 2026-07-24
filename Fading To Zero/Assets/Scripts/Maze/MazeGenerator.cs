using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    public struct Cell
    {
        public bool top, bottom, left, right;
        public bool visited;
    }

    private int width;
    private int height;
    private Cell[,] grid;

    public Cell[,] GenerateMaze(int w, int h)
    {
        width = w;
        height = h;
        grid = new Cell[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = new Cell { top = true, bottom = true, left = true, right = true, visited = false };

        CarvePassagesFrom(0, 0);

        return grid;
    }

    private void CarvePassagesFrom(int cx, int cy)
    {
        grid[cx, cy].visited = true;

        List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }

        foreach (Vector2Int dir in directions)
        {
            int nx = cx + dir.x;
            int ny = cy + dir.y;

            if (IsInsideGrid(nx, ny) && !grid[nx, ny].visited)
            {
                if (dir.x == 1)  { grid[cx, cy].right = false; grid[nx, ny].left = false; }
                if (dir.x == -1) { grid[cx, cy].left = false;  grid[nx, ny].right = false; }
                if (dir.y == 1)  { grid[cx, cy].top = false;   grid[nx, ny].bottom = false; }
                if (dir.y == -1) { grid[cx, cy].bottom = false; grid[nx, ny].top = false; }

                CarvePassagesFrom(nx, ny);
            }
        }
    }

    private void RemoveRandomWalls(int count)
    {
        int removed = 0;
        int attempts = 0;
        int maxAttempts = count * 10;

        while (removed < count && attempts < maxAttempts)
        {
            attempts++;
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);
            int dir = Random.Range(0, 4);

            int nx = x + (dir == 1 ? 1 : dir == 3 ? -1 : 0);
            int ny = y + (dir == 0 ? 1 : dir == 2 ? -1 : 0);

            if (!IsInsideGrid(nx, ny)) continue;

            if (dir == 0 && grid[x, y].top)    { grid[x, y].top = false;    grid[nx, ny].bottom = false; removed++; }
            else if (dir == 1 && grid[x, y].right)  { grid[x, y].right = false;  grid[nx, ny].left = false;   removed++; }
            else if (dir == 2 && grid[x, y].bottom) { grid[x, y].bottom = false; grid[nx, ny].top = false;    removed++; }
            else if (dir == 3 && grid[x, y].left)   { grid[x, y].left = false;   grid[nx, ny].right = false;  removed++; }
        }
    }

    private bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool HasWall(int x, int y, string direction)
    {
        if (!IsInsideGrid(x, y)) return true;

        switch (direction)
        {
            case "top":    return grid[x, y].top;
            case "bottom": return grid[x, y].bottom;
            case "left":   return grid[x, y].left;
            case "right":  return grid[x, y].right;
            default:       return true;
        }
    }

    public bool IsWallBetween(int x1, int y1, int x2, int y2)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;

        if (dx == 1)  return grid[x1, y1].right;
        if (dx == -1) return grid[x1, y1].left;
        if (dy == 1)  return grid[x1, y1].top;
        if (dy == -1) return grid[x1, y1].bottom;

        return true;
    }
}

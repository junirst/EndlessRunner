using System.Collections.Generic;
using UnityEngine;

public class Level4MapGenerator : MonoBehaviour
{
    #region Inspector Fields

    [Header("Prefabs")]
    [SerializeField] private GameObject straightWallPrefab;
    [SerializeField] private GameObject lShapeWallPrefab;

    [Header("Grid")]
    [SerializeField] private BoxCollider2D gridArea;
    [SerializeField] private Food food;
    [SerializeField] private Transform wallsParent;
    [SerializeField] private float cellSize = 2f;

    #endregion

    #region Tuning

    [Header("Cellular Automata")]
    [SerializeField, Range(0.3f, 0.6f)] private float initialFillProbability = 0.42f;
    [SerializeField, Range(1, 10)] private int smoothIterations = 4;

    [Header("Spawn Safety")]
    [SerializeField, Min(1)] private int spawnSafetyRadius = 3;
    [SerializeField, Min(1)] private int edgeClearance = 1;

    [Header("Maze Validation")]
    [SerializeField, Min(1)] private int maxPlacementAttempts = 50;
    [SerializeField, Min(1)] private int minReachableCells = 80;
    [SerializeField] private bool persistSeed = true;

    private const string SeedPrefKey = "SnakeLevel4LastSeed";

    #endregion

    #region State

    private System.Random rng;
    private readonly List<GameObject> instantiatedWalls = new List<GameObject>();

    #endregion

    #region Lifecycle

    private void Start()
    {
        AutoConfigure();

        if (straightWallPrefab == null)
            straightWallPrefab = Resources.Load<GameObject>("StraigthWall");
        if (lShapeWallPrefab == null)
            lShapeWallPrefab = Resources.Load<GameObject>("L-ShapeWall");

        if (straightWallPrefab == null || lShapeWallPrefab == null)
        {
            Debug.LogError("[Level4MapGenerator] Wall prefabs not assigned and could not load from Resources.");
            return;
        }

        int seed = Random.Range(int.MinValue, int.MaxValue);
        int lastSeed = persistSeed ? PlayerPrefs.GetInt(SeedPrefKey, int.MinValue) : int.MinValue;
        if (seed == lastSeed)
            seed += 1;

        rng = new System.Random(seed);

        bool placed = TryGenerateMaze();

        if (!placed)
        {
            ClearInstantiatedWalls();
            Debug.LogWarning($"[Level4MapGenerator] {gameObject.scene.name}: CA placement failed after {maxPlacementAttempts} attempts (seed {seed}).");
        }
        else
        {
            Debug.Log($"[Level4MapGenerator] {gameObject.scene.name}: built cellular automata cave (seed {seed}).");
        }

        if (persistSeed)
            PlayerPrefs.SetInt(SeedPrefKey, seed);

        if (food != null)
            food.ApplyRandomWeights(BuildRandomWeights());
    }

    #endregion

    #region Auto-Configuration

    private void AutoConfigure()
    {
        if (food == null)
            food = FindObjectOfType<Food>();

        if (gridArea == null)
        {
            BoxCollider2D[] allColliders = FindObjectsOfType<BoxCollider2D>();
            float maxArea = 0f;
            foreach (BoxCollider2D col in allColliders)
            {
                if (col.isTrigger) continue;
                float area = col.size.x * col.size.y;
                if (area > maxArea)
                {
                    maxArea = area;
                    gridArea = col;
                }
            }
        }

        if (wallsParent == null)
        {
            GameObject wallsGO = GameObject.Find("Walls");
            if (wallsGO != null)
                wallsParent = wallsGO.transform;
        }
    }

    #endregion

    #region Maze Generation

    private bool TryGenerateMaze()
    {
        if (gridArea == null || straightWallPrefab == null || lShapeWallPrefab == null)
            return false;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            ClearInstantiatedWalls();

            Bounds bounds = gridArea.bounds;
            int width = Mathf.RoundToInt(bounds.size.x / cellSize);
            int height = Mathf.RoundToInt(bounds.size.y / cellSize);
            int x0 = Mathf.RoundToInt(bounds.min.x / cellSize);
            int y0 = Mathf.RoundToInt(bounds.min.y / cellSize);

            bool[,] grid = InitializeGrid(width, height);

            for (int i = 0; i < smoothIterations; i++)
                grid = SmoothGrid(grid, width, height);

            ClearSpawnZone(grid, x0, y0, width, height);
            ClearGridEdges(grid, width, height);

            if (!IsCaveNavigable(grid, x0, y0, width, height))
                continue;

            InstantiateWalls(grid, x0, y0, width, height);
            return true;
        }
        return false;
    }

    private bool[,] InitializeGrid(int width, int height)
    {
        bool[,] grid = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = rng.NextDouble() < initialFillProbability;
        return grid;
    }

    private bool[,] SmoothGrid(bool[,] grid, int width, int height)
    {
        bool[,] next = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int walls = CountWallNeighbors(grid, x, y, width, height);
                next[x, y] = walls >= 5;
            }
        }
        return next;
    }

    private int CountWallNeighbors(bool[,] grid, int cx, int cy, int width, int height)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = cx + dx;
                int ny = cy + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    count++;
                else if (grid[nx, ny])
                    count++;
            }
        }
        return count;
    }

    private void ClearSpawnZone(bool[,] grid, int x0, int y0, int width, int height)
    {
        for (int dx = -spawnSafetyRadius; dx <= spawnSafetyRadius; dx++)
        {
            for (int dy = -spawnSafetyRadius; dy <= spawnSafetyRadius; dy++)
            {
                int gx = -x0 + dx;
                int gy = -y0 + dy;
                if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                    grid[gx, gy] = false;
            }
        }
    }

    private void ClearGridEdges(bool[,] grid, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < edgeClearance || x >= width - edgeClearance ||
                    y < edgeClearance || y >= height - edgeClearance)
                {
                    grid[x, y] = false;
                }
            }
        }
    }

    #endregion

    #region Navigability Check

    private bool IsCaveNavigable(bool[,] grid, int x0, int y0, int width, int height)
    {
        Vector2Int head = new Vector2Int(-x0, -y0);
        HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
        reachable.Add(head);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(head);

        Vector2Int[] dirs =
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = current + dir;
                if (reachable.Contains(next)) continue;

                int gx = next.x + x0;
                int gy = next.y + y0;
                if (gx < 0 || gx >= width || gy < 0 || gy >= height) continue;
                if (grid[gx, gy]) continue;

                reachable.Add(next);
                queue.Enqueue(next);
            }
        }

        return reachable.Count >= minReachableCells;
    }

    #endregion

    #region Wall Instantiation

    private void InstantiateWalls(bool[,] grid, int x0, int y0, int width, int height)
    {
        HashSet<Vector2Int> processed = new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y]) continue;

                Vector2Int cell = new Vector2Int(x, y);
                if (processed.Contains(cell)) continue;

                if (TryPlaceStraight(grid, x, y, x0, y0, width, height, processed))
                    continue;

                TryPlaceLShape(grid, x, y, x0, y0, width, height, processed);
            }
        }
    }

    /// <summary>
    /// Checks for a straight run of 3 wall cells starting at (cx, cy) going in
    /// the given direction. If found, places the straight wall prefab and marks
    /// all 3 cells as processed.
    /// </summary>
    private bool TryPlaceStraight(bool[,] grid, int cx, int cy, int x0, int y0,
        int width, int height, HashSet<Vector2Int> processed)
    {
        Vector2Int[] offsets = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
        float[] rotations = { 0f, 90f, 180f, 270f };

        for (int d = 0; d < 4; d++)
        {
            Vector2Int o = offsets[d];
            Vector2Int c1 = new Vector2Int(cx + o.x, cy + o.y);
            Vector2Int c2 = new Vector2Int(cx + o.x * 2, cy + o.y * 2);

            if (!InBounds(c1, width, height) || !InBounds(c2, width, height))
                continue;
            if (!grid[c1.x, c1.y] || !grid[c2.x, c2.y])
                continue;
            if (processed.Contains(c1) || processed.Contains(c2))
                continue;

            Vector3 pos = GridToWorld(cx, cy, x0, y0);
            SpawnPrefab(straightWallPrefab, pos, rotations[d]);
            processed.Add(new Vector2Int(cx, cy));
            processed.Add(c1);
            processed.Add(c2);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks for an L-shaped cluster of 3 wall cells in a 2x2 block where one
    /// corner is missing. Places the L-shape prefab at the corner cell (the one
    /// adjacent to both arms) and rotates to match the orientation.
    /// </summary>
    private bool TryPlaceLShape(bool[,] grid, int cx, int cy, int x0, int y0,
        int width, int height, HashSet<Vector2Int> processed)
    {
        // The 4 possible L orientations, defined as (dx,dy) offsets of the two
        // arm cells relative to the corner cell, plus the rotation for the prefab.
        // Missing corner pattern: the corner cell + two arms, the 4th cell in the
        // 2x2 block is open.
        (int dx1, int dy1, int dx2, int dy2, float rot)[] patterns =
        {
            (1, 0, 0, 1, 0f),      // corner + right + up    → missing bottom-right? no...
            (1, 0, 0, -1, 90f),    // corner + right + down
            (-1, 0, 0, 1, 270f),   // corner + left + up
            (-1, 0, 0, -1, 180f),  // corner + left + down
        };

        foreach (var p in patterns)
        {
            Vector2Int a1 = new Vector2Int(cx + p.dx1, cy + p.dy1);
            Vector2Int a2 = new Vector2Int(cx + p.dx2, cy + p.dy2);

            if (!InBounds(a1, width, height) || !InBounds(a2, width, height))
                continue;
            if (!grid[a1.x, a1.y] || !grid[a2.x, a2.y])
                continue;
            if (processed.Contains(a1) || processed.Contains(a2))
                continue;

            Vector3 pos = GridToWorld(cx, cy, x0, y0);
            SpawnPrefab(lShapeWallPrefab, pos, p.rot);
            processed.Add(new Vector2Int(cx, cy));
            processed.Add(a1);
            processed.Add(a2);
            return true;
        }
        return false;
    }

    private Vector3 GridToWorld(int gx, int gy, int x0, int y0)
    {
        return new Vector3((gx + x0) * cellSize, (gy + y0) * cellSize, 0f);
    }

    private bool InBounds(Vector2Int cell, int width, int height)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    private void SpawnPrefab(GameObject prefab, Vector3 position, float rotationZ)
    {
        if (prefab == null) return;
        GameObject wall = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, rotationZ));
        wall.tag = "Obstacle";
        if (wallsParent != null)
            wall.transform.SetParent(wallsParent);
        instantiatedWalls.Add(wall);
    }

    private void ClearInstantiatedWalls()
    {
        for (int i = 0; i < instantiatedWalls.Count; i++)
        {
            if (instantiatedWalls[i] != null)
                Destroy(instantiatedWalls[i]);
        }
        instantiatedWalls.Clear();
    }

    #endregion

    #region Food Weights

    private float[] BuildRandomWeights()
    {
        float normal = NextFloat(60f, 120f);
        float golden = NextFloat(2f, 12f);
        float speed = NextFloat(4f, 16f);
        float slow = NextFloat(2f, 12f);
        float shrink = NextFloat(3f, 14f);
        float multiplier = NextFloat(4f, 16f);

        return new float[] { normal, golden, speed, slow, shrink, multiplier };
    }

    private float NextFloat(float min, float max)
    {
        return (float)(rng.NextDouble() * (max - min) + min);
    }

    #endregion
}

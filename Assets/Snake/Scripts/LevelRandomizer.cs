using System.Collections.Generic;
using UnityEngine;

public class LevelRandomizer : MonoBehaviour
{
    #region Inspector Fields

    [SerializeField] private BoxCollider2D gridArea;
    [SerializeField] private Food food;
    [SerializeField] private Transform wallsParent;
    [SerializeField] private float cellSize = 2f;

    // The interior wall transforms (frame walls are excluded by name).
    [SerializeField] private Transform[] wallTransforms;

    #endregion

    #region Tuning

    [Header("Maze generation")]
    [SerializeField, Min(1)] private int maxPlacementAttempts = 100;
    [SerializeField, Min(1)] private int minReachableCells = 80;
    [SerializeField] private bool persistSeed = true;

    private const string SeedPrefKey = "SnakeLevel4LastSeed";

    #endregion

    #region Wall Layout

    private System.Random rng;
    private Vector3[] originalPositions;

    private void Awake()
    {
        if (wallTransforms == null || wallTransforms.Length == 0)
            CacheWallTransforms();

        if (wallTransforms != null && wallTransforms.Length > 0)
        {
            originalPositions = new Vector3[wallTransforms.Length];
            for (int i = 0; i < wallTransforms.Length; i++)
                originalPositions[i] = wallTransforms[i].position;
        }
    }

    private void Start()
    {
        // Runs in the same frame as Snake.Start/Food.Start, before Food's
        // coroutine respawns next frame, so walls and weights are settled
        // before the first food placement and the obstacle-cell cache.
        int seed = Random.Range(int.MinValue, int.MaxValue);
        int lastSeed = persistSeed ? PlayerPrefs.GetInt(SeedPrefKey, int.MinValue) : int.MinValue;
        if (seed == lastSeed)
            seed += 1;

        rng = new System.Random(seed);

        bool placed = BuildRandomMaze();
        if (!placed)
        {
            RestoreOriginalLayout();
            Debug.LogWarning($"[LevelRandomizer] {gameObject.scene.name}: maze placement failed after {maxPlacementAttempts} attempts, fell back to serialized layout (seed {seed}).");
        }
        else
        {
            Debug.Log($"[LevelRandomizer] {gameObject.scene.name}: built randomized maze (seed {seed}).");
        }

        if (persistSeed)
            PlayerPrefs.SetInt(SeedPrefKey, seed);

        if (food != null)
            food.ApplyRandomWeights(BuildRandomWeights());
    }

    #endregion

    #region Wall Discovery

    private void CacheWallTransforms()
    {
        if (wallsParent == null)
            return;

        List<Transform> walls = new List<Transform>();
        foreach (Transform child in wallsParent)
        {
            if (!child.CompareTag("Obstacle")) continue;
            if (IsFrameWall(child)) continue;
            walls.Add(child);
        }
        wallTransforms = walls.ToArray();
    }

    private bool IsFrameWall(Transform wall)
    {
        string name = wall.name;
        return name == "Wall"
            || name == "Wall (1)"
            || name == "Wall (2)"
            || name == "Wall (3)";
    }

    #endregion

    #region Maze Generation

    private bool BuildRandomMaze()
    {
        if (wallTransforms == null || wallTransforms.Length == 0 || gridArea == null)
            return false;

        Bounds bounds = gridArea.bounds;
        float cs = cellSize;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            if (TryPlaceRandomLayout(bounds, cs))
                return true;
        }
        return false;
    }

    private bool TryPlaceRandomLayout(Bounds bounds, float cs)
    {
        for (int i = 0; i < wallTransforms.Length; i++)
        {
            Transform wall = wallTransforms[i];
            if (wall == null) return false;

            Vector3 scale = wall.localScale;
            bool vertical = scale.y >= scale.x;

            float axisHalf = (vertical ? Mathf.Abs(scale.y) : Mathf.Abs(scale.x)) * 0.5f;
            float perpHalf = (vertical ? Mathf.Abs(scale.x) : Mathf.Abs(scale.y)) * 0.5f;

            float minX = bounds.min.x + cs + perpHalf;
            float maxX = bounds.max.x - cs - perpHalf;
            float minY = bounds.min.y + cs + axisHalf;
            float maxY = bounds.max.y - cs - axisHalf;

            if (maxX < minX || maxY < minY)
                return false;

            float x = (float)(rng.NextDouble() * (maxX - minX) + minX);
            float y = (float)(rng.NextDouble() * (maxY - minY) + minY);

            wall.position = new Vector3(x, y, wall.position.z);
        }

        return IsMazeNavigable(bounds, cs);
    }

    private bool IsMazeNavigable(Bounds bounds, float cs)
    {
        int x0 = Mathf.FloorToInt(bounds.min.x / cs);
        int x1 = Mathf.CeilToInt(bounds.max.x / cs);
        int y0 = Mathf.FloorToInt(bounds.min.y / cs);
        int y1 = Mathf.CeilToInt(bounds.max.y / cs);

        HashSet<Vector2Int> obstacleCells = new HashSet<Vector2Int>();
        for (int cx = x0; cx <= x1; cx++)
        {
            for (int cy = y0; cy <= y1; cy++)
            {
                if (OverlapsObstacle(new Vector2(cx * cs, cy * cs), cs * 0.9f))
                    obstacleCells.Add(new Vector2Int(cx, cy));
            }
        }

        // Snake head spawns at cell (0,0), body extends left along the start
        // row; a wall on any of these cells means instant death.
        Vector2Int[] spawnCells =
        {
            new Vector2Int(0, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(-3, 0)
        };
        foreach (Vector2Int cell in spawnCells)
        {
            if (obstacleCells.Contains(cell))
                return false;
        }

        Vector2Int head = spawnCells[0];
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
                if (obstacleCells.Contains(next)) continue;
                if (next.x < x0 || next.x > x1 || next.y < y0 || next.y > y1) continue;
                reachable.Add(next);
                queue.Enqueue(next);
            }
        }

        return reachable.Count >= minReachableCells;
    }

    private bool OverlapsObstacle(Vector2 center, float size)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, Vector2.one * size, 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Obstacle"))
                return true;
        }
        return false;
    }

    private void RestoreOriginalLayout()
    {
        if (wallTransforms == null || originalPositions == null)
            return;

        for (int i = 0; i < wallTransforms.Length; i++)
        {
            if (wallTransforms[i] != null)
                wallTransforms[i].position = originalPositions[i];
        }
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

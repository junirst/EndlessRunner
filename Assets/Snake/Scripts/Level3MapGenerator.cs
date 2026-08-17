using System.Collections.Generic;
using UnityEngine;

public class Level3MapGenerator : MonoBehaviour
{
    #region Inspector Fields

    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private BoxCollider2D gridArea;
    [SerializeField] private Food food;
    [SerializeField] private Transform wallsParent;
    [SerializeField] private float cellSize = 2f;

    #endregion

    #region Tuning

    [Header("Wall Clusters")]
    [SerializeField, Min(1)] private int minClusters = 4;
    [SerializeField, Min(1)] private int maxClusters = 6;
    [SerializeField, Min(1)] private int minWallsPerCluster = 2;
    [SerializeField, Min(2)] private int maxWallsPerCluster = 3;

    [Header("Spawn Safety")]
    [SerializeField, Min(1)] private int spawnSafetyRadius = 3;

    [Header("Maze Generation")]
    [SerializeField, Min(1)] private int maxPlacementAttempts = 100;
    [SerializeField, Min(1)] private int minReachableCells = 80;
    [SerializeField] private bool persistSeed = true;

    private const string SeedPrefKey = "SnakeLevel3LastSeed";

    #endregion

    #region State

    private System.Random rng;
    private readonly List<GameObject> instantiatedWalls = new List<GameObject>();
    private Vector2 wallColliderSize;

    #endregion

    #region Lifecycle

    public void AutoConfigure(Food foodRef)
    {
        if (food == null) food = foodRef;
        if (food == null) food = FindObjectOfType<Food>();

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
            if (wallsGO != null) wallsParent = wallsGO.transform;
        }
    }

    private void Start()
    {
        AutoConfigure(food);

        if (wallPrefab == null)
        {
            wallPrefab = Resources.Load<GameObject>("Wall");
            if (wallPrefab == null)
            {
                Debug.LogError("[Level3MapGenerator] No wall prefab assigned and could not load from Resources.");
                return;
            }
        }

        BoxCollider2D prefabCollider = wallPrefab.GetComponent<BoxCollider2D>();
        if (prefabCollider != null)
            wallColliderSize = prefabCollider.size;
        else
            wallColliderSize = new Vector2(1f, 1f);

        int seed = Random.Range(int.MinValue, int.MaxValue);
        int lastSeed = persistSeed ? PlayerPrefs.GetInt(SeedPrefKey, int.MinValue) : int.MinValue;
        if (seed == lastSeed)
            seed += 1;

        rng = new System.Random(seed);

        bool placed = TryGenerateMaze();

        if (!placed)
        {
            ClearInstantiatedWalls();
            Debug.LogWarning($"[Level3MapGenerator] {gameObject.scene.name}: maze placement failed after {maxPlacementAttempts} attempts, no interior walls placed (seed {seed}).");
        }
        else
        {
            Debug.Log($"[Level3MapGenerator] {gameObject.scene.name}: built randomized maze (seed {seed}).");
        }

        if (persistSeed)
            PlayerPrefs.SetInt(SeedPrefKey, seed);

        if (food != null)
            food.ApplyRandomWeights(BuildRandomWeights());
    }

    #endregion

    #region Maze Generation

    private bool TryGenerateMaze()
    {
        if (gridArea == null || wallPrefab == null)
            return false;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            ClearInstantiatedWalls();

            if (TryPlaceClusters())
                return true;
        }
        return false;
    }

    private bool TryPlaceClusters()
    {
        Bounds bounds = gridArea.bounds;
        float cs = cellSize;
        int clusterCount = rng.Next(minClusters, maxClusters + 1);

        for (int c = 0; c < clusterCount; c++)
        {
            int wallCount = rng.Next(minWallsPerCluster, maxWallsPerCluster + 1);
            bool vertical = rng.Next(2) == 0;
            float rotation = vertical ? 90f : 0f;

            float longAxis = wallColliderSize.x;
            float shortAxis = wallColliderSize.y;

            float totalLong = wallCount * longAxis;
            float halfLong = totalLong * 0.5f;
            float halfShort = shortAxis * 0.5f;

            float minX = bounds.min.x + cs + (vertical ? halfShort : halfLong);
            float maxX = bounds.max.x - cs - (vertical ? halfShort : halfLong);
            float minY = bounds.min.y + cs + (vertical ? halfLong : halfShort);
            float maxY = bounds.max.y - cs - (vertical ? halfLong : halfShort);

            if (maxX < minX || maxY < minY)
                return false;

            float anchorX = (float)(rng.NextDouble() * (maxX - minX) + minX);
            float anchorY = (float)(rng.NextDouble() * (maxY - minY) + minY);

            for (int w = 0; w < wallCount; w++)
            {
                float offset = (w - (wallCount - 1) * 0.5f) * longAxis;
                float wx = vertical ? anchorX : anchorX + offset;
                float wy = vertical ? anchorY + offset : anchorY;
                SpawnWall(new Vector3(wx, wy, 0f), rotation);
            }
        }

        return IsMazeNavigable(bounds, cs);
    }

    private void SpawnWall(Vector3 position, float rotationZ)
    {
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.Euler(0f, 0f, rotationZ));
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

    #region Navigability Check

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

        for (int dx = -spawnSafetyRadius; dx <= spawnSafetyRadius; dx++)
        {
            for (int dy = -spawnSafetyRadius; dy <= spawnSafetyRadius; dy++)
            {
                if (obstacleCells.Contains(new Vector2Int(dx, dy)))
                    return false;
            }
        }

        Vector2Int head = Vector2Int.zero;
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

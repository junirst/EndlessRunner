using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{
    public enum FoodType
    {
        Normal,
        Golden,
        Speed,
        Slow,
        Shrink,
        ScoreMultiplier
    }

    [SerializeField] private BoxCollider2D gridArea;
    [SerializeField] private Snake snake;
    [SerializeField] private Color[] foodColors;
    [SerializeField] private Sprite[] foodSprites;
    [SerializeField] private int[] scoreValues;
    [SerializeField] private float[] durations;
    [SerializeField] private float[] weights;
    [SerializeField, Range(0.6f, 1f)] private float fillScale = 1f;

    private HashSet<Vector2Int> obstacleCells;
    private bool obstacleCellsBuilt;
    private bool boardInfoLogged;

    public FoodType Type { get; private set; }
    public int ScoreValue => scoreValues != null && (int)Type < scoreValues.Length ? scoreValues[(int)Type] : 10;
    public float Duration => durations != null && (int)Type < durations.Length ? durations[(int)Type] : 0f;

    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;
    private float totalWeight;
    private CircleCollider2D circleCollider;
    private float baseLocalScale = 1f;
    private float baseColliderRadius;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        circleCollider = GetComponent<CircleCollider2D>();
        baseLocalScale = Mathf.Abs(transform.localScale.x);
        if (baseLocalScale <= 0f) baseLocalScale = 1f;
        if (circleCollider != null)
            baseColliderRadius = circleCollider.radius * baseLocalScale;

        defaultSprite = spriteRenderer.sprite;

        if (weights == null || weights.Length == 0)
            weights = new float[] { 1f };

        foreach (float w in weights)
            totalWeight += w;

        if (foodColors == null || foodColors.Length == 0)
            foodColors = new Color[] { Color.red };
    }

    private void Start()
    {
        // Wait one frame so Snake.Start() has built the initial body before
        // deciding where food is allowed to spawn.
        StartCoroutine(RandomizeNextFrame());
    }

    private IEnumerator RandomizeNextFrame()
    {
        yield return null;
        RandomizedPosition();
    }

    public void RandomizedPosition()
    {
        Type = PickRandomType();
        ApplyVisuals();

        Bounds bounds = gridArea.bounds;
        float cs = snake != null ? snake.CellSize : 1f;

        // Stay at least one cell away from each wall. Board corners / food rubbing
        // the wall make the auto-play bot path around it in a circle, so keep a
        // gutter of one cell on every side.
        float minX = bounds.min.x + cs;
        float maxX = bounds.max.x - cs;
        float minY = bounds.min.y + cs;
        float maxY = bounds.max.y - cs;
        if (maxX <= minX) { minX = bounds.min.x; maxX = bounds.max.x; }
        if (maxY <= minY) { minY = bounds.min.y; maxY = bounds.max.y; }

        int x0 = Mathf.RoundToInt(minX / cs);
        int x1 = Mathf.RoundToInt(maxX / cs);
        int y0 = Mathf.RoundToInt(minY / cs);
        int y1 = Mathf.RoundToInt(maxY / cs);

        EnsureObstacleCells();
        HashSet<Vector2Int> bodyCells = CollectBodyCells();

        // A spawn point must be clear of the snake AND keep a full one-cell ring
        // of clear space around it, so the auto-play bot never has to squeeze
        // against a wall to reach the food.
        List<Vector2Int> safeCells = new List<Vector2Int>();
        List<Vector2Int> openCells = new List<Vector2Int>();
        for (int cx = x0; cx <= x1; cx++)
        {
            for (int cy = y0; cy <= y1; cy++)
            {
                Vector2Int cell = new Vector2Int(cx, cy);
                if (bodyCells.Contains(cell)) continue;
                if (HasClearRing(cell))
                    safeCells.Add(cell);
                else
                    openCells.Add(cell);
            }
        }

        // Prefer cells the auto-play bot can actually path to from the head, so
        // the food never lands in a pocket the bot refuses to enter.
        List<Vector2Int> reachable = new List<Vector2Int>();
        if (snake != null)
        {
            HashSet<Vector2Int> reachableCells = CollectReachableCells(HeadCell(), bodyCells);
            foreach (Vector2Int cell in safeCells)
            {
                if (reachableCells.Contains(cell))
                    reachable.Add(cell);
            }
        }

        if (!boardInfoLogged)
        {
            boardInfoLogged = true;
            Debug.Log($"[Food] {gameObject.scene.name}: spawner active (cells x:{x0}..{x1} y:{y0}..{y1}, cellSize={cs}, obstacles={obstacleCells?.Count ?? 0}, body={bodyCells.Count})");
        }

        List<Vector2Int> pool = reachable.Count > 0 ? reachable : safeCells;
        if (pool.Count > 0)
        {
            Vector2Int chosen = pool[Random.Range(0, pool.Count)];

            // Defensive: the pool excludes snake cells by construction, but if
            // stale body data ever slipped through, resample instead of spawning
            // on a segment - and flag it so it shows in the console.
            for (int attempt = 0; attempt < 32 && bodyCells.Contains(chosen); attempt++)
                chosen = pool[Random.Range(0, pool.Count)];

            bool onBody = bodyCells.Contains(chosen);
            if (onBody)
                Debug.LogWarning($"[Food] {gameObject.scene.name}: WARNING spawn cell {chosen} still on body after resampling!");

            transform.position = new Vector3(chosen.x * cs, chosen.y * cs, 0f);
            return;
        }

        // Board nearly filled by walls or the snake: use the clear cell furthest
        // from the body. Never falls back onto the snake itself.
        if (openCells.Count > 0)
        {
            Vector2Int bestCell = openCells[0];
            float bestDist = -1f;
            foreach (Vector2Int cell in openCells)
            {
                float d = SnakeDistance(new Vector3(cell.x * cs, cell.y * cs, 0f));
                if (d > bestDist)
                {
                    bestDist = d;
                    bestCell = cell;
                }
            }
            transform.position = new Vector3(bestCell.x * cs, bestCell.y * cs, 0f);
        }
    }

    private void ApplyVisuals()
    {
        int index = (int)Type;
        Sprite sprite = foodSprites != null && index >= 0 && index < foodSprites.Length
            ? foodSprites[index]
            : defaultSprite;

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            FitToCell(sprite);
        }
        else
        {
            spriteRenderer.color = foodColors != null && index < foodColors.Length
                ? foodColors[index]
                : Color.white;
        }
    }

    private void FitToCell(Sprite sprite)
    {
        float cs = snake != null ? snake.CellSize : 1f;
        Vector2 size = sprite.bounds.size;
        float scale = cs / Mathf.Max(size.x, size.y) * fillScale;
        transform.localScale = new Vector3(scale, scale, transform.localScale.z);

        // Keep the trigger's world-space radius constant so scaling the sprite
        // never inflates the pickup area.
        if (circleCollider != null && baseColliderRadius > 0f)
            circleCollider.radius = baseColliderRadius / Mathf.Abs(scale);
    }

    private FoodType PickRandomType()
    {
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return (FoodType)i;
        }
        return FoodType.Normal;
    }

    private Vector2Int HeadCell()
    {
        if (snake == null) return Vector2Int.zero;
        float cs = snake.CellSize;
        return new Vector2Int(
            Mathf.RoundToInt(snake.transform.position.x / cs),
            Mathf.RoundToInt(snake.transform.position.y / cs)
        );
    }

    private HashSet<Vector2Int> CollectBodyCells()
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        if (snake == null) return cells;
        float cs = snake.CellSize;
        foreach (Transform seg in snake.Segments)
        {
            cells.Add(new Vector2Int(
                Mathf.RoundToInt(seg.position.x / cs),
                Mathf.RoundToInt(seg.position.y / cs)
            ));
        }
        return cells;
    }

    private float SnakeDistance(Vector3 position)
    {
        if (snake == null) return float.MaxValue;

        float min = float.MaxValue;
        foreach (Transform seg in snake.Segments)
            min = Mathf.Min(min, Vector3.Distance(position, seg.position));
        return min;
    }

    private void EnsureObstacleCells()
    {
        if (obstacleCellsBuilt) return;
        obstacleCellsBuilt = true;
        obstacleCells = new HashSet<Vector2Int>();
        if (gridArea == null) return;

        float cs = snake != null ? snake.CellSize : 1f;
        Bounds bounds = gridArea.bounds;

        int x0 = Mathf.FloorToInt(bounds.min.x / cs);
        int x1 = Mathf.CeilToInt(bounds.max.x / cs);
        int y0 = Mathf.FloorToInt(bounds.min.y / cs);
        int y1 = Mathf.CeilToInt(bounds.max.y / cs);

        for (int cx = x0; cx <= x1; cx++)
        {
            for (int cy = y0; cy <= y1; cy++)
            {
                if (OverlapsObstacle(new Vector2(cx * cs, cy * cs), cs * 0.9f))
                    obstacleCells.Add(new Vector2Int(cx, cy));
            }
        }
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

    private bool HasClearRing(Vector2Int cell)
    {
        if (obstacleCells == null) return true;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (obstacleCells.Contains(new Vector2Int(cell.x + dx, cell.y + dy)))
                    return false;
            }
        }
        return true;
    }

    // Flood-fill from the head over open cells only (the tail vacates on the
    // next move, so it is not a wall, mirroring the auto-play bot's view).
    private HashSet<Vector2Int> CollectReachableCells(Vector2Int head, HashSet<Vector2Int> bodyCells)
    {
        HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
        reachable.Add(head);

        HashSet<Vector2Int> walls = new HashSet<Vector2Int>(bodyCells);
        if (snake != null && snake.Segments.Count > 1)
        {
            Transform tail = snake.Segments[snake.Segments.Count - 1];
            float cs = snake.CellSize;
            walls.Remove(new Vector2Int(
                Mathf.RoundToInt(tail.position.x / cs),
                Mathf.RoundToInt(tail.position.y / cs)
            ));
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(head);

        Vector2Int[] dirs = {
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
                if (walls.Contains(next)) continue;
                if (obstacleCells != null && obstacleCells.Contains(next)) continue;
                if (!IsInBounds(next)) continue;
                reachable.Add(next);
                queue.Enqueue(next);
            }
        }
        return reachable;
    }

    private bool IsInBounds(Vector2Int cell)
    {
        if (gridArea == null) return true;
        float cs = snake != null ? snake.CellSize : 1f;
        Bounds bounds = gridArea.bounds;
        return cell.x >= Mathf.FloorToInt(bounds.min.x / cs) &&
               cell.x <= Mathf.CeilToInt(bounds.max.x / cs) &&
               cell.y >= Mathf.FloorToInt(bounds.min.y / cs) &&
               cell.y <= Mathf.CeilToInt(bounds.max.y / cs);
    }

    public void Reposition()
    {
        RandomizedPosition();
    }

    // Replaces the serialized drop-weight table (used by Level4's randomizer to
    // make each run's food effects differ), recomputes the total, and respawns
    // the current food immediately so the new distribution applies right away.
    public void ApplyRandomWeights(float[] newWeights)
    {
        if (newWeights == null || newWeights.Length == 0)
            return;

        weights = newWeights;

        totalWeight = 0f;
        foreach (float w in weights)
            totalWeight += w;

        RandomizedPosition();
    }
}

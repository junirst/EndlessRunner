using System.Collections;
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
    [SerializeField] private int[] scoreValues;
    [SerializeField] private float[] durations;
    [SerializeField] private float[] weights;

    public FoodType Type { get; private set; }
    public int ScoreValue => scoreValues != null && (int)Type < scoreValues.Length ? scoreValues[(int)Type] : 10;
    public float Duration => durations != null && (int)Type < durations.Length ? durations[(int)Type] : 0f;

    private SpriteRenderer spriteRenderer;
    private float totalWeight;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

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
        spriteRenderer.color = foodColors[(int)Type];

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

        Vector3 best = transform.position;
        float bestSpace = -1f;
        for (int i = 0; i < 30; i++)
        {
            float x = Mathf.Clamp(Mathf.Round(Random.Range(minX, maxX) / cs) * cs, minX, maxX);
            float y = Mathf.Clamp(Mathf.Round(Random.Range(minY, maxY) / cs) * cs, minY, maxY);
            Vector3 candidate = new Vector3(x, y, 0f);

            // Keep the food out of the snake AND away from obstacle-tagged walls.
            // A cell touching an obstacle makes the auto-play bot path around it
            // in circles, so require a one-cell gutter of clear space.
            if (!IsOnSnake(candidate) && !IsNearObstacle(candidate))
            {
                transform.position = candidate;
                return;
            }

            float space = SnakeDistance(candidate);
            if (space > bestSpace)
            {
                bestSpace = space;
                best = candidate;
            }
        }

        // No fully open cell after all tries - use the cell furthest from the snake.
        transform.position = best;
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

    private bool IsOnSnake()
    {
        return IsOnSnake(transform.position);
    }

    private bool IsOnSnake(Vector3 position)
    {
        if (snake == null) return false;

        float threshold = Mathf.Max(snake.CellSize * 0.5f, 0.1f);
        foreach (Transform seg in snake.Segments)
        {
            if (Vector3.Distance(position, seg.position) < threshold)
                return true;
        }
        return false;
    }

    private float SnakeDistance(Vector3 position)
    {
        if (snake == null) return float.MaxValue;

        float min = float.MaxValue;
        foreach (Transform seg in snake.Segments)
            min = Mathf.Min(min, Vector3.Distance(position, seg.position));
        return min;
    }
    private bool IsNearObstacle(Vector3 position)
    {
        // Overlap a box big enough to reach all four neighbouring cells, so a
        // spawn only passes when the surrounding ring is also clear of Obstacle.
        BoxCollider2D collider = snake.GetComponent<BoxCollider2D>();
        Vector2 size = collider != null ? collider.size * snake.CellSize : Vector2.one * snake.CellSize;
        size += Vector2.one * snake.CellSize * 2f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            position,
            size,
            0f
        );
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Obstacle"))
                return true;
        }
        return false;
    }

    public void Reposition()
    {
        RandomizedPosition();
    }
}

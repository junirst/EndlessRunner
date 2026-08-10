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
    [SerializeField] private Sprite[] foodSprites;
    [SerializeField] private int[] scoreValues;
    [SerializeField] private float[] durations;
    [SerializeField] private float[] weights;
    [SerializeField, Range(0.6f, 1f)] private float fillScale = 1f;

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

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
        RandomizedPosition();
    }

    public void RandomizedPosition()
    {
        Type = PickRandomType();
        spriteRenderer.color = foodColors[(int)Type];

        Bounds bounds = gridArea.bounds;
        for (int i = 0; i < 30; i++)
        {
            float x = Mathf.Round(Random.Range(bounds.min.x, bounds.max.x));
            float y = Mathf.Round(Random.Range(bounds.min.y, bounds.max.y));
            transform.position = new Vector3(x, y, 0f);

            if (!IsOnSnake())
                return;
        }
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
        if (snake == null) return false;

        foreach (Transform seg in snake.Segments)
        {
            if (Vector3.Distance(transform.position, seg.position) < 0.1f)
                return true;
        }
        return false;
    }

    public void Reposition()
    {
        RandomizedPosition();
    }
}

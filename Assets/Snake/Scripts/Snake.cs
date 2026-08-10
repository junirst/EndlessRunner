using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Snake : MonoBehaviour
{
    public Transform segmentPrefab;
    public Vector2Int direction = Vector2Int.right;
    public float speed = 20f;
    public float speedMultiplier = 1f;
    public int initialSize = 4;
    public float moveThroughWalls = 0f;
    public float verticalBound = 0f;

    [SerializeField] private float cellSize = 0f;

    private readonly List<Transform> segments = new List<Transform>();
    private readonly List<Vector2Int> cells = new List<Vector2Int>();
    private Vector2Int input;
    private Vector2Int? autoInput;
    private float nextUpdate;
    private float permanentSpeedBonus;

    [SerializeField] private float permanentSpeedIncrease = 0.05f;

    private readonly List<SpeedEffect> speedEffects = new List<SpeedEffect>();
    private Coroutine speedEffectsRoutine;
    private float baseSpeedMultiplier;
    private float wallImmunityTimer;
    private const float MinSpeedMultiplier = 0.1f;
    private const string InputXParam = "InputX";
    private const string InputYParam = "InputY";

    private Animator animator;
    private Vector2Int lastDirection;
    private float bodyScale = 1f;
    private Vector2 bodyColliderSize = Vector2.one;

    public IReadOnlyList<Transform> Segments => segments;
    public Vector2Int CurrentDirection => direction;
    public bool IsWallImmune => wallImmunityTimer > 0f;

    /// <summary>
    /// Grid step in world units (distance between segment centers). Uses the
    /// serialized <see cref="cellSize"/> when set, otherwise the head's width so
    /// scaled-up sprites do not overlap each other.
    /// </summary>
    public float CellSize { get; private set; } = 1f;

    public void SetAutoInput(Vector2Int dir)
    {
        autoInput = dir;
    }

    private void Awake()
    {
        baseSpeedMultiplier = speedMultiplier;
        CellSize = cellSize > 0f ? cellSize : Mathf.Abs(transform.localScale.x);
        if (CellSize <= 0f) CellSize = 1f;

        animator = GetComponent<Animator>();

        if (segmentPrefab != null)
        {
            SpriteRenderer prefabRenderer = segmentPrefab.GetComponent<SpriteRenderer>();
            if (prefabRenderer != null && prefabRenderer.sprite != null)
            {
                Vector2 size = prefabRenderer.sprite.bounds.size;
                float maxSize = Mathf.Max(size.x, size.y);
                if (maxSize > 0f)
                    bodyScale = CellSize / maxSize;
            }

            BoxCollider2D prefabCollider = segmentPrefab.GetComponent<BoxCollider2D>();
            if (prefabCollider != null)
                bodyColliderSize = prefabCollider.size;
        }

        // Capture the grid size above before rescaling the visuals so the
        // head sprite can fill its cell exactly without shifting the grid.
        FitToCell(transform);
    }

    private void Start()
    {
        ResetState();
    }

    private void UpdateAnimatorDirection()
    {
        if (animator == null) return;

        if (direction != lastDirection)
        {
            lastDirection = direction;
            animator.SetFloat(InputXParam, direction.x);
            animator.SetFloat(InputYParam, direction.y);
        }
    }

    private void Update()
    {
        if (autoInput.HasValue)
        {
            input = autoInput.Value;
            autoInput = null;
        }
        else if (direction.x != 0f)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                input = Vector2Int.up;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                input = Vector2Int.down;
        }
        else if (direction.y != 0f)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                input = Vector2Int.right;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                input = Vector2Int.left;
        }
    }

    private void FixedUpdate()
    {
        if (wallImmunityTimer > 0f)
            wallImmunityTimer -= Time.deltaTime;

        if (Time.time < nextUpdate)
            return;

        if (input != Vector2Int.zero)
            direction = input;

        UpdateAnimatorDirection();

        Vector2Int headCell = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / CellSize),
            Mathf.RoundToInt(transform.position.y / CellSize)
        );
        Vector2Int newHead = Wrap(headCell + direction);

        if (newHead != headCell && WouldCollide(newHead))
        {
            GameManager.Instance.GameOver();
            return;
        }

        cells.Insert(0, newHead);
        cells.RemoveAt(cells.Count - 1);

        transform.position = new Vector2(newHead.x * CellSize, newHead.y * CellSize);
        for (int i = 1; i < segments.Count; i++)
        {
            Vector2Int cell = cells[i];
            segments[i].position = new Vector2(cell.x * CellSize, cell.y * CellSize);
            UpdateSegmentAnimation(segments[i], GetSegmentAnimationDirection(i));
        }

        nextUpdate = Time.time + (1f / (speed * (1f + permanentSpeedBonus) * speedMultiplier));
    }

    private Vector2Int Wrap(Vector2Int cell)
    {
        if (moveThroughWalls <= 0f) return cell;

        int x = cell.x;
        int y = cell.y;
        int boundX = Mathf.RoundToInt(moveThroughWalls / CellSize);
        int boundY = verticalBound > 0f
            ? Mathf.RoundToInt(verticalBound / CellSize)
            : Mathf.RoundToInt((moveThroughWalls * 0.5f) / CellSize);

        if (x > boundX) x = -boundX;
        else if (x < -boundX) x = boundX;
        if (y > boundY) y = -boundY;
        else if (y < -boundY) y = boundY;

        return new Vector2Int(x, y);
    }

    private bool WouldCollide(Vector2Int pos)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            // The tail cell vacates on this move, so it is not a collision.
            if (i == cells.Count - 1) continue;
            if (cells[i] == pos) return true;
        }
        return false;
    }

    private void FitToCell(Transform segment)
    {
        SpriteRenderer spriteRenderer = segment.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        Vector2 size = spriteRenderer.sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f) return;

        float scale = CellSize / Mathf.Max(size.x, size.y);
        segment.localScale = new Vector3(
            scale,
            scale,
            segment.localScale.z
        );
    }

    // Facing of a body segment: it points one cell toward the head. Falls back
    // to the nearest segment with a non-zero delta so a freshly grown tail
    // (which shares its cell with the old tail for one tick) still faces along
    // the body.
    private Vector2Int GetSegmentFacing(int index)
    {
        if (index <= 0 || index >= cells.Count)
            return Vector2Int.right;

        Vector2Int facing = cells[index - 1] - cells[index];
        if (facing != Vector2Int.zero)
            return facing;

        for (int k = index - 1; k > 0; k--)
        {
            facing = cells[k - 1] - cells[k];
            if (facing != Vector2Int.zero)
                return facing;
        }
        return Vector2Int.right;
    }

    // Direction from a segment toward the tail. Returns null when there is no
    // segment behind this one (tail) or when the neighbor shares this cell for
    // a tick after growing, so those segments keep rendering straight.
    private Vector2Int? GetBackDirection(int index)
    {
        if (index <= 0 || index >= cells.Count - 1)
            return null;

        for (int k = index; k < cells.Count - 1; k++)
        {
            Vector2Int delta = cells[k + 1] - cells[k];
            if (delta != Vector2Int.zero)
                return delta;
        }
        return null;
    }

    // Blend-tree direction for a body segment: the cardinal facing for a
    // straight run, or the diagonal (front + back) that selects the matching
    // corner sprite where the body bends.
    private Vector2Int GetSegmentAnimationDirection(int index)
    {
        Vector2Int front = GetSegmentFacing(index);
        Vector2Int? back = GetBackDirection(index);
        if (back == null) return front;

        Vector2Int backValue = back.Value;
        if (front.x * backValue.y - front.y * backValue.x == 0)
            return front;

        return front + backValue;
    }

    private void UpdateSegmentAnimation(Transform segment, Vector2Int dir)
    {
        // All segments (straights and corners) use the same fit-to-cell scale so
        // the corner L-arms reach the neighboring cell edges exactly, keeping the
        // body width consistent through turns. The collider stays at body size.
        segment.localScale = new Vector3(bodyScale, bodyScale, segment.localScale.z);

        BoxCollider2D collider = segment.GetComponent<BoxCollider2D>();
        if (collider != null)
            collider.size = bodyColliderSize;

        Animator segmentAnimator = segment.GetComponent<Animator>();
        if (segmentAnimator == null) return;

        segmentAnimator.SetFloat(InputXParam, dir.x);
        segmentAnimator.SetFloat(InputYParam, dir.y);
    }

    public void Grow()
    {
        Vector2Int tailCell = cells[cells.Count - 1];
        Transform segment = Instantiate(segmentPrefab);
        segment.position = new Vector2(tailCell.x * CellSize, tailCell.y * CellSize);
        segment.tag = "Body";
        FitToCell(segment);
        segments.Add(segment);
        cells.Add(tailCell);

        UpdateSegmentAnimation(
            segment,
            GetSegmentAnimationDirection(segments.Count - 1)
        );
    }

    public void Shrink(int count)
    {
        count = Mathf.Min(count, segments.Count - 1);
        for (int i = 0; i < count; i++)
        {
            Transform tail = segments[segments.Count - 1];
            segments.RemoveAt(segments.Count - 1);
            cells.RemoveAt(cells.Count - 1);
            Destroy(tail.gameObject);
        }
    }

    public void ResetState()
    {
        StopAllCoroutines();
        speedEffectsRoutine = null;
        speedEffects.Clear();
        speedMultiplier = baseSpeedMultiplier;
        permanentSpeedBonus = 0f;
        wallImmunityTimer = 0.2f;
        ScoreManager.Instance.ResetMultiplier();
        PowerUpUI.Instance?.ClearAll();

        direction = Vector2Int.right;
        transform.position = Vector3.zero;

        for (int i = 1; i < segments.Count; i++)
            Destroy(segments[i].gameObject);

        segments.Clear();
        segments.Add(transform);

        cells.Clear();
        for (int i = 0; i < initialSize; i++)
            cells.Add(new Vector2Int(-i, 0));

        for (int i = 1; i < initialSize; i++)
        {
            Vector2Int cell = cells[i];
            Transform segment = Instantiate(segmentPrefab);
            segment.position = new Vector2(cell.x * CellSize, cell.y * CellSize);
            segment.tag = "Body";
            FitToCell(segment);
            segments.Add(segment);
            UpdateSegmentAnimation(segment, GetSegmentAnimationDirection(i));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Food"))
        {
            Food food = other.GetComponent<Food>();
            switch (food.Type)
            {
                case Food.FoodType.Normal:
                    Grow();
                    ScoreManager.Instance.AddScore(food.ScoreValue);
                    break;
                case Food.FoodType.Golden:
                    ScoreManager.Instance.AddScore(food.ScoreValue);
                    break;
                case Food.FoodType.Shrink:
                    if (segments.Count > 4) Shrink(1);
                    break;
                case Food.FoodType.Speed:
                    ApplySpeedEffect(1.5f, food.Duration);
                    break;
                case Food.FoodType.Slow:
                    ApplySpeedEffect(0.5f, food.Duration);
                    break;
                case Food.FoodType.ScoreMultiplier:
                    ScoreManager.Instance.SetMultiplier(2f, food.Duration);
                    break;
            }
            permanentSpeedBonus += permanentSpeedIncrease;
            SnakeAudioManager.Instance?.PlayEatSfx();
            food.Reposition();
        }
        else if (other.CompareTag("Obstacle"))
        {
            GameManager.Instance.GameOver();
        }
        else if (other.CompareTag("Wall"))
        {
            if (moveThroughWalls > 0f) return;
            if (wallImmunityTimer > 0f) return;
            ResetState();
        }
        else if (other.CompareTag("Body"))
        {
            GameManager.Instance.GameOver();
        }
    }

    private void ApplySpeedEffect(float factor, float duration)
    {
        if (duration <= 0f) return;

        speedEffects.Add(new SpeedEffect(factor, duration));
        PowerUpUI.Instance?.ShowPowerUp(
            factor > 1f ? Food.FoodType.Speed : Food.FoodType.Slow,
            duration
        );
        RecalculateSpeedMultiplier();

        if (speedEffectsRoutine == null)
            speedEffectsRoutine = StartCoroutine(TickSpeedEffects());
    }

    private IEnumerator TickSpeedEffects()
    {
        while (speedEffects.Count > 0)
        {
            bool changed = false;
            for (int i = speedEffects.Count - 1; i >= 0; i--)
            {
                speedEffects[i].remaining -= Time.deltaTime;
                if (speedEffects[i].remaining <= 0f)
                {
                    speedEffects.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed)
                RecalculateSpeedMultiplier();
            yield return null;
        }
        speedEffectsRoutine = null;
    }

    // Multiplicative stacking: normal x 1.5 (Speed) then x 0.5 (Slow) results in
    // the base multiplier times the product of all active effects, and each effect
    // reverts individually when its timer expires.
    private void RecalculateSpeedMultiplier()
    {
        float multiplier = baseSpeedMultiplier;
        foreach (SpeedEffect effect in speedEffects)
            multiplier *= effect.factor;
        speedMultiplier = Mathf.Max(multiplier, MinSpeedMultiplier);
    }

    private class SpeedEffect
    {
        public readonly float factor;
        public float remaining;

        public SpeedEffect(float factor, float duration)
        {
            this.factor = factor;
            remaining = duration;
        }
    }
}

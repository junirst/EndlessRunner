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

    private readonly List<Transform> segments = new List<Transform>();
    private readonly List<Vector2Int> cells = new List<Vector2Int>();
    private Vector2Int input;
    private Vector2Int? autoInput;
    private float nextUpdate;
    private float permanentSpeedBonus;

    [SerializeField] private float permanentSpeedIncrease = 0.05f;

    private Coroutine speedCoroutine;
    private float baseSpeedMultiplier;
    private float wallImmunityTimer;

    public IReadOnlyList<Transform> Segments => segments;
    public Vector2Int CurrentDirection => direction;
    public bool IsWallImmune => wallImmunityTimer > 0f;

    public void SetAutoInput(Vector2Int dir)
    {
        autoInput = dir;
    }

    private void Awake()
    {
        baseSpeedMultiplier = speedMultiplier;
    }

    private void Start()
    {
        ResetState();
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

        Vector2Int headCell = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
        Vector2Int newHead = Wrap(headCell + direction);

        if (newHead != headCell && WouldCollide(newHead))
        {
            GameManager.Instance.GameOver();
            return;
        }

        cells.Insert(0, newHead);
        cells.RemoveAt(cells.Count - 1);

        transform.position = new Vector2(newHead.x, newHead.y);
        for (int i = 1; i < segments.Count; i++)
        {
            Vector2Int cell = cells[i];
            segments[i].position = new Vector2(cell.x, cell.y);
        }

        nextUpdate = Time.time + (1f / (speed * (1f + permanentSpeedBonus) * speedMultiplier));
    }

    private Vector2Int Wrap(Vector2Int cell)
    {
        if (moveThroughWalls <= 0f) return cell;

        int x = cell.x;
        int y = cell.y;
        float boundX = moveThroughWalls;
        float boundY = verticalBound > 0f ? verticalBound : moveThroughWalls * 0.5f;

        if (x > boundX) x = Mathf.RoundToInt(-boundX);
        else if (x < -boundX) x = Mathf.RoundToInt(boundX);
        if (y > boundY) y = Mathf.RoundToInt(-boundY);
        else if (y < -boundY) y = Mathf.RoundToInt(boundY);

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

    public void Grow()
    {
        Vector2Int tailCell = cells[cells.Count - 1];
        Transform segment = Instantiate(segmentPrefab);
        segment.position = new Vector2(tailCell.x, tailCell.y);
        segment.tag = "Body";
        segments.Add(segment);
        cells.Add(tailCell);
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
        speedCoroutine = null;
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
            segment.position = new Vector2(cell.x, cell.y);
            segment.tag = "Body";
            segments.Add(segment);
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
                    SetSpeedMultiplier(1.5f, food.Duration);
                    break;
                case Food.FoodType.Slow:
                    SetSpeedMultiplier(0.5f, food.Duration);
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

    private void SetSpeedMultiplier(float value, float duration)
    {
        if (speedCoroutine != null)
            StopCoroutine(speedCoroutine);
        speedCoroutine = StartCoroutine(SpeedRoutine(value, duration));
    }

    private IEnumerator SpeedRoutine(float value, float duration)
    {
        speedMultiplier = value;
        PowerUpUI.Instance?.ShowPowerUp(
            value > 1f ? Food.FoodType.Speed : Food.FoodType.Slow,
            duration
        );
        yield return new WaitForSeconds(duration);
        speedMultiplier = baseSpeedMultiplier;
        speedCoroutine = null;
    }
}

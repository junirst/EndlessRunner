using System.Collections.Generic;
using UnityEngine;

public class SnakeAutoPlay : MonoBehaviour
{
    [SerializeField] private Snake snake;
    [SerializeField] private bool autoPlayEnabled;

    private Food food;
    private BoxCollider2D gridArea;

    private void Awake()
    {
        if (snake == null) snake = GetComponent<Snake>();
        food = FindObjectOfType<Food>();
        GameObject grid = GameObject.Find("GridArea");
        if (grid != null)
            gridArea = grid.GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (!autoPlayEnabled || snake == null || food == null) return;

        Vector2Int headPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
        Vector2Int foodPos = new Vector2Int(
            Mathf.RoundToInt(food.transform.position.x),
            Mathf.RoundToInt(food.transform.position.y)
        );

        Vector2Int bestDir = GetBestDirection(headPos, foodPos);
        if (bestDir != Vector2Int.zero)
            snake.SetAutoInput(bestDir);
    }

    private bool UsesWrapping => snake.moveThroughWalls > 0f;

    private int WrapX(int x)
    {
        if (!UsesWrapping) return x;
        int half = Mathf.RoundToInt(snake.moveThroughWalls);
        int width = half * 2;
        x = ((x + half) % width + width) % width - half;
        return x;
    }

    private int WrapY(int y)
    {
        if (!UsesWrapping) return y;
        int half = Mathf.RoundToInt(snake.verticalBound > 0f ? snake.verticalBound : snake.moveThroughWalls * 0.5f);
        int height = half * 2;
        y = ((y + half) % height + height) % height - half;
        return y;
    }

    private Vector2Int WrapPos(Vector2Int pos)
    {
        return new Vector2Int(WrapX(pos.x), WrapY(pos.y));
    }

    private Vector2Int GetBestDirection(Vector2Int head, Vector2Int target)
    {
        Vector2Int currentDir = snake.CurrentDirection;
        Vector2Int[] dirs = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        Vector2Int fallback = Vector2Int.zero;
        Vector2Int safeFallback = Vector2Int.zero;
        float bestScore = float.MinValue;
        float safeScore = float.MinValue;
        Vector2Int best = Vector2Int.zero;

        int wrappedDx = WrappedAxisDelta(head.x, target.x, true);
        int wrappedDy = WrappedAxisDelta(head.y, target.y, false);

        foreach (Vector2Int dir in dirs)
        {
            if (dir == -currentDir) continue;

            Vector2Int rawNext = head + dir;
            Vector2Int nextPos = UsesWrapping ? WrapPos(rawNext) : rawNext;
            bool safe = IsSafe(nextPos);

            if (safe && CanReachFood(nextPos, target))
            {
                float score = ScoreDirection(nextPos, target, wrappedDx, wrappedDy, dir, currentDir);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = dir;
                }
            }
            else if (safe)
            {
                float score = ScoreDirection(nextPos, target, wrappedDx, wrappedDy, dir, currentDir);
                if (score > safeScore)
                {
                    safeScore = score;
                    safeFallback = dir;
                }
            }

            if (fallback == Vector2Int.zero)
                fallback = dir;
        }

        if (best == Vector2Int.zero)
            best = safeFallback;
        if (best == Vector2Int.zero)
            best = fallback;

        return best;
    }

    private int WrappedAxisDelta(int from, int to, bool isX)
    {
        if (!UsesWrapping)
            return to - from;

        int half = isX
            ? Mathf.RoundToInt(snake.moveThroughWalls)
            : Mathf.RoundToInt(snake.verticalBound > 0f ? snake.verticalBound : snake.moveThroughWalls * 0.5f);
        int size = half * 2;
        int raw = to - from;
        int wrapped = raw;
        if (raw > half) wrapped = raw - size;
        else if (raw < -half) wrapped = raw + size;
        return wrapped;
    }

    private float ScoreDirection(Vector2Int nextPos, Vector2Int target, int wrappedDx, int wrappedDy, Vector2Int dir, Vector2Int currentDir)
    {
        int nx = UsesWrapping ? WrappedAxisDelta(nextPos.x, target.x, true) : target.x - nextPos.x;
        int ny = UsesWrapping ? WrappedAxisDelta(nextPos.y, target.y, false) : target.y - nextPos.y;
        float dist = Mathf.Sqrt(nx * nx + ny * ny);
        float score = -dist;

        if (dir == currentDir)
            score += 0.5f;

        if (wrappedDx > 0 && dir == Vector2Int.right) score += 0.3f;
        else if (wrappedDx < 0 && dir == Vector2Int.left) score += 0.3f;
        if (wrappedDy > 0 && dir == Vector2Int.up) score += 0.3f;
        else if (wrappedDy < 0 && dir == Vector2Int.down) score += 0.3f;

        return score;
    }

    private bool IsSafe(Vector2Int pos)
    {
        if (IsOnBody(pos)) return false;
        if (!IsInBounds(pos)) return false;
        return true;
    }

    private bool IsOnBody(Vector2Int pos)
    {
        IReadOnlyList<Transform> segs = snake.Segments;
        for (int i = 0; i < segs.Count; i++)
        {
            Vector2Int segPos = new Vector2Int(
                Mathf.RoundToInt(segs[i].position.x),
                Mathf.RoundToInt(segs[i].position.y)
            );
            if (UsesWrapping)
                segPos = WrapPos(segPos);
            if (pos == segPos)
                return true;
        }
        return false;
    }

    private bool IsOnBodyForBFS(Vector2Int pos)
    {
        IReadOnlyList<Transform> segs = snake.Segments;
        int tailIndex = segs.Count - 1;
        for (int i = 0; i < segs.Count; i++)
        {
            if (i == tailIndex) continue;

            Vector2Int segPos = new Vector2Int(
                Mathf.RoundToInt(segs[i].position.x),
                Mathf.RoundToInt(segs[i].position.y)
            );
            if (UsesWrapping)
                segPos = WrapPos(segPos);
            if (pos == segPos)
                return true;
        }
        return false;
    }

    private bool IsInBounds(Vector2Int pos)
    {
        if (UsesWrapping)
            return true;

        if (gridArea == null) return true;

        Bounds bounds = gridArea.bounds;
        return pos.x >= Mathf.RoundToInt(bounds.min.x) &&
               pos.x <= Mathf.RoundToInt(bounds.max.x) &&
               pos.y >= Mathf.RoundToInt(bounds.min.y) &&
               pos.y <= Mathf.RoundToInt(bounds.max.y);
    }

    private bool CanReachFood(Vector2Int start, Vector2Int target, int maxSteps = 50)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int wrappedTarget = UsesWrapping ? WrapPos(target) : target;

        int steps = 0;
        while (queue.Count > 0 && steps < maxSteps)
        {
            Vector2Int current = queue.Dequeue();
            steps++;

            if (current == wrappedTarget)
                return true;

            Vector2Int[] dirs = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = UsesWrapping ? WrapPos(current + dir) : current + dir;
                if (visited.Contains(next)) continue;
                if (!IsInBounds(next)) continue;
                if (IsOnBodyForBFS(next)) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }
}

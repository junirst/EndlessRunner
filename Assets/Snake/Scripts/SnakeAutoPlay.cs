using System.Collections.Generic;
using UnityEngine;

public class SnakeAutoPlay : MonoBehaviour
{
    [SerializeField] private Snake snake;
    [SerializeField] private bool autoPlayEnabled;

    private Food food;
    private BoxCollider2D gridArea;
    private BoxCollider2D snakeCollider;

    private void Awake()
    {
        if (snake == null) snake = GetComponent<Snake>();
        snakeCollider = snake.GetComponent<BoxCollider2D>();
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

        List<Vector2Int> path = AStar(head, target);
        if (path != null && path.Count > 0)
        {
            Vector2Int next = path[0];
            Vector2Int dir = next - head;
            if (dir != -currentDir)
                return dir;
        }

        return SmartFallback(head, currentDir, target);
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

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        if (UsesWrapping)
        {
            int dx = Mathf.Abs(WrappedAxisDelta(a.x, b.x, true));
            int dy = Mathf.Abs(WrappedAxisDelta(a.y, b.y, false));
            return dx + dy;
        }
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool IsSafe(Vector2Int pos)
    {
        if (IsOnBody(pos)) return false;
        if (IsOnObstacle(pos)) return false;
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

    private bool IsOnObstacle(Vector2Int pos)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            new Vector2(pos.x, pos.y),
            snakeCollider.size,
            0f
        );
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Obstacle"))
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

    private List<Vector2Int> AStar(Vector2Int start, Vector2Int goal)
    {
        var openSet = new List<(Vector2Int pos, int g, int h)>();
        var closedSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();

        openSet.Add((start, 0, Manhattan(start, goal)));
        gScore[start] = 0;

        int maxSteps = 3000;
        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxSteps)
        {
            iterations++;

            int bestIdx = 0;
            int bestF = openSet[0].g + openSet[0].h;
            for (int i = 1; i < openSet.Count; i++)
            {
                int f = openSet[i].g + openSet[i].h;
                if (f < bestF)
                {
                    bestF = f;
                    bestIdx = i;
                }
            }

            var current = openSet[bestIdx];
            openSet.RemoveAt(bestIdx);

            if (current.pos == goal)
            {
                var path = new List<Vector2Int>();
                Vector2Int p = goal;
                while (p != start)
                {
                    path.Add(p);
                    p = cameFrom[p];
                }
                path.Reverse();
                return path;
            }

            closedSet.Add(current.pos);

            Vector2Int[] dirs = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = UsesWrapping ? WrapPos(current.pos + dir) : current.pos + dir;

                if (closedSet.Contains(next)) continue;
                if (!IsInBounds(next)) continue;
                if (IsOnObstacle(next)) continue;
                if (IsOnBodyForBFS(next)) continue;

                int tentativeG = current.g + 1;
                if (!gScore.ContainsKey(next) || tentativeG < gScore[next])
                {
                    gScore[next] = tentativeG;
                    cameFrom[next] = current.pos;
                    openSet.Add((next, tentativeG, Manhattan(next, goal)));
                }
            }
        }

        return null;
    }

    private Vector2Int SmartFallback(Vector2Int head, Vector2Int currentDir, Vector2Int target)
    {
        Vector2Int bestDir = Vector2Int.zero;
        int bestDist = int.MaxValue;
        int bestSpace = -1;

        Vector2Int[] dirs = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        foreach (Vector2Int dir in dirs)
        {
            if (dir == -currentDir) continue;

            Vector2Int next = UsesWrapping ? WrapPos(head + dir) : head + dir;
            if (!IsSafe(next)) continue;

            int dist = Manhattan(next, target);
            int space = CountReachableSpace(next, 100);

            if (dist < bestDist || (dist == bestDist && space > bestSpace))
            {
                bestDist = dist;
                bestSpace = space;
                bestDir = dir;
            }
        }

        if (bestDir != Vector2Int.zero)
            return bestDir;

        foreach (Vector2Int dir in dirs)
        {
            if (dir == -currentDir) continue;
            Vector2Int next = UsesWrapping ? WrapPos(head + dir) : head + dir;
            if (IsSafe(next))
                return dir;
        }

        return Vector2Int.zero;
    }

    private int CountReachableSpace(Vector2Int start, int maxCount)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        int count = 0;
        while (queue.Count > 0 && count < maxCount)
        {
            Vector2Int current = queue.Dequeue();
            count++;

            Vector2Int[] dirs = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = UsesWrapping ? WrapPos(current + dir) : current + dir;
                if (visited.Contains(next)) continue;
                if (!IsSafe(next)) continue;
                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return count;
    }
}

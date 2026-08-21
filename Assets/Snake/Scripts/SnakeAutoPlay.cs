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

        float cs = snake.CellSize;
        Vector2Int headPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / cs),
            Mathf.RoundToInt(transform.position.y / cs)
        );
        Vector2Int foodPos = new Vector2Int(
            Mathf.RoundToInt(food.transform.position.x / cs),
            Mathf.RoundToInt(food.transform.position.y / cs)
        );

        Vector2Int bestDir = GetBestDirection(headPos, foodPos);
        if (bestDir != Vector2Int.zero)
            snake.SetAutoInput(bestDir);
    }

    private bool UsesWrapping => snake.moveThroughWalls > 0f;

    private int WrapX(int x)
    {
        if (!UsesWrapping) return x;
        int bound = Mathf.RoundToInt(snake.moveThroughWalls / snake.CellSize);
        if (x > bound) return -bound;
        if (x < -bound) return bound;
        return x;
    }

private int WrapY(int y)
    {
        if (!UsesWrapping) return y;
        float cs = snake.CellSize;
        float rawBound = snake.verticalBound > 0f ? snake.verticalBound : snake.moveThroughWalls * 0.5f;
        int bound = Mathf.RoundToInt(rawBound / cs);
        if (y > bound) return -bound;
        if (y < -bound) return bound;
        return y;
    }

    private Vector2Int WrapPos(Vector2Int pos)
    {
        return new Vector2Int(WrapX(pos.x), WrapY(pos.y));
    }

    private Vector2Int GetBestDirection(Vector2Int head, Vector2Int target)
    {
        Vector2Int currentDir = snake.CurrentDirection;

        // Strategy 1 (eat when it does not box itself in): take the shortest path
        // to the food, but only if after walking it there is still a corridor
        // from the head back to the tail. This is what keeps a small sliver of
        // open space inside/near the body instead of trapping the snake.
        List<Vector2Int> foodPath = AStar(head, target);
        if (foodPath != null && foodPath.Count > 0 && IsSimSafe(head, foodPath))
        {
            Vector2Int dir = DirectionTo(head, foodPath[0], currentDir);
            if (dir != Vector2Int.zero)
                return dir;
        }

        // Strategy 2 (chase the tail): eating right now would close the escape
        // route, so instead steer toward the tail. The tail frees one cell every
        // tick, so following it opens up the board again (opening style).
        Vector2Int tailPos = GetTail();
        List<Vector2Int> tailPath = AStar(head, tailPos);
        if (tailPath != null && tailPath.Count > 0 && IsSimSafe(head, tailPath))
        {
            Vector2Int dir = DirectionTo(head, tailPath[0], currentDir);
            if (dir != Vector2Int.zero)
                return dir;
        }

        // Strategy 3 (maximize open air): pick the move that keeps the most
        // reachable cells around the head, still weighting toward the food.
        Vector2Int maxSpaceDir = MaxSpaceDirection(head, currentDir, target);
        if (maxSpaceDir != Vector2Int.zero)
            return maxSpaceDir;

        // Strategy 4 (last resort): any non-reverse safe move, chosen at random,
        // keeps the bot alive while the corridor around its body reopens.
        return RandomOpenDirection(head, currentDir);
    }

    private Vector2Int DirectionTo(Vector2Int head, Vector2Int next, Vector2Int currentDir)
    {
        Vector2Int dir = new Vector2Int(
            Mathf.Clamp(WrappedAxisDelta(head.x, next.x, true), -1, 1),
            Mathf.Clamp(WrappedAxisDelta(head.y, next.y, false), -1, 1)
        );
        if (dir == Vector2Int.zero || dir == -currentDir)
            return Vector2Int.zero;
        return dir;
    }

    private Vector2Int GetTail()
    {
        IReadOnlyList<Transform> segs = snake.Segments;
        float cs = snake.CellSize;
        Vector2Int tail = new Vector2Int(
            Mathf.RoundToInt(segs[segs.Count - 1].position.x / cs),
            Mathf.RoundToInt(segs[segs.Count - 1].position.y / cs)
        );
        return UsesWrapping ? WrapPos(tail) : tail;
    }

    private List<Vector2Int> CollectBodyCells()
    {
        IReadOnlyList<Transform> segs = snake.Segments;
        float cs = snake.CellSize;
        List<Vector2Int> body = new List<Vector2Int>();
        // The tail cell vacates on the next move, so it is not a wall and is
        // excluded (it is also the escape route target).
        for (int i = 0; i < segs.Count - 1; i++)
        {
            Vector2Int p = new Vector2Int(
                Mathf.RoundToInt(segs[i].position.x / cs),
                Mathf.RoundToInt(segs[i].position.y / cs)
            );
            if (UsesWrapping) p = WrapPos(p);
            body.Add(p);
        }
        return body;
    }

    /// <summary>
    /// Simulates walking the whole path (the tail vacates one cell per step) and
    /// returns true only if afterwards the head can still reach the tail cell.
    /// That "head-to-tail reachability" is the affordable test of whether the
    /// move leaves an open corridor behind instead of sealing the snake inside
    /// the volume of its own body.
    /// </summary>
    private bool IsSimSafe(Vector2Int head, List<Vector2Int> path)
    {
        List<Vector2Int> sim = CollectBodyCells();
        Vector2Int cursor = head;

        foreach (Vector2Int step in path)
        {
            if (sim.Count > 0)
                sim.RemoveAt(sim.Count - 1); // tail vacates first
            if (sim.Contains(step))
                return false;
            sim.Insert(0, step);
            cursor = step;
        }

        if (sim.Count == 0)
            return true;
        return CanReachTail(cursor, sim);
    }

    private bool CanReachTail(Vector2Int start, List<Vector2Int> simBody)
    {
        Vector2Int tail = simBody[simBody.Count - 1];

        HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
        for (int i = 0; i < simBody.Count - 1; i++)
            walls.Add(simBody[i]);

        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        int steps = 0;
        while (queue.Count > 0 && steps < 3000)
        {
            steps++;
            Vector2Int current = queue.Dequeue();
            if (current == tail)
                return true;

            Vector2Int[] dirs = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };
            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = UsesWrapping ? WrapPos(current + dir) : current + dir;
                if (visited.Contains(next)) continue;
                if (walls.Contains(next)) continue;
                if (!IsInBounds(next)) continue;
                if (IsOnObstacle(next)) continue;
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return visited.Contains(tail);
    }

    private Vector2Int MaxSpaceDirection(Vector2Int head, Vector2Int currentDir, Vector2Int target)
    {
        Vector2Int bestDir = Vector2Int.zero;
        int bestSpace = -1;
        int bestDist = int.MaxValue;

        Vector2Int[] dirs = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };
        foreach (Vector2Int dir in dirs)
        {
            if (dir == -currentDir) continue;

            Vector2Int next = UsesWrapping ? WrapPos(head + dir) : head + dir;
            if (!IsSafe(next)) continue;

            int space = CountReachableSpace(next, 200);
            int dist = Manhattan(next, target);
            if (space > bestSpace || (space == bestSpace && dist < bestDist))
            {
                bestSpace = space;
                bestDist = dist;
                bestDir = dir;
            }
        }
        return bestDir;
    }

    private Vector2Int RandomOpenDirection(Vector2Int head, Vector2Int currentDir)
    {
        List<Vector2Int> options = new List<Vector2Int>();
        Vector2Int[] dirs = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };
        foreach (Vector2Int dir in dirs)
        {
            if (dir == -currentDir) continue;

            Vector2Int next = UsesWrapping ? WrapPos(head + dir) : head + dir;
            if (IsSafe(next))
                options.Add(dir);
        }

        if (options.Count == 0)
            return Vector2Int.zero;
        return options[Random.Range(0, options.Count)];
    }

    private int WrappedAxisDelta(int from, int to, bool isX)
    {
        if (!UsesWrapping)
            return to - from;

        float cs = snake.CellSize;
        int bound = isX
            ? Mathf.RoundToInt(snake.moveThroughWalls / cs)
            : Mathf.RoundToInt((snake.verticalBound > 0f ? snake.verticalBound : snake.moveThroughWalls * 0.5f) / cs);
        int n = bound * 2 + 1;
        int raw = to - from;
        int mod = ((raw % n) + n) % n;
        if (mod > bound) mod -= n;
        return mod;
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
        float cs = snake.CellSize;
        int tailIndex = segs.Count - 1;
        for (int i = 0; i < segs.Count; i++)
        {
            if (i == tailIndex) continue;

            Vector2Int segPos = new Vector2Int(
                Mathf.RoundToInt(segs[i].position.x / cs),
                Mathf.RoundToInt(segs[i].position.y / cs)
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
        float cs = snake.CellSize;
        int tailIndex = segs.Count - 1;
        for (int i = 0; i < segs.Count; i++)
        {
            if (i == tailIndex) continue;

            Vector2Int segPos = new Vector2Int(
                Mathf.RoundToInt(segs[i].position.x / cs),
                Mathf.RoundToInt(segs[i].position.y / cs)
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
        float cs = snake.CellSize;
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            new Vector2(pos.x * cs, pos.y * cs),
            snakeCollider.size * cs,
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

        float cs = snake.CellSize;
        Bounds bounds = gridArea.bounds;
        return pos.x >= Mathf.RoundToInt(bounds.min.x / cs) &&
               pos.x <= Mathf.RoundToInt(bounds.max.x / cs) &&
               pos.y >= Mathf.RoundToInt(bounds.min.y / cs) &&
               pos.y <= Mathf.RoundToInt(bounds.max.y / cs);
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

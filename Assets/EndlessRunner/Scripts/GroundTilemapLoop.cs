using UnityEngine;
using UnityEngine.Tilemaps;

public class GroundTilemapLoop : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool moveOnlyWhenPlaying = true;
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool resetOnPlay = true;
    [SerializeField] private bool resetOnGameOver = false;
    #endregion

    #region Private Variables
    private TileBase[,] sourcePattern;
    private Vector3 initialLocalPosition;

    private int minX;
    private int minY;
    private int width;
    private int height;

    private int patternCursor;
    private float scrollAccumulator;
    private bool isInitialized;
    #endregion

    private void Start()
    {
        if (targetTilemap == null)
        {
            targetTilemap = GetComponent<Tilemap>();
        }

        Initialize();
        SubscribeGameEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeGameEvents();
    }

    private void Update()
    {
        if (!CanMove())
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float cellWidth = GetCellWidth();

        if (cellWidth <= 0f)
        {
            return;
        }

        scrollAccumulator += moveSpeed * deltaTime;

        while (scrollAccumulator >= cellWidth)
        {
            scrollAccumulator -= cellWidth;
            ShiftOneColumnLeft();
        }

        targetTilemap.transform.localPosition = initialLocalPosition + Vector3.left * scrollAccumulator;
    }

    private void Initialize()
    {
        if (targetTilemap == null)
        {
            return;
        }

        targetTilemap.CompressBounds();

        BoundsInt bounds = targetTilemap.cellBounds;
        width = bounds.size.x;
        height = bounds.size.y;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        minX = bounds.xMin;
        minY = bounds.yMin;

        sourcePattern = new TileBase[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cell = new Vector3Int(minX + x, minY + y, 0);
                sourcePattern[x, y] = targetTilemap.GetTile(cell);
            }
        }

        initialLocalPosition = targetTilemap.transform.localPosition;
        patternCursor = 0;
        scrollAccumulator = 0f;
        isInitialized = true;
    }

    private bool CanMove()
    {
        if (!isInitialized || targetTilemap == null)
        {
            return false;
        }

        if (!moveOnlyWhenPlaying)
        {
            return true;
        }

        return CubeGameManager.Instance != null && CubeGameManager.Instance.isPlaying;
    }

    private float GetCellWidth()
    {
        if (targetTilemap == null)
        {
            return 0f;
        }

        return Mathf.Abs(targetTilemap.layoutGrid.cellSize.x);
    }

    private void ShiftOneColumnLeft()
    {
        if (sourcePattern == null || width <= 0 || height <= 0)
        {
            return;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                Vector3Int fromCell = new Vector3Int(minX + x + 1, minY + y, 0);
                Vector3Int toCell = new Vector3Int(minX + x, minY + y, 0);
                targetTilemap.SetTile(toCell, targetTilemap.GetTile(fromCell));
            }

            Vector3Int rightMostCell = new Vector3Int(minX + width - 1, minY + y, 0);
            targetTilemap.SetTile(rightMostCell, sourcePattern[patternCursor, y]);
        }

        patternCursor++;
        if (patternCursor >= width)
        {
            patternCursor = 0;
        }
    }

    public void ResetLoop()
    {
        if (!isInitialized || targetTilemap == null || sourcePattern == null)
        {
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cell = new Vector3Int(minX + x, minY + y, 0);
                targetTilemap.SetTile(cell, sourcePattern[x, y]);
            }
        }

        patternCursor = 0;
        scrollAccumulator = 0f;
        targetTilemap.transform.localPosition = initialLocalPosition;
    }

    private void SubscribeGameEvents()
    {
        if (CubeGameManager.Instance == null)
        {
            return;
        }

        CubeGameManager.Instance.onPlay.AddListener(HandleOnPlay);
        CubeGameManager.Instance.onGameOver.AddListener(HandleOnGameOver);
    }

    private void UnsubscribeGameEvents()
    {
        if (CubeGameManager.Instance == null)
        {
            return;
        }

        CubeGameManager.Instance.onPlay.RemoveListener(HandleOnPlay);
        CubeGameManager.Instance.onGameOver.RemoveListener(HandleOnGameOver);
    }

    private void HandleOnPlay()
    {
        if (resetOnPlay)
        {
            ResetLoop();
        }
    }

    private void HandleOnGameOver()
    {
        if (resetOnGameOver)
        {
            ResetLoop();
        }
    }
}

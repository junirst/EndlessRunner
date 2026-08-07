using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class ProceduralWorldGenerator2D : MonoBehaviour
{
    public enum SeedMode
    {
        RandomEachStart,
        FixedSeed
    }

    #region Inspector Fields

    [Header("Tilemap")]
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private bool clearTilemapBeforeGenerate = true;

    [Header("World Size")]
    [SerializeField, Min(16)] private int width = 96;
    [SerializeField, Min(16)] private int height = 64;
    [SerializeField, Min(1)] private int borderThickness = 2;

    [Header("Seed")]
    [SerializeField] private SeedMode seedMode = SeedMode.RandomEachStart;
    [SerializeField] private int fixedSeed = 12345;
    [SerializeField] private bool logSeed = true;

    [Header("Biome Sprites")]
    [SerializeField] private Sprite[] grassSprites;
    [SerializeField] private Sprite[] dirtSprites;
    [SerializeField] private Sprite[] sandSprites;
    [SerializeField] private Sprite[] waterSprites;
    [SerializeField] private Sprite[] roadSprites;

    [Header("Biome Noise")]
    [SerializeField, Min(0.1f)] private float noiseScale = 18f;
    [SerializeField, Range(0f, 1f)] private float waterThreshold = 0.28f;
    [SerializeField, Range(0f, 1f)] private float sandThreshold = 0.42f;
    [SerializeField, Range(0f, 1f)] private float dirtThreshold = 0.66f;

    [Header("Road Network")]
    [SerializeField, Min(1)] private int roadCount = 4;
    [SerializeField, Min(1)] private int roadWidth = 2;
    [SerializeField, Min(0)] private int roadDetourOffset = 8;

    [Header("Player Setup")]
    [SerializeField] private Transform player;
    [SerializeField] private bool placePlayerAtSpawn = true;
    [SerializeField] private bool alignCameraToPlayer = true;
    [SerializeField] private Vector2Int spawnOffset;

    #endregion

    #region State

    private readonly Dictionary<Sprite, Tile> tileCache = new Dictionary<Sprite, Tile>();
    private System.Random random;
    private int currentSeed;

    #endregion

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        GenerateWorld();
    }

    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        ResolveReferences();

        if (!targetTilemap)
        {
            Debug.LogWarning($"{nameof(ProceduralWorldGenerator2D)} needs a Tilemap reference.", this);
            return;
        }

        currentSeed = seedMode == SeedMode.FixedSeed ? fixedSeed : CreateRandomSeed();
        random = new System.Random(currentSeed);

        if (clearTilemapBeforeGenerate)
        {
            targetTilemap.ClearAllTiles();
        }

        Vector3Int origin = new Vector3Int(-(width / 2), -(height / 2), 0);
        Vector2Int spawnCell = GetSpawnCell();

        FillBaseTerrain(origin);
        GenerateRoadNetwork(origin, spawnCell);

        targetTilemap.RefreshAllTiles();

        if (placePlayerAtSpawn)
        {
            PlacePlayerAtSpawn(origin, spawnCell);
        }

        if (alignCameraToPlayer)
        {
            AlignCameraToPlayer();
        }

        if (logSeed)
        {
            Debug.Log($"{nameof(ProceduralWorldGenerator2D)} generated seed {currentSeed} with size {width}x{height}.", this);
        }
    }

    [ContextMenu("Regenerate World")]
    public void RegenerateWorld()
    {
        GenerateWorld();
    }

    private void ResolveReferences()
    {
        if (!targetTilemap)
        {
            targetTilemap = GetComponentInChildren<Tilemap>();
        }

        if (!player)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject)
            {
                player = playerObject.transform;
            }
        }
    }

    private int CreateRandomSeed()
    {
        unchecked
        {
            int timeSeed = (int)DateTime.UtcNow.Ticks;
            int frameSeed = Time.frameCount;
            int noiseSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            return timeSeed ^ (frameSeed << 1) ^ noiseSeed;
        }
    }

    private Vector2Int GetSpawnCell()
    {
        int spawnX = Mathf.Clamp((width / 2) + spawnOffset.x, borderThickness, width - borderThickness - 1);
        int spawnY = Mathf.Clamp((height / 2) + spawnOffset.y, borderThickness, height - borderThickness - 1);
        return new Vector2Int(spawnX, spawnY);
    }

    private void FillBaseTerrain(Vector3Int origin)
    {
        float seedOffsetX = random.Next(-50000, 50000);
        float seedOffsetY = random.Next(-50000, 50000);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cell = origin + new Vector3Int(x, y, 0);

                if (IsBorderCell(x, y))
                {
                    SetRandomTile(cell, waterSprites);
                    continue;
                }

                float sample = Mathf.PerlinNoise((x + seedOffsetX) / noiseScale, (y + seedOffsetY) / noiseScale);
                Sprite sprite = PickBiomeSprite(sample);
                SetRandomTile(cell, new[] { sprite });
            }
        }
    }

    private void GenerateRoadNetwork(Vector3Int origin, Vector2Int spawnCell)
    {
        List<Vector2Int> anchors = new List<Vector2Int>();
        anchors.Add(spawnCell);

        for (int i = 0; i < roadCount; i++)
        {
            anchors.Add(GetRandomInteriorCell());
        }

        for (int i = 0; i < anchors.Count - 1; i++)
        {
            Vector2Int from = anchors[i];
            Vector2Int to = anchors[i + 1];

            Vector2Int bend = new Vector2Int(
                ClampCell(from.x + random.Next(-roadDetourOffset, roadDetourOffset + 1), width),
                ClampCell(to.y + random.Next(-roadDetourOffset, roadDetourOffset + 1), height));

            PaintRoadSegment(origin, from, bend);
            PaintRoadSegment(origin, bend, to);
        }
    }

    private void PaintRoadSegment(Vector3Int origin, Vector2Int start, Vector2Int end)
    {
        foreach (Vector2Int cell in GetLineCells(start, end))
        {
            PaintRoadBrush(origin, cell);
        }
    }

    private IEnumerable<Vector2Int> GetLineCells(Vector2Int start, Vector2Int end)
    {
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int error = dx - dy;

        while (true)
        {
            yield return new Vector2Int(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int doubledError = error * 2;

            if (doubledError > -dy)
            {
                error -= dy;
                x0 += sx;
            }

            if (doubledError < dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private void PaintRoadBrush(Vector3Int origin, Vector2Int centerCell)
    {
        for (int x = -roadWidth; x <= roadWidth; x++)
        {
            for (int y = -roadWidth; y <= roadWidth; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) > roadWidth)
                {
                    continue;
                }

                int cellX = ClampCell(centerCell.x + x, width);
                int cellY = ClampCell(centerCell.y + y, height);

                if (IsBorderCell(cellX, cellY))
                {
                    continue;
                }

                SetRandomTile(origin + new Vector3Int(cellX, cellY, 0), roadSprites);
            }
        }
    }

    private void PlacePlayerAtSpawn(Vector3Int origin, Vector2Int spawnCell)
    {
        if (!player)
        {
            return;
        }

        Vector3 worldPosition = targetTilemap.GetCellCenterWorld(origin + new Vector3Int(spawnCell.x, spawnCell.y, 0));

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = worldPosition;
        }

        player.position = worldPosition;
    }

    private void AlignCameraToPlayer()
    {
        if (!player || !Camera.main)
        {
            return;
        }

        SmoothCameraFollow cameraFollow = Camera.main.GetComponent<SmoothCameraFollow>();
        if (cameraFollow && cameraFollow.target == null)
        {
            cameraFollow.target = player;
        }
    }

    private void SetRandomTile(Vector3Int cell, Sprite[] sprites)
    {
        Sprite sprite = PickRandomSprite(sprites);
        if (!sprite)
        {
            return;
        }

        targetTilemap.SetTile(cell, GetTile(sprite));
    }

    private Tile GetTile(Sprite sprite)
    {
        if (!tileCache.TryGetValue(sprite, out Tile tile))
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            tile.hideFlags = HideFlags.HideAndDontSave;
            tileCache.Add(sprite, tile);
        }

        return tile;
    }

    private Sprite PickBiomeSprite(float sample)
    {
        if (sample < waterThreshold)
        {
            return PickRandomSprite(waterSprites);
        }

        if (sample < sandThreshold)
        {
            return PickRandomSprite(sandSprites);
        }

        if (sample < dirtThreshold)
        {
            return PickRandomSprite(dirtSprites);
        }

        return PickRandomSprite(grassSprites);
    }

    private Sprite PickRandomSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        return sprites[random.Next(0, sprites.Length)];
    }

    private bool IsBorderCell(int x, int y)
    {
        return x < borderThickness || y < borderThickness || x >= width - borderThickness || y >= height - borderThickness;
    }

    private Vector2Int GetRandomInteriorCell()
    {
        int minX = Mathf.Clamp(borderThickness + 1, 0, width - 1);
        int minY = Mathf.Clamp(borderThickness + 1, 0, height - 1);
        int maxX = Mathf.Clamp(width - borderThickness - 2, minX, width - 1);
        int maxY = Mathf.Clamp(height - borderThickness - 2, minY, height - 1);

        int x = random.Next(minX, maxX + 1);
        int y = random.Next(minY, maxY + 1);
        return new Vector2Int(x, y);
    }

    private int ClampCell(int value, int maxExclusive)
    {
        return Mathf.Clamp(value, 0, maxExclusive - 1);
    }
}
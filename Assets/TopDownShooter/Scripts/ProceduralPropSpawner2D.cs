using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class ProceduralPropSpawner2D : MonoBehaviour
{
    [Header("Source Map")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private ProceduralWorldGenerator2D worldGenerator;

    [Header("Spawn Rules")]
    [SerializeField, Min(1)] private int propCount = 35;
    [SerializeField, Min(0)] private int edgePaddingCells = 2;
    [SerializeField, Min(1)] private int cellsBetweenProps = 2;
    [SerializeField, Min(1)] private int maxAttemptsPerProp = 25;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool randomRotation = true;

    [Header("Water Filter")]
    [SerializeField] private Sprite[] waterSprites;

    [Header("Props")]
    [SerializeField] private GameObject[] propPrefabs;
    [SerializeField] private Transform propParent;
    [SerializeField] private bool addBoxColliderIfMissing = false;

    private readonly HashSet<Vector3Int> usedCells = new HashSet<Vector3Int>();
    private System.Random random;

    private void Awake()
    {
        ResolveReferences();
        random = new System.Random();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            StartCoroutine(SpawnAfterMapIsReady());
        }
    }

    [ContextMenu("Spawn Props")]
    public void SpawnProps()
    {
        ResolveReferences();

        if (!IsReady())
        {
            Debug.LogWarning($"{nameof(ProceduralPropSpawner2D)} needs a generated Tilemap and at least one prop prefab.", this);
            return;
        }

        ClearSpawnedProps();
        usedCells.Clear();

        BoundsInt bounds = GetSpawnBounds();
        int spawnedCount = 0;
        int safetyBudget = propCount * Mathf.Max(1, maxAttemptsPerProp);

        while (spawnedCount < propCount && safetyBudget-- > 0)
        {
            Vector3Int cell = GetRandomCell(bounds);
            if (!IsValidSpawnCell(cell))
            {
                continue;
            }

            if (!usedCells.Add(cell))
            {
                continue;
            }

            SpawnPropAtCell(cell);
            spawnedCount++;
        }

        Debug.Log($"{nameof(ProceduralPropSpawner2D)} spawned {spawnedCount}/{propCount} props.", this);
    }

    [ContextMenu("Clear Props")]
    public void ClearSpawnedProps()
    {
        if (!propParent)
        {
            Transform existing = transform.Find("SpawnedProps");
            if (existing)
            {
                propParent = existing;
            }
        }

        if (!propParent)
        {
            return;
        }

        for (int i = propParent.childCount - 1; i >= 0; i--)
        {
            Transform child = propParent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private IEnumerator SpawnAfterMapIsReady()
    {
        yield return null;
        SpawnProps();
    }

    private void ResolveReferences()
    {
        if (!groundTilemap)
        {
            groundTilemap = GetComponentInChildren<Tilemap>();
        }

        if (!worldGenerator)
        {
            worldGenerator = FindObjectOfType<ProceduralWorldGenerator2D>();
        }

        if (!propParent)
        {
            Transform existing = transform.Find("SpawnedProps");
            if (existing)
            {
                propParent = existing;
            }
            else
            {
                GameObject parentObject = new GameObject("SpawnedProps");
                parentObject.transform.SetParent(transform, false);
                propParent = parentObject.transform;
            }
        }
    }

    private bool IsReady()
    {
        return groundTilemap && propPrefabs != null && propPrefabs.Length > 0;
    }

    private BoundsInt GetSpawnBounds()
    {
        BoundsInt bounds = groundTilemap.cellBounds;
        int minX = bounds.xMin + edgePaddingCells;
        int minY = bounds.yMin + edgePaddingCells;
        int maxX = bounds.xMax - edgePaddingCells;
        int maxY = bounds.yMax - edgePaddingCells;

        if (worldGenerator != null)
        {
            // The generator already places the play area in a centered rectangle,
            // so using the tilemap bounds is safe, but the generator reference is
            // kept to make the relationship explicit in the Inspector.
        }

        return new BoundsInt(minX, minY, 0, Mathf.Max(1, maxX - minX), Mathf.Max(1, maxY - minY), 1);
    }

    private Vector3Int GetRandomCell(BoundsInt bounds)
    {
        int x = random.Next(bounds.xMin, bounds.xMax);
        int y = random.Next(bounds.yMin, bounds.yMax);
        return new Vector3Int(x, y, 0);
    }

    private bool IsValidSpawnCell(Vector3Int cell)
    {
        if (!groundTilemap.HasTile(cell))
        {
            return false;
        }

        Sprite tileSprite = groundTilemap.GetSprite(cell);
        if (tileSprite && IsWaterSprite(tileSprite))
        {
            return false;
        }

        if (cellsBetweenProps > 0)
        {
            for (int x = -cellsBetweenProps; x <= cellsBetweenProps; x++)
            {
                for (int y = -cellsBetweenProps; y <= cellsBetweenProps; y++)
                {
                    Vector3Int nearbyCell = new Vector3Int(cell.x + x, cell.y + y, 0);
                    if (usedCells.Contains(nearbyCell))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private bool IsWaterSprite(Sprite sprite)
    {
        if (waterSprites == null || waterSprites.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < waterSprites.Length; i++)
        {
            if (waterSprites[i] == sprite)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnPropAtCell(Vector3Int cell)
    {
        GameObject prefab = propPrefabs[random.Next(0, propPrefabs.Length)];
        if (!prefab)
        {
            return;
        }

        Vector3 worldPosition = groundTilemap.GetCellCenterWorld(cell);
        Quaternion rotation = randomRotation ? Quaternion.Euler(0f, 0f, random.Next(0, 4) * 90f) : Quaternion.identity;

        GameObject prop = Instantiate(prefab, worldPosition, rotation, propParent);
        prop.transform.position = worldPosition;

        if (addBoxColliderIfMissing && !prop.GetComponent<Collider2D>())
        {
            BoxCollider2D collider = prop.AddComponent<BoxCollider2D>();
            SpriteRenderer spriteRenderer = prop.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer)
            {
                collider.size = spriteRenderer.sprite ? spriteRenderer.sprite.bounds.size : Vector2.one;
            }
            collider.isTrigger = false;
        }
    }
}
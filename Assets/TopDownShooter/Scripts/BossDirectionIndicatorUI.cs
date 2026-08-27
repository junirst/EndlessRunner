using UnityEngine;

// Attach to a controller GameObject (not the arrow visual itself) so Update keeps running while the arrow is hidden.
public class BossDirectionIndicatorUI : MonoBehaviour
{
    [SerializeField] private RectTransform arrowRoot;
    [SerializeField] private GameObject arrowVisual;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0f)] private float viewportMargin = 0.05f;

    private CanvasGroup selfCanvasGroup;

    private void Awake()
    {
        if (!arrowRoot)
        {
            arrowRoot = GetComponent<RectTransform>();
        }

        if (!arrowVisual)
        {
            // No distinct child assigned: fall back to hiding via alpha on this object instead of SetActive.
            selfCanvasGroup = GetComponent<CanvasGroup>();
            if (!selfCanvasGroup)
            {
                selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void OnEnable()
    {
        BossEnemy.BossSpawned += HandleBossSpawned;
        BossEnemy.BossDespawned += HandleBossDespawned;
        UpdateVisibility(false);
    }

    private void OnDisable()
    {
        BossEnemy.BossSpawned -= HandleBossSpawned;
        BossEnemy.BossDespawned -= HandleBossDespawned;
    }

    private void HandleBossSpawned(BossEnemy boss)
    {
    }

    private void HandleBossDespawned(BossEnemy boss)
    {
        UpdateVisibility(false);
    }

    private void Update()
    {
        BossEnemy boss = BossEnemy.CurrentBoss;
        if (!boss)
        {
            UpdateVisibility(false);
            return;
        }

        if (!targetCamera)
        {
            targetCamera = Camera.main;
        }

        if (!targetCamera)
        {
            UpdateVisibility(false);
            return;
        }

        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(boss.transform.position);
        bool isOffScreen = viewportPoint.z < 0f
            || viewportPoint.x < viewportMargin || viewportPoint.x > 1f - viewportMargin
            || viewportPoint.y < viewportMargin || viewportPoint.y > 1f - viewportMargin;

        UpdateVisibility(isOffScreen);

        if (isOffScreen)
        {
            PointArrowAt(boss.transform.position);
        }
    }

    private void PointArrowAt(Vector3 worldPosition)
    {
        if (!arrowRoot || !targetCamera)
        {
            return;
        }

        Vector3 direction = worldPosition - targetCamera.transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        arrowRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateVisibility(bool visible)
    {
        if (arrowVisual)
        {
            arrowVisual.SetActive(visible);
        }
        else if (selfCanvasGroup)
        {
            selfCanvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}

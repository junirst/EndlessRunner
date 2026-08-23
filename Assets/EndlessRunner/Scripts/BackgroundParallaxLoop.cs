using System;
using UnityEngine;

public class BackgroundParallaxLoop : MonoBehaviour
{
    [Serializable]
    public class BackgroundLayer
    {
        public string layerName = "Layer";
        public Transform[] segments = new Transform[2];
        [Min(0f)] public float moveSpeed = 2f;
        [HideInInspector] public float segmentWidth = 20f;
    }

    #region Inspector Fields
    [SerializeField] private BackgroundLayer[] layers = new BackgroundLayer[5];
    [SerializeField] private Transform recycleReference;
    [SerializeField] private float recycleDistance = 25f;
    [SerializeField] private bool moveOnlyWhenPlaying = true;
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool autoDetectSegmentWidth = true;
    [SerializeField] private float fallbackSegmentWidth = 20f;
    [SerializeField] private bool resetLayersOnPlay = true;
    [SerializeField] private bool resetLayersOnGameOver = false;
    #endregion

    #region Private Variables
    private Vector3[][] initialSegmentPositions;
    private bool isInitialized;
    #endregion

    private void Awake()
    {
        EnsureFiveLayers();
    }

    private void Start()
    {
        InitializeLayers();
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

        for (int i = 0; i < layers.Length; i++)
        {
            MoveLayer(layers[i], deltaTime);
        }
    }

    private bool CanMove()
    {
        if (!isInitialized)
        {
            return false;
        }

        if (!moveOnlyWhenPlaying)
        {
            return true;
        }

        return CubeGameManager.Instance != null && CubeGameManager.Instance.isPlaying;
    }

    private void InitializeLayers()
    {
        EnsureFiveLayers();

        initialSegmentPositions = new Vector3[layers.Length][];

        for (int i = 0; i < layers.Length; i++)
        {
            BackgroundLayer layer = layers[i];
            if (layer == null)
            {
                continue;
            }

            if (layer.segments == null || layer.segments.Length == 0)
            {
                continue;
            }

            layer.segmentWidth = ResolveSegmentWidth(layer);
            initialSegmentPositions[i] = new Vector3[layer.segments.Length];

            for (int j = 0; j < layer.segments.Length; j++)
            {
                if (layer.segments[j] == null)
                {
                    continue;
                }

                initialSegmentPositions[i][j] = layer.segments[j].position;
            }
        }

        isInitialized = true;
    }

    private void MoveLayer(BackgroundLayer layer, float deltaTime)
    {
        if (layer == null || layer.segments == null || layer.segments.Length == 0)
        {
            return;
        }

        float moveStep = layer.moveSpeed * deltaTime;

        for (int i = 0; i < layer.segments.Length; i++)
        {
            if (layer.segments[i] == null)
            {
                continue;
            }

            layer.segments[i].position += Vector3.left * moveStep;
        }

        RecycleSegments(layer);
    }

    private void RecycleSegments(BackgroundLayer layer)
    {
        if (layer.segmentWidth <= 0f)
        {
            return;
        }

        float referenceX = recycleReference != null ? recycleReference.position.x : transform.position.x;
        float recycleLineX = referenceX - recycleDistance;

        float rightMostX = float.MinValue;
        for (int i = 0; i < layer.segments.Length; i++)
        {
            Transform segment = layer.segments[i];
            if (segment == null)
            {
                continue;
            }

            if (segment.position.x > rightMostX)
            {
                rightMostX = segment.position.x;
            }
        }

        for (int i = 0; i < layer.segments.Length; i++)
        {
            Transform segment = layer.segments[i];
            if (segment == null)
            {
                continue;
            }

            if (segment.position.x <= recycleLineX)
            {
                float nextX = rightMostX + layer.segmentWidth;
                segment.position = new Vector3(nextX, segment.position.y, segment.position.z);
                rightMostX = nextX;
            }
        }
    }

    private float ResolveSegmentWidth(BackgroundLayer layer)
    {
        if (!autoDetectSegmentWidth)
        {
            return Mathf.Max(0.1f, fallbackSegmentWidth);
        }

        if (layer.segments == null || layer.segments.Length == 0 || layer.segments[0] == null)
        {
            return Mathf.Max(0.1f, fallbackSegmentWidth);
        }

        SpriteRenderer spriteRenderer = layer.segments[0].GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return Mathf.Max(0.1f, spriteRenderer.bounds.size.x);
        }

        Renderer rendererComponent = layer.segments[0].GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            return Mathf.Max(0.1f, rendererComponent.bounds.size.x);
        }

        return Mathf.Max(0.1f, fallbackSegmentWidth);
    }

    private void SubscribeGameEvents()
    {
        if (CubeGameManager.Instance == null)
        {
            return;
        }

        CubeGameManager.Instance.onPlay.AddListener(HandlePlay);
        CubeGameManager.Instance.onGameOver.AddListener(HandleGameOver);
    }

    private void UnsubscribeGameEvents()
    {
        if (CubeGameManager.Instance == null)
        {
            return;
        }

        CubeGameManager.Instance.onPlay.RemoveListener(HandlePlay);
        CubeGameManager.Instance.onGameOver.RemoveListener(HandleGameOver);
    }

    private void HandlePlay()
    {
        if (resetLayersOnPlay)
        {
            ResetLayerPositions();
        }
    }

    private void HandleGameOver()
    {
        if (resetLayersOnGameOver)
        {
            ResetLayerPositions();
        }
    }

    public void ResetLayerPositions()
    {
        if (initialSegmentPositions == null)
        {
            return;
        }

        for (int i = 0; i < layers.Length; i++)
        {
            BackgroundLayer layer = layers[i];
            if (layer == null || layer.segments == null || i >= initialSegmentPositions.Length)
            {
                continue;
            }

            Vector3[] savedPositions = initialSegmentPositions[i];
            if (savedPositions == null)
            {
                continue;
            }

            int count = Mathf.Min(layer.segments.Length, savedPositions.Length);
            for (int j = 0; j < count; j++)
            {
                if (layer.segments[j] == null)
                {
                    continue;
                }

                layer.segments[j].position = savedPositions[j];
            }
        }
    }

    private void OnValidate()
    {
        EnsureFiveLayers();
    }

    private void EnsureFiveLayers()
    {
        if (layers == null)
        {
            layers = new BackgroundLayer[5];
        }

        if (layers.Length != 5)
        {
            Array.Resize(ref layers, 5);
        }

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null)
            {
                layers[i] = new BackgroundLayer
                {
                    layerName = "Layer " + (i + 1),
                    segments = new Transform[2],
                    moveSpeed = 2f + i
                };
            }
            else if (string.IsNullOrWhiteSpace(layers[i].layerName))
            {
                layers[i].layerName = "Layer " + (i + 1);
            }
        }
    }
}

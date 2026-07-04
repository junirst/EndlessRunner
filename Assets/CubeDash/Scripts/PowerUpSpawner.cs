using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] private Transform powerUpParent;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float obstacleCheckRadius = 0.45f;

    public float powerUpSpawnTime = 6f;
    [Range(0, 1)] public float powerUpSpawnTimeFactor = 0.08f;
    public float powerUpSpeed = 4f;
    [Range(0, 1)] public float powerUpSpeedFactor = 0.1f;

    private float _powerUpSpawnTime;
    private float _powerUpSpeed;

    private float timeAlive;
    private float timeUntilPowerUpSpawn;

    private void Start()
    {
        if (CubeGameManager.Instance != null)
        {
            CubeGameManager.Instance.onGameOver.AddListener(ClearPowerUps);
            CubeGameManager.Instance.onPlay.AddListener(ResetFactors);
        }

        ResetFactors();
    }

    private void OnDestroy()
    {
        if (CubeGameManager.Instance != null)
        {
            CubeGameManager.Instance.onGameOver.RemoveListener(ClearPowerUps);
            CubeGameManager.Instance.onPlay.RemoveListener(ResetFactors);
        }
    }

    private void Update()
    {
        if (CubeGameManager.Instance != null && CubeGameManager.Instance.isPlaying)
        {
            timeAlive += Time.deltaTime;
            CalculateFactors();
            SpawnLoop();
        }
    }

    private void SpawnLoop()
    {
        timeUntilPowerUpSpawn += Time.deltaTime;

        if (timeUntilPowerUpSpawn >= _powerUpSpawnTime)
        {
            Spawn();
            timeUntilPowerUpSpawn = 0f;
        }
    }

    private void ClearPowerUps()
    {
        if (powerUpParent == null)
        {
            return;
        }

        foreach (Transform child in powerUpParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void CalculateFactors()
    {
        _powerUpSpawnTime = powerUpSpawnTime / Mathf.Pow(timeAlive, powerUpSpawnTimeFactor);
        _powerUpSpeed = powerUpSpeed * Mathf.Pow(timeAlive, powerUpSpeedFactor);
    }

    private void ResetFactors()
    {
        timeAlive = 1f;
        timeUntilPowerUpSpawn = 0f;
        _powerUpSpawnTime = powerUpSpawnTime;
        _powerUpSpeed = powerUpSpeed;
    }

    private void Spawn()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0)
        {
            return;
        }

        if (!TryGetSpawnPoint(out Transform spawnPoint))
        {
            return;
        }

        GameObject powerUpToSpawn = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
        GameObject spawnedPowerUp = Instantiate(powerUpToSpawn, spawnPoint.position, spawnPoint.rotation);

        if (powerUpParent != null)
        {
            spawnedPowerUp.transform.SetParent(powerUpParent);
        }

        Rigidbody2D powerUpRB = spawnedPowerUp.GetComponent<Rigidbody2D>();
        if (powerUpRB != null)
        {
            powerUpRB.velocity = Vector2.left * _powerUpSpeed;
        }
    }

    private bool TryGetSpawnPoint(out Transform spawnPoint)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoint = transform;
            return !IsBlocked(transform.position);
        }

        int startIndex = Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform candidate = spawnPoints[(startIndex + i) % spawnPoints.Length];

            if (candidate == null)
            {
                continue;
            }

            if (!IsBlocked(candidate.position))
            {
                spawnPoint = candidate;
                return true;
            }
        }

        spawnPoint = null;
        return false;
    }

    private bool IsBlocked(Vector2 position)
    {
        return Physics2D.OverlapCircle(position, obstacleCheckRadius, obstacleLayerMask) != null;
    }
}

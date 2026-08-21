using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float spawnRate = 1f;
    [SerializeField, Min(0.01f)] private float minimumSpawnRate = 0.25f;
    [SerializeField, Min(1f)] private float spawnRateRampDuration = 90f;
    [SerializeField] private GameObject[] enemyPrefab;
    [Header("Boss Spawn")]
    [SerializeField, Min(1f)] private float bossSpawnInterval = 60f;
    [SerializeField, Min(0f)] private float firstBossSpawnDelay = 30f;
    [SerializeField] private GameObject[] bossPrefabs;
    [SerializeField] private bool limitToOneAliveBoss = true;
    [SerializeField] private bool canSpawn = true;
    [SerializeField] private ProceduralWorldGenerator2D worldGenerator;
    [SerializeField] private bool spawnInsideGeneratedMap = true;

    private void Start()
    {
        if (!worldGenerator)
        {
            worldGenerator = FindObjectOfType<ProceduralWorldGenerator2D>();
        }

        StartCoroutine(Spawner());

        if (bossPrefabs != null && bossPrefabs.Length > 0)
        {
            StartCoroutine(BossSpawner());
        }
    }

    private IEnumerator Spawner()
    {
        while (canSpawn)
        {
            yield return new WaitForSeconds(GetCurrentSpawnDelay());

            if (!canSpawn)
            {
                yield break;
            }

            int rand = Random.Range(0, enemyPrefab.Length);
            GameObject enemyToSpawn = enemyPrefab[rand];

            Vector3 spawnPosition = GetSpawnPosition();
            Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
        }   
    }

    private IEnumerator BossSpawner()
    {
        if (firstBossSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(firstBossSpawnDelay);
        }

        while (canSpawn)
        {
            TrySpawnBoss();
            yield return new WaitForSeconds(bossSpawnInterval);
        }
    }

    private void TrySpawnBoss()
    {
        if (!canSpawn || bossPrefabs == null || bossPrefabs.Length == 0)
        {
            return;
        }

        if (limitToOneAliveBoss && FindObjectOfType<BossEnemy>() != null)
        {
            return;
        }

        int rand = Random.Range(0, bossPrefabs.Length);
        GameObject bossToSpawn = bossPrefabs[rand];

        if (!bossToSpawn)
        {
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Instantiate(bossToSpawn, spawnPosition, Quaternion.identity);
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 spawnPosition = transform.position;
        if (spawnInsideGeneratedMap && worldGenerator && worldGenerator.TryGetRandomEnemySpawnPosition(out Vector3 generatedPosition))
        {
            spawnPosition = generatedPosition;
        }

        return spawnPosition;
    }

    private float GetCurrentSpawnDelay()
    {
        float elapsedTime = Time.timeSinceLevelLoad;
        float rampT = Mathf.Clamp01(elapsedTime / spawnRateRampDuration);
        return Mathf.Max(minimumSpawnRate, Mathf.Lerp(spawnRate, minimumSpawnRate, rampT));
    }
}

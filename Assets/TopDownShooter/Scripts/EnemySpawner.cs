using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float spawnRate = 1f;
    [SerializeField, Min(0.01f)] private float minimumSpawnRate = 0.25f;
    [SerializeField, Min(1f)] private float spawnRateRampDuration = 90f;
    [SerializeField] private GameObject[] enemyPrefab;
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

            Vector3 spawnPosition = transform.position;
            if (spawnInsideGeneratedMap && worldGenerator && worldGenerator.TryGetRandomEnemySpawnPosition(out Vector3 generatedPosition))
            {
                spawnPosition = generatedPosition;
            }

            Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
        }   
    }

    private float GetCurrentSpawnDelay()
    {
        float elapsedTime = Time.timeSinceLevelLoad;
        float rampT = Mathf.Clamp01(elapsedTime / spawnRateRampDuration);
        return Mathf.Max(minimumSpawnRate, Mathf.Lerp(spawnRate, minimumSpawnRate, rampT));
    }
}

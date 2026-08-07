using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float spawnRate = 1f;
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
        WaitForSeconds wait = new WaitForSeconds(spawnRate);

        while (canSpawn)
        {
            yield return wait;
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
}

using System.Collections;
using UnityEngine;

public class WeaponPrefabSpawner : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float spawnRate = 10f;
    [SerializeField] private GameObject[] weaponPrefabs;
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
            yield return new WaitForSeconds(spawnRate);

            if (!canSpawn || weaponPrefabs == null || weaponPrefabs.Length == 0)
            {
                continue;
            }

            int randomIndex = Random.Range(0, weaponPrefabs.Length);
            GameObject weaponToSpawn = weaponPrefabs[randomIndex];
            if (!weaponToSpawn)
            {
                continue;
            }

            Vector3 spawnPosition = transform.position;
            if (spawnInsideGeneratedMap && worldGenerator && worldGenerator.TryGetRandomEnemySpawnPosition(out Vector3 generatedPosition))
            {
                spawnPosition = generatedPosition;
            }

            Instantiate(weaponToSpawn, spawnPosition, Quaternion.identity);
        }
    }
}
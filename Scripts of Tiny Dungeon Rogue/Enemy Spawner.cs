using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject swarmerPrefab;
    [SerializeField] private GameObject bigSwarmerPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float swarmerInterval = 3.5f;
    [SerializeField] private float bigSwarmerInterval = 10f;
    
    void Start()
    {
        StartCoroutine(spawnEnemy(swarmerInterval, swarmerPrefab));
        StartCoroutine(spawnEnemy(bigSwarmerInterval, bigSwarmerPrefab));
    }

    private IEnumerator spawnEnemy(float interval, GameObject enemy)
    {
        yield return new WaitForSeconds(interval);
        GameObject newEnemy = Instantiate(enemy, new Vector3(Random.Range(-5f, 5), Random.Range(-6f, 6f), 0f), Quaternion.identity);
        StartCoroutine(spawnEnemy(interval, enemy));
    }

 
}

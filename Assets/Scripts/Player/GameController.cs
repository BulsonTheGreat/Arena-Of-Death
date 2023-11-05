using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [System.Serializable]
    public class WaveContent
    {
        [SerializeField] GameObject[] monsters;
        [SerializeField] float[] spawnDelays;
        private List<(GameObject, float)> monsterSpawner;

        public List<(GameObject, float)> GetMonsterSpawnList()
        {
            if (monsterSpawner == null)
            {
                monsterSpawner = new List<(GameObject, float)>();
                for (int i = 0; i < monsters.Length; i++)
                {
                    monsterSpawner.Add((monsters[i], spawnDelays[i]));
                }
            }
            return monsterSpawner;
        }
    }

    [SerializeField] WaveContent[] waves;
    List<Transform> SpawnPoints = new List<Transform>();
    ThirdPersonController player;
    int currentWave = 0;
    public int enemiesKilled { get; private set; }

    public void IncrementEnemiesKilled()
    {
        enemiesKilled++;
    }

    void Start()
    {
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("Spawnpoint");
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();

        foreach (GameObject spawnPointObject in spawnPointObjects)
        {
            SpawnPoints.Add(spawnPointObject.transform);
        }
        StartCoroutine(SpawnWave());
        Debug.Log("Current wave: " + (currentWave + 1));
    }

    // Update is called once per frame
    void Update()
    {
        if (enemiesKilled == waves[currentWave].GetMonsterSpawnList().Count)
        {
            enemiesKilled = 0;
            currentWave++;
            player.FullHealth();
            Debug.Log("Current wave: " + (currentWave + 1));
            StartCoroutine(SpawnWave());
        }
        if(currentWave >= waves.Length) 
        {
            StartCoroutine(EndGame());
        }

    }

    private IEnumerator SpawnWave()
    {
        List<(GameObject, float)> monsterSpawnList = waves[currentWave].GetMonsterSpawnList();

        foreach ((GameObject monster, float delay) in monsterSpawnList)
        {
            int r = UnityEngine.Random.Range(0, SpawnPoints.Count);
            GameObject monsterPrefab = monster;
            float spawnDelay = delay;

            yield return new WaitForSeconds(spawnDelay);
            Instantiate(monsterPrefab, SpawnPoints[r].transform.position, Quaternion.identity);
        }
    }

    private IEnumerator EndGame()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(3);
    }
}


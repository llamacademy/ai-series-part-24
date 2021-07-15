using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public Player Player;
    public int NumberOfEnemiesToSpawn = 5;
    public float SpawnDelay = 1f;
    public List<WeightedSpawnScriptableObject> WeightedEnemies = new List<WeightedSpawnScriptableObject>();
    public ScalingScriptableObject Scaling;
    public SpawnMethod EnemySpawnMethod = SpawnMethod.RoundRobin;
    public bool ContinuousSpawning;
    [Space]
    [Header("Read At Runtime")]
    [SerializeField]
    private int Level = 0;
    [SerializeField]
    private List<EnemyScriptableObject> ScaledEnemies = new List<EnemyScriptableObject>();
    [SerializeField]
    private float[] Weights;

    private int EnemiesAlive = 0;
    private int SpawnedEnemies = 0;
    private int InitialEnemiesToSpawn;
    private float InitialSpawnDelay;


    private NavMeshTriangulation Triangulation;
    private Dictionary<int, ObjectPool> EnemyObjectPools = new Dictionary<int, ObjectPool>();

    private void Awake()
    {
        for (int i = 0; i < WeightedEnemies.Count; i++)
        {
            EnemyObjectPools.Add(i, ObjectPool.CreateInstance(WeightedEnemies[i].Enemy.Prefab, NumberOfEnemiesToSpawn));
        }

        Weights = new float[WeightedEnemies.Count];
        InitialEnemiesToSpawn = NumberOfEnemiesToSpawn;
        InitialSpawnDelay = SpawnDelay;
    }

    private void Start()
    {
        Triangulation = NavMesh.CalculateTriangulation();

        for (int i = 0; i < WeightedEnemies.Count; i++)
        {
            ScaledEnemies.Add(WeightedEnemies[i].Enemy.ScaleUpForLevel(Scaling, 0));
        }

        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        Level++;
        SpawnedEnemies = 0;
        EnemiesAlive = 0;
        for (int i = 0; i < WeightedEnemies.Count; i++)
        {
            ScaledEnemies[i] = WeightedEnemies[i].Enemy.ScaleUpForLevel(Scaling, Level);
        }

        ResetSpawnWeights();

        WaitForSeconds Wait = new WaitForSeconds(SpawnDelay);

        while (SpawnedEnemies < NumberOfEnemiesToSpawn)
        {
            if (EnemySpawnMethod == SpawnMethod.RoundRobin)
            {
                SpawnRoundRobinEnemy(SpawnedEnemies);
            }
            else if (EnemySpawnMethod == SpawnMethod.Random)
            {
                SpawnRandomEnemy();
            }
            else if (EnemySpawnMethod == SpawnMethod.WeightedRandom)
            {
                SpawnWeightedRandomEnemy();
            }

            SpawnedEnemies++;

            yield return Wait;
        }

        if (ContinuousSpawning)
        {
            ScaleUpSpawns();
            StartCoroutine(SpawnEnemies());
        }
    }

    private void ResetSpawnWeights()
    {
        float TotalWeight = 0;

        for (int i = 0; i < WeightedEnemies.Count; i++)
        {
            Weights[i] = WeightedEnemies[i].GetWeight();
            TotalWeight += Weights[i];
        }

        for (int i = 0; i < Weights.Length; i++)
        {
            Weights[i] = Weights[i] / TotalWeight;
        }
    }

    private void SpawnRoundRobinEnemy(int SpawnedEnemies)
    {
        int SpawnIndex = SpawnedEnemies % WeightedEnemies.Count;

        DoSpawnEnemy(SpawnIndex, ChooseRandomPositionOnNavMesh());
    }

    private void SpawnRandomEnemy()
    {
        DoSpawnEnemy(Random.Range(0, WeightedEnemies.Count), ChooseRandomPositionOnNavMesh());
    }

    private void SpawnWeightedRandomEnemy()
    {
        float Value = Random.value;

        for (int i = 0; i < Weights.Length; i++)
        {
            if (Value < Weights[i])
            {
                DoSpawnEnemy(i, ChooseRandomPositionOnNavMesh());
                return;
            }

            Value -= Weights[i];
        }

        Debug.LogError("Invalid configuration! Could not spawn a Weighted Random Enemy. Did you forget to call ResetSpawnWeights()?");
    }

    private Vector3 ChooseRandomPositionOnNavMesh()
    {
        int VertexIndex = Random.Range(0, Triangulation.vertices.Length);
        return Triangulation.vertices[VertexIndex];
    }

    public void DoSpawnEnemy(int SpawnIndex, Vector3 SpawnPosition)
    {
        PoolableObject poolableObject = EnemyObjectPools[SpawnIndex].GetObject();

        if (poolableObject != null)
        {
            Enemy enemy = poolableObject.GetComponent<Enemy>();
            ScaledEnemies[SpawnIndex].SetupEnemy(enemy);


            NavMeshHit Hit;
            if (NavMesh.SamplePosition(SpawnPosition, out Hit, 2f, -1))
            {
                enemy.Agent.Warp(Hit.position);
                // enemy needs to get enabled and start chasing now.
                enemy.Movement.Player = Player.transform;
                enemy.Movement.Triangulation = Triangulation;
                enemy.Agent.enabled = true;
                enemy.Movement.Spawn();
                enemy.OnDie += HandleEnemyDeath;
                enemy.Level = Level;
                enemy.Skills = ScaledEnemies[SpawnIndex].Skills;
                enemy.Player = Player;

                EnemiesAlive++;
            }
            else
            {
                Debug.LogError($"Unable to place NavMeshAgent on NavMesh. Tried to use {SpawnPosition}");
            }
        }
        else
        {
            Debug.LogError($"Unable to fetch enemy of type {SpawnIndex} from object pool. Out of objects?");
        }
    }

    private void ScaleUpSpawns()
    {
        NumberOfEnemiesToSpawn = Mathf.FloorToInt(InitialEnemiesToSpawn * Scaling.SpawnCountCurve.Evaluate(Level + 1));
        SpawnDelay = InitialSpawnDelay * Scaling.SpawnRateCurve.Evaluate(Level + 1);
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        EnemiesAlive--;

        if (EnemiesAlive == 0 && SpawnedEnemies == NumberOfEnemiesToSpawn)
        {
            ScaleUpSpawns();
            StartCoroutine(SpawnEnemies());
        }
    }


    public enum SpawnMethod
    {
        RoundRobin,
        Random,
        WeightedRandom
        // Other spawn methods can be added here
    }
}

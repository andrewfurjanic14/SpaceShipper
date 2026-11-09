using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform cargoship;
    [SerializeField] private float spawnDistance = 350f;

    public int AliveCount { get; private set; }

    private void Awake()
    {
        if (cargoship == null)
            cargoship = GameObject.Find("cargoship")?.transform;
    }

    // difficulty: 1 = base stats, 2 = double health / speed / fire rate, etc.
    public void SpawnWave(int count, float difficulty)
    {
        if (cargoship == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = cargoship.position + Random.onUnitSphere * spawnDistance;
            Quaternion rot = Quaternion.LookRotation(cargoship.position - pos);
            GameObject enemy = Instantiate(enemyPrefab, pos, rot);

            enemy.GetComponent<Health>().ScaleMaxHealth(difficulty);
            enemy.GetComponent<EnemyMovement>().ScaleSpeed(difficulty);
            enemy.GetComponentInChildren<Shooter>().FireRateMultiplier = difficulty;

            // Count the enemy as gone the first time it dies
            AliveCount++;
            Health health = enemy.GetComponent<Health>();
            System.Action onDied = null;
            onDied = () => { AliveCount--; health.Died -= onDied; };
            health.Died += onDied;
        }
    }
}
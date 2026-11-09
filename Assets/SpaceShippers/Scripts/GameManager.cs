using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private int firstWaveSize = 3;
    [SerializeField] private int extraEnemiesPerWave = 2;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float difficultyPerWave = 0.15f; // +15% stats each wave

    public int Wave { get; private set; }

    private IEnumerator Start()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenWaves);

            Wave++;
            float difficulty = 1f + (Wave - 1) * difficultyPerWave;
            spawner.SpawnWave(firstWaveSize + (Wave - 1) * extraEnemiesPerWave, difficulty);

            // Next wave starts once every enemy is dead
            yield return new WaitUntil(() => spawner.AliveCount <= 0);
        }
    }
}
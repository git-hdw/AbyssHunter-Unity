using System;
using System.Collections;
using UnityEngine;

namespace AbyssHunter.Enemy
{
    public sealed class EnemyWaveSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Waves")]
        [SerializeField, Min(1)] private int numberOfWaves = 3;
        [SerializeField, Min(1)] private int enemiesInFirstWave = 3;
        [SerializeField, Min(0)] private int additionalEnemiesPerWave = 1;
        [SerializeField, Min(0f)] private float startDelay = 1f;
        [SerializeField, Min(0.01f)] private float timeBetweenSpawns = 0.75f;
        [SerializeField, Min(0.01f)] private float timeBetweenWaves = 5f;

        public int CurrentWave { get; private set; }
        public int TotalWaves => numberOfWaves;
        public int ActiveEnemies { get; private set; }

        public event Action<int, int> WaveStarted;
        public event Action<int> RemainingEnemiesChanged;
        public event Action AllWavesCompleted;

        private int nextSpawnIndex;

        private IEnumerator Start()
        {
            if (enemyPool == null || playerTarget == null ||
                spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("Wave Spawner 的引用没有配置完整。", this);
                yield break;
            }

            yield return enemyPool.WaitUntilReady();

            if (enemyPool.HasFailed)
            {
                Debug.LogError(
                    "Enemy Pool 初始化失败，波次生成已停止。",
                    this);
                yield break;
            }

            nextSpawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            yield return new WaitForSeconds(startDelay);

            for (int wave = 1; wave <= numberOfWaves; wave++)
            {
                CurrentWave = wave;
                int enemyCount = enemiesInFirstWave + (wave - 1) * additionalEnemiesPerWave;

                Debug.Log($"第 {CurrentWave} 波开始，敌人数量：{enemyCount}", this);
                WaveStarted?.Invoke(CurrentWave, numberOfWaves);

                for (int i = 0; i < enemyCount; i++)
                {
                    SpawnEnemy();
                    yield return new WaitForSeconds(timeBetweenSpawns);
                }

                while (ActiveEnemies > 0)
                {
                    yield return null;
                }

                if (wave < numberOfWaves)
                {
                    yield return new WaitForSeconds(timeBetweenWaves);
                }
            }

            Debug.Log("所有波次已完成。", this);
            AllWavesCompleted?.Invoke();
        }

        private void SpawnEnemy()
        {
            Transform spawnPoint = spawnPoints[nextSpawnIndex];
            nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;

            PooledEnemy enemy = enemyPool.Get(spawnPoint.position, spawnPoint.rotation);
            if (enemy == null)
            {
                return;
            }

            enemy.Controller.SetTarget(playerTarget, alertImmediately: true);
            enemy.Defeated -= HandleEnemyDefeated;
            enemy.Defeated += HandleEnemyDefeated;

            ActiveEnemies++;
            RemainingEnemiesChanged?.Invoke(ActiveEnemies);
        }

        private void HandleEnemyDefeated(PooledEnemy enemy)
        {
            enemy.Defeated -= HandleEnemyDefeated;
            ActiveEnemies = Mathf.Max(0, ActiveEnemies - 1);
            RemainingEnemiesChanged?.Invoke(ActiveEnemies);
        }
    }
}

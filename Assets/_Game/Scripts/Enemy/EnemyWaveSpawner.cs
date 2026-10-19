using System;
using System.Collections;
using UnityEngine;

namespace AbyssHunter.Enemy
{
    public sealed class EnemyWaveSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private EnemyPool heavyEnemyPool;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Waves")]
        [SerializeField, Min(1)] private int numberOfWaves = 3;
        [SerializeField, Min(1)] private int enemiesInFirstWave = 3;
        [SerializeField, Min(0)] private int additionalEnemiesPerWave = 1;
        [SerializeField, Min(0f)] private float startDelay = 1f;
        [SerializeField, Min(0.01f)] private float timeBetweenSpawns = 0.75f;
        [SerializeField, Min(0.01f)] private float timeBetweenWaves = 5f;
        [Serializable]
        public struct Encounter
        {
            public string title;
            [Min(0)] public int normalCount;
            [Min(0)] public int heavyCount;
            public Transform approach;
            public Transform[] positions;
            [Min(1)] public float activationRadius;
        }
        [SerializeField] private Encounter[] encounters;

        public int CurrentWave { get; private set; }
        public int TotalWaves => encounters != null && encounters.Length > 0 ? encounters.Length : numberOfWaves;
        public string CurrentEncounterTitle => encounters != null && CurrentWave > 0 && CurrentWave <= encounters.Length
            ? encounters[CurrentWave - 1].title : "";
        public int ActiveEnemies { get; private set; }
        public int EnemiesLeftInWave { get; private set; }
        public int CompletedEncounters { get; private set; }
        public bool SpatialEncounters => encounters != null && encounters.Length > 0 && encounters[0].approach != null;
        public Transform NextApproach => SpatialEncounters && CompletedEncounters < encounters.Length ? encounters[CompletedEncounters].approach : null;

        public event Action<int, int> WaveStarted;
        public event Action<int> RemainingEnemiesChanged;
        public event Action AllWavesCompleted;
        public event Action EnemyDefeated;
        public event Action<int> EncounterCompleted;

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
            if (heavyEnemyPool != null) yield return heavyEnemyPool.WaitUntilReady();

            if (enemyPool.HasFailed || (heavyEnemyPool != null && heavyEnemyPool.HasFailed))
            {
                Debug.LogError(
                    "Enemy Pool 初始化失败，波次生成已停止。",
                    this);
                yield break;
            }

            nextSpawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            if (SpatialEncounters)
                foreach (var encounter in encounters)
                    if (encounter.approach == null || encounter.positions == null || encounter.positions.Length == 0 ||
                        Array.Exists(encounter.positions, point => point == null))
                    { Debug.LogError("遗迹遭遇缺少接近点或出生点。", this); yield break; }
            yield return new WaitForSeconds(startDelay);

            for (int wave = 1; wave <= TotalWaves; wave++)
            {
                if (SpatialEncounters)
                {
                    var encounter = encounters[wave-1];
                    while (playerTarget != null && Vector3.Distance(playerTarget.position, encounter.approach.position) > encounter.activationRadius)
                        yield return null;
                }
                CurrentWave = wave;
                bool configured = encounters != null && encounters.Length > 0;
                int normalCount = configured ? encounters[wave-1].normalCount : enemiesInFirstWave + (wave-1)*additionalEnemiesPerWave;
                int heavyCount = configured ? encounters[wave-1].heavyCount : 0;
                if (heavyCount > 0 && heavyEnemyPool == null) {Debug.LogError("Encounter requires a heavy enemy pool.",this);yield break;}
                int enemyCount = normalCount + heavyCount;
                EnemiesLeftInWave = enemyCount;

                Debug.Log($"第 {CurrentWave} 波开始，敌人数量：{enemyCount}", this);
                WaveStarted?.Invoke(CurrentWave, TotalWaves);

                for (int i = 0; i < enemyCount; i++)
                {
                    SpawnEnemy(i < heavyCount ? heavyEnemyPool : enemyPool, i);
                    yield return new WaitForSeconds(timeBetweenSpawns);
                }

                while (ActiveEnemies > 0)
                {
                    yield return null;
                }

                CompletedEncounters = wave;
                EncounterCompleted?.Invoke(wave);

                if (wave < TotalWaves)
                {
                    yield return new WaitForSeconds(timeBetweenWaves);
                }
            }

            Debug.Log("所有波次已完成。", this);
            AllWavesCompleted?.Invoke();
        }

        private void SpawnEnemy(EnemyPool pool, int index)
        {
            var positions = SpatialEncounters ? encounters[CurrentWave-1].positions : spawnPoints;
            Transform spawnPoint = positions[SpatialEncounters ? index % positions.Length : nextSpawnIndex];
            nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;

            PooledEnemy enemy = pool.Get(spawnPoint.position, spawnPoint.rotation);
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
            EnemiesLeftInWave = Mathf.Max(0, EnemiesLeftInWave - 1);
            EnemyDefeated?.Invoke();
            RemainingEnemiesChanged?.Invoke(ActiveEnemies);
        }
    }
}

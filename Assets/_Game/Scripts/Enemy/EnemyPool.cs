using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AbyssHunter.Enemy
{
    public sealed class EnemyPool : MonoBehaviour
    {
        [SerializeField] private AssetReferenceGameObject enemyPrefabReference;
        [SerializeField, Min(0)] private int initialSize = 5;

        private readonly Queue<PooledEnemy> availableEnemies = new();
        private AsyncOperationHandle<GameObject> prefabLoadHandle;
        private GameObject loadedEnemyPrefab;

        public int AvailableCount => availableEnemies.Count;
        public bool IsReady { get; private set; }
        public bool HasFailed { get; private set; }

        private IEnumerator Start()
        {
            if (enemyPrefabReference == null ||
                !enemyPrefabReference.RuntimeKeyIsValid())
            {
                FailInitialization("Enemy Pool 缺少有效的 Addressable Enemy Prefab。");
                yield break;
            }

            prefabLoadHandle =
                enemyPrefabReference.LoadAssetAsync<GameObject>();
            yield return prefabLoadHandle;

            if (prefabLoadHandle.Status != AsyncOperationStatus.Succeeded ||
                prefabLoadHandle.Result == null)
            {
                FailInitialization("Addressables 加载 Enemy Prefab 失败。");
                yield break;
            }

            loadedEnemyPrefab = prefabLoadHandle.Result;

            if (!loadedEnemyPrefab.TryGetComponent(out PooledEnemy _))
            {
                FailInitialization("Addressable Enemy Prefab 缺少 PooledEnemy 组件。");
                yield break;
            }

            for (int i = 0; i < initialSize; i++)
            {
                availableEnemies.Enqueue(CreateEnemy());
            }

            IsReady = true;
            Debug.Log($"Enemy Pool 异步加载完成，已预热 {initialSize} 个敌人。", this);
        }

        public IEnumerator WaitUntilReady()
        {
            while (!IsReady && !HasFailed)
            {
                yield return null;
            }
        }

        public PooledEnemy Get(Vector3 position, Quaternion rotation)
        {
            if (!IsReady)
            {
                Debug.LogWarning("Enemy Pool 尚未初始化完成。", this);
                return null;
            }

            PooledEnemy enemy = availableEnemies.Count > 0
                ? availableEnemies.Dequeue()
                : CreateEnemy();

            enemy.transform.SetPositionAndRotation(position, rotation);
            enemy.gameObject.SetActive(true);
            return enemy;
        }

        private PooledEnemy CreateEnemy()
        {
            GameObject instance = Instantiate(loadedEnemyPrefab, transform);
            PooledEnemy enemy = instance.GetComponent<PooledEnemy>();
            enemy.Initialize(ReturnToPool);
            enemy.gameObject.SetActive(false);
            return enemy;
        }

        private void ReturnToPool(PooledEnemy enemy)
        {
            enemy.transform.SetParent(transform);
            availableEnemies.Enqueue(enemy);
        }

        private void FailInitialization(string message)
        {
            HasFailed = true;
            Debug.LogError(message, this);
        }

        private void OnDestroy()
        {
            if (prefabLoadHandle.IsValid())
            {
                Addressables.Release(prefabLoadHandle);
            }
        }
    }
}

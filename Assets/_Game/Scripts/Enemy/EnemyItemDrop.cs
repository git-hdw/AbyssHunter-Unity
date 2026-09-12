using AbyssHunter.Inventory;
using UnityEngine;

namespace AbyssHunter.Enemy
{
    [RequireComponent(typeof(PooledEnemy))]
    public sealed class EnemyItemDrop : MonoBehaviour
    {
        [SerializeField] private PickupItem pickupPrefab;
        [SerializeField] private ItemData item;
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
        [SerializeField, Min(1)] private int minimumQuantity = 1;
        [SerializeField, Min(1)] private int maximumQuantity = 3;

        private PooledEnemy pooledEnemy;

        private void Awake()
        {
            pooledEnemy = GetComponent<PooledEnemy>();
        }

        private void OnEnable()
        {
            pooledEnemy.Defeated += DropItem;
        }

        private void OnDisable()
        {
            pooledEnemy.Defeated -= DropItem;
        }

        private void DropItem(PooledEnemy defeatedEnemy)
        {
            if (pickupPrefab == null || item == null || Random.value > dropChance)
            {
                return;
            }

            int min = Mathf.Min(minimumQuantity, maximumQuantity);
            int max = Mathf.Max(minimumQuantity, maximumQuantity);
            int quantity = Random.Range(min, max + 1);

            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            Vector3 spawnPosition = transform.position +
                                    new Vector3(randomOffset.x, 0.5f, randomOffset.y);

            PickupItem pickup = Instantiate(
                pickupPrefab,
                spawnPosition,
                Quaternion.identity);

            pickup.Initialize(item, quantity);
        }
    }
}

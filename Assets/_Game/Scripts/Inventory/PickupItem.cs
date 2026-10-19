using UnityEngine;

namespace AbyssHunter.Inventory
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PickupItem : MonoBehaviour
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField, Min(0f)] private float rotationSpeed = 90f;
        [SerializeField, Min(0f)] private float bobHeight = 0.15f;
        [SerializeField, Min(0f)] private float bobSpeed = 3f;

        private Vector3 basePosition;
        private PlayerInventory nearbyInventory;
        public ItemData Item => item;

        private void Awake()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            basePosition = transform.position;
            nearbyInventory = FindFirstObjectByType<PlayerInventory>();
        }

        private void Update()
        {
            if (AbyssHunter.UI.GamePresentation.BlocksGameplay) return;
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            Vector3 position = basePosition;
            position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = position;
            if (item != null && item.ItemId == "abyss_shard" && nearbyInventory != null &&
                !nearbyInventory.GetComponent<AbyssHunter.Combat.Health>().IsDead &&
                Vector3.Distance(nearbyInventory.transform.position, transform.position) < 2.4f)
                Collect(nearbyInventory);
        }

        public void Initialize(ItemData itemData, int itemQuantity)
        {
            item = itemData;
            quantity = Mathf.Max(1, itemQuantity);
            basePosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            Collect(inventory);
        }

        private void Collect(PlayerInventory inventory)
        {
            if (AbyssHunter.UI.GamePresentation.BlocksGameplay || quantity <= 0 ||
                (inventory != null && inventory.GetComponent<AbyssHunter.Combat.Health>().IsDead)) return;
            if (inventory == null || item == null)
            {
                return;
            }

            int originalQuantity = quantity;
            inventory.TryAdd(item, quantity, out int remaining);
            quantity = remaining;

            if (remaining == 0)
            {
                Destroy(gameObject);
            }
            else if (remaining < originalQuantity)
            {
                Debug.Log($"背包已满，地面剩余 {item.DisplayName} × {remaining}", this);
            }
        }
    }
}

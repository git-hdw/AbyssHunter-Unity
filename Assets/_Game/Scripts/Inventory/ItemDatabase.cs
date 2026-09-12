using System.Collections.Generic;
using UnityEngine;

namespace AbyssHunter.Inventory
{
    [CreateAssetMenu(
        fileName = "ItemDatabase",
        menuName = "Abyss Hunter/Items/Item Database")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemData> items = new();

        private readonly Dictionary<string, ItemData> itemsById = new();

        private void OnEnable()
        {
            RebuildLookup();
        }

        public bool TryGetItem(string itemId, out ItemData item)
        {
            if (itemsById.Count == 0)
            {
                RebuildLookup();
            }

            return itemsById.TryGetValue(itemId, out item);
        }

        private void RebuildLookup()
        {
            itemsById.Clear();

            foreach (ItemData item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                if (!itemsById.TryAdd(item.ItemId, item))
                {
                    Debug.LogError($"物品 ID 重复：{item.ItemId}", this);
                }
            }
        }
    }
}

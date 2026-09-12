using System;
using System.Collections.Generic;
using AbyssHunter.Save;
using UnityEngine;

namespace AbyssHunter.Inventory
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 12;

        private InventorySlot[] slots;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public int Capacity => capacity;

        public event Action Changed;

        private void Awake()
        {
            slots = new InventorySlot[capacity];

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new InventorySlot();
            }
        }

        public bool TryAdd(ItemData item, int quantity, out int remaining)
        {
            remaining = quantity;

            if (item == null || quantity <= 0)
            {
                return false;
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].CanStack(item))
                {
                    remaining = slots[i].Add(item, remaining);
                }
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    remaining = slots[i].Add(item, remaining);
                }
            }

            int addedQuantity = quantity - remaining;
            if (addedQuantity > 0)
            {
                Changed?.Invoke();
                Debug.Log(
                    $"拾取 {item.DisplayName} × {addedQuantity}，" +
                    $"当前持有：{GetTotalQuantity(item)}，本次未装入：{remaining}",
                    this);
            }

            return remaining == 0;
        }

        public int GetTotalQuantity(ItemData item)
        {
            int total = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].Item == item)
                {
                    total += slots[i].Quantity;
                }
            }

            return total;
        }

        public bool TryUseSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return false;
            }

            InventorySlot slot = slots[slotIndex];
            if (slot.IsEmpty)
            {
                return false;
            }

            ItemData item = slot.Item;
            if (!item.TryUse(gameObject) || !slot.Remove(1))
            {
                return false;
            }

            Changed?.Invoke();
            Debug.Log($"使用 {item.DisplayName} × 1", this);
            return true;
        }

        public List<InventorySlotSaveData> CaptureSaveData()
        {
            List<InventorySlotSaveData> data = new(slots.Length);

            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];
                data.Add(new InventorySlotSaveData
                {
                    itemId = slot.IsEmpty ? string.Empty : slot.Item.ItemId,
                    quantity = slot.IsEmpty ? 0 : slot.Quantity
                });
            }

            return data;
        }

        public void RestoreSaveData(
            IReadOnlyList<InventorySlotSaveData> data,
            ItemDatabase itemDatabase)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Clear();
            }

            if (data == null || itemDatabase == null)
            {
                Changed?.Invoke();
                return;
            }

            int slotCount = Mathf.Min(slots.Length, data.Count);

            for (int i = 0; i < slotCount; i++)
            {
                InventorySlotSaveData slotData = data[i];
                if (slotData == null ||
                    string.IsNullOrWhiteSpace(slotData.itemId) ||
                    slotData.quantity <= 0)
                {
                    continue;
                }

                if (itemDatabase.TryGetItem(slotData.itemId, out ItemData item))
                {
                    slots[i].Add(item, slotData.quantity);
                }
                else
                {
                    Debug.LogWarning(
                        $"存档中的物品 ID 不存在：{slotData.itemId}",
                        this);
                }
            }

            Changed?.Invoke();
        }
    }
}

using System;
using UnityEngine;

namespace AbyssHunter.Inventory
{
    [Serializable]
    public sealed class InventorySlot
    {
        [field: SerializeField] public ItemData Item { get; private set; }
        [field: SerializeField, Min(0)] public int Quantity { get; private set; }

        public bool IsEmpty => Item == null || Quantity <= 0;
        public bool CanStack(ItemData item) =>
            !IsEmpty && Item.HasSameIdentity(item) && Quantity < Item.MaximumStack;

        public int Add(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return Mathf.Max(0, quantity);
            }

            if (IsEmpty)
            {
                Item = item;
                Quantity = 0;
            }

            if (!Item.HasSameIdentity(item))
            {
                return quantity;
            }

            int amountToAdd = Mathf.Min(quantity, Item.MaximumStack - Quantity);
            Quantity += amountToAdd;
            return quantity - amountToAdd;
        }

        public bool Remove(int quantity)
        {
            if (IsEmpty || quantity <= 0 || Quantity < quantity)
            {
                return false;
            }

            Quantity -= quantity;

            if (Quantity == 0)
            {
                Clear();
            }

            return true;
        }

        public void Clear()
        {
            Item = null;
            Quantity = 0;
        }
    }
}

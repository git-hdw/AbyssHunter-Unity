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
            !IsEmpty && Item == item && Quantity < Item.MaximumStack;

        public int Add(ItemData item, int quantity)
        {
            if (quantity <= 0)
            {
                return 0;
            }

            if (IsEmpty)
            {
                Item = item;
                Quantity = 0;
            }

            if (Item != item)
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

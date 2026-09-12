using System;
using System.Collections.Generic;

namespace AbyssHunter.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 1;
        public PlayerSaveData player = new();
        public List<InventorySlotSaveData> inventorySlots = new();
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public float positionX;
        public float positionY;
        public float positionZ;
        public int currentHealth;
    }

    [Serializable]
    public sealed class InventorySlotSaveData
    {
        public string itemId;
        public int quantity;
    }
}

using System.Collections.Generic;
using AbyssHunter.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.UI
{
    public sealed class InventoryUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private InventorySlotUI slotPrefab;

        private readonly List<InventorySlotUI> slotViews = new();

        public bool IsOpen =>
            inventoryPanel != null && inventoryPanel.activeSelf;

        private void Awake()
        {
            BuildSlots();

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.Changed += Refresh;
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.Changed -= Refresh;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.iKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (inventoryPanel == null)
            {
                return;
            }

            inventoryPanel.SetActive(!inventoryPanel.activeSelf);

            if (inventoryPanel.activeSelf)
            {
                Refresh();
            }
        }

        private void BuildSlots()
        {
            if (inventory == null || slotsParent == null || slotPrefab == null)
            {
                Debug.LogError("Inventory UI 的引用没有配置完整。", this);
                return;
            }

            for (int i = slotsParent.childCount - 1; i >= 0; i--)
            {
                Destroy(slotsParent.GetChild(i).gameObject);
            }

            for (int i = 0; i < inventory.Capacity; i++)
            {
                InventorySlotUI slotView = Instantiate(slotPrefab, slotsParent);
                slotView.Initialize(inventory, i);
                slotViews.Add(slotView);
            }
        }

        private void Refresh()
        {
            if (inventory == null)
            {
                return;
            }

            for (int i = 0; i < slotViews.Count; i++)
            {
                InventorySlot slot = i < inventory.Slots.Count
                    ? inventory.Slots[i]
                    : null;

                slotViews[i].Display(slot);
            }
        }
    }
}

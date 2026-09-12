using AbyssHunter.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbyssHunter.UI
{
    public sealed class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text quantityText;

        private PlayerInventory inventory;
        private int slotIndex = -1;

        public void Initialize(PlayerInventory playerInventory, int index)
        {
            inventory = playerInventory;
            slotIndex = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right &&
                inventory != null)
            {
                inventory.TryUseSlot(slotIndex);
            }
        }

        public void Display(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty)
            {
                Clear();
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = slot.Item.Icon;
                iconImage.enabled = slot.Item.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = slot.Item.DisplayName;
            }

            if (quantityText != null)
            {
                quantityText.text = $"×{slot.Quantity}";
            }
        }

        private void Clear()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (nameText != null)
            {
                nameText.text = string.Empty;
            }

            if (quantityText != null)
            {
                quantityText.text = string.Empty;
            }
        }
    }
}

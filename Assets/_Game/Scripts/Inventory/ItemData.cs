using UnityEngine;

namespace AbyssHunter.Inventory
{
    [CreateAssetMenu(
        fileName = "NewItem",
        menuName = "Abyss Hunter/Items/Item Data")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId = "item_id";
        [SerializeField] private string displayName = "新物品";
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int maximumStack = 99;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaximumStack => maximumStack;

        // Scene and Addressables bundles can deserialize separate instances of one item.
        public bool HasSameIdentity(ItemData other) => other != null &&
            (this == other || (!string.IsNullOrWhiteSpace(itemId) &&
                              string.Equals(itemId, other.itemId, System.StringComparison.Ordinal)));

        public virtual bool TryUse(GameObject user)
        {
            return false;
        }
    }
}

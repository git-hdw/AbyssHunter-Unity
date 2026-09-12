using AbyssHunter.Combat;
using UnityEngine;

namespace AbyssHunter.Inventory
{
    [CreateAssetMenu(
        fileName = "NewHealingItem",
        menuName = "Abyss Hunter/Items/Healing Item")]
    public sealed class HealingItemData : ItemData
    {
        [SerializeField, Min(1)] private int healingAmount = 30;

        public int HealingAmount => healingAmount;

        public override bool TryUse(GameObject user)
        {
            if (user == null || !user.TryGetComponent(out Health health))
            {
                return false;
            }

            return health.Heal(healingAmount);
        }
    }
}

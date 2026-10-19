using UnityEngine;

namespace AbyssHunter.Enemy
{
    [CreateAssetMenu(menuName = "Abyss Hunter/Heavy Attack Profile")]
    public sealed class HeavyAttackProfile : ScriptableObject
    {
        [Min(.1f)] public float windup = .8f;
        [Min(0)] public float trackingTime = .3f;
        [Min(.02f)] public float strikeDuration = .12f;
        [Min(.1f)] public float recovery = 1.1f;
        [Min(0)] public float recoveryStaggerBonus = .25f;
        [Min(.1f)] public float range = 3f;
        [Range(10,180)] public float angle = 110;
        [Min(1)] public int damage = 25;
        [Min(.1f)] public float triggerDistance = 2.5f;
        [Range(0,1)] public float animationContact = .35f;
        private void OnValidate()
        {
            trackingTime = Mathf.Clamp(trackingTime, 0, windup);
            triggerDistance = Mathf.Min(triggerDistance, range);
        }
    }
}

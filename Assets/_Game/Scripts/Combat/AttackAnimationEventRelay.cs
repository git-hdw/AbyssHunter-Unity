using AbyssHunter.Enemy;
using UnityEngine;

namespace AbyssHunter.Combat
{
    public sealed class AttackAnimationEventRelay : MonoBehaviour
    {
        private PlayerMeleeAttack playerAttack;
        private EnemyController enemyAttack;

        private void Awake()
        {
            playerAttack = GetComponentInParent<PlayerMeleeAttack>();
            enemyAttack = GetComponentInParent<EnemyController>();
        }

        public void ApplyAttackDamage()
        {
            if (playerAttack != null)
            {
                playerAttack.ApplyAttackDamage();
                return;
            }

            enemyAttack?.ApplyAttackDamage();
        }
    }
}

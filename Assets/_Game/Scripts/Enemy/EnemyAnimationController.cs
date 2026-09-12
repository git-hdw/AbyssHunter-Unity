using AbyssHunter.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace AbyssHunter.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyAnimationController : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField] private Animator animator;

        private NavMeshAgent agent;
        private EnemyController controller;
        private Health health;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            controller = GetComponent<EnemyController>();
            health = GetComponent<Health>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            controller.Attacked += PlayAttack;
            health.Damaged += PlayHit;
            health.Died += PlayDeath;

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void OnDisable()
        {
            controller.Attacked -= PlayAttack;
            health.Damaged -= PlayHit;
            health.Died -= PlayDeath;
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            animator.SetFloat(SpeedHash, velocity.magnitude);
        }

        private void PlayAttack()
        {
            animator?.SetTrigger(AttackHash);
        }

        private void PlayHit()
        {
            if (!health.IsDead)
            {
                animator?.SetTrigger(HitHash);
            }
        }

        private void PlayDeath()
        {
            animator?.SetTrigger(DieHash);
        }
    }
}

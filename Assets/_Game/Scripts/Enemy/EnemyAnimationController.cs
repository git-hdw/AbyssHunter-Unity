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
                animator.speed = 1;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void OnDisable()
        {
            if (animator != null) animator.speed = 1;
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

            if (!health.IsDead && controller.IsHeavyAttacking)
            {
                var profile = controller.HeavyAttack;
                float pose;
                if (controller.CurrentState == EnemyState.Windup)
                    pose = Mathf.Lerp(0, profile.animationContact, Mathf.Clamp01(controller.PhaseElapsed / profile.windup));
                else if (controller.CurrentState == EnemyState.Strike)
                    pose = Mathf.Lerp(profile.animationContact, .55f, Mathf.Clamp01(controller.PhaseElapsed / profile.strikeDuration));
                else
                    pose = Mathf.Lerp(.55f, .99f, Mathf.Clamp01(controller.PhaseElapsed / controller.RecoveryDuration));
                animator.speed = 0;
                animator.Play("HeavySwing", 0, pose);
                return;
            }
            if (animator.speed == 0)
            {
                animator.speed = 1;
                animator.Play(health.IsDead ? "Death_A" : "Idle_A", 0, 0);
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
            if (!health.IsDead && !controller.IsHeavyAttacking)
            {
                animator?.SetTrigger(HitHash);
            }
        }

        private void PlayDeath()
        {
            if (animator != null) animator.speed = 1;
            animator?.SetTrigger(DieHash);
        }
    }
}

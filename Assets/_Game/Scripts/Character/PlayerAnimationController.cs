using AbyssHunter.Combat;
using UnityEngine;

namespace AbyssHunter.Character
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerMeleeAttack))]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DodgeHash = Animator.StringToHash("Dodge");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField] private Animator animator;

        private CharacterController characterController;
        private PlayerMovement movement;
        private PlayerMeleeAttack meleeAttack;
        private Health health;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            meleeAttack = GetComponent<PlayerMeleeAttack>();
            health = GetComponent<Health>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            movement.DodgeStarted += PlayDodge;
            meleeAttack.Attacked += PlayAttack;
            health.Damaged += PlayHit;
            health.Died += PlayDeath;
        }

        private void OnDisable()
        {
            movement.DodgeStarted -= PlayDodge;
            meleeAttack.Attacked -= PlayAttack;
            health.Damaged -= PlayHit;
            health.Died -= PlayDeath;
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            animator.SetFloat(SpeedHash, velocity.magnitude);
        }

        private void PlayAttack()
        {
            animator?.SetTrigger(AttackHash);
        }

        private void PlayDodge()
        {
            animator?.SetTrigger(DodgeHash);
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

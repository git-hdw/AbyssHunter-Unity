using System;
using System.Collections;
using AbyssHunter.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace AbyssHunter.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float detectionRange = 8f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f;

        [Header("Attack")]
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1f;
        [SerializeField, Min(0f)] private float turnSpeed = 10f;
        [SerializeField] private HeavyAttackProfile heavyAttack;

        private NavMeshAgent agent;
        private IDamageable targetDamageable;
        private EnemyState currentState;
        private float nextAttackTime;
        private bool isAlerted;
        private bool attackDamagePending;
        private bool isKnockedBack;
        private Coroutine knockbackCoroutine;
        private Health health;
        private float phaseElapsed;
        private float recoveryBonus;
        private bool recoveryStaggerUsed;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[16];

        public EnemyState CurrentState => currentState;
        public event Action Attacked;
        public event Action<EnemyState> StateChanged;
        public event Action HeavyImpact;
        public HeavyAttackProfile HeavyAttack => heavyAttack;
        public bool IsHeavy => heavyAttack != null;
        public bool IsArmored => IsHeavy && (currentState == EnemyState.Windup || currentState == EnemyState.Strike);
        public bool IsHeavyAttacking => IsHeavy && (IsArmored || currentState == EnemyState.Recovery);
        public float PhaseElapsed => phaseElapsed;
        public float RecoveryDuration => IsHeavy ? heavyAttack.recovery + recoveryBonus : 0;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            nextAttackTime = 0f;
            isAlerted = false;
            phaseElapsed = 0;
            recoveryBonus = 0;
            recoveryStaggerUsed = false;
            attackDamagePending = false;
            targetDamageable = null;
            target = null;
            ChangeState(EnemyState.Idle);
        }

        private void OnDisable()
        {
            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
                knockbackCoroutine = null;
            }

            isKnockedBack = false;
            attackDamagePending = false;
            ChangeState(EnemyState.Idle);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private void Start()
        {
            CacheTargetDamageable();
        }

        public void SetTarget(Transform newTarget, bool alertImmediately = false)
        {
            target = newTarget;
            isAlerted = alertImmediately;
            CacheTargetDamageable();

            if (alertImmediately && target != null)
            {
                ChangeState(EnemyState.Chase);
            }
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;
            if (IsHeavyAttacking)
            {
                UpdateHeavyAttack();
                return;
            }
            if (target == null || isKnockedBack)
            {
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            switch (currentState)
            {
                case EnemyState.Idle:
                    UpdateIdle(distanceToTarget);
                    break;
                case EnemyState.Chase:
                    UpdateChase(distanceToTarget);
                    break;
                case EnemyState.Attack:
                    UpdateAttack(distanceToTarget);
                    break;
            }
        }

        private void UpdateIdle(float distanceToTarget)
        {
            if (distanceToTarget <= detectionRange)
            {
                ChangeState(EnemyState.Chase);
            }
        }

        private void UpdateChase(float distanceToTarget)
        {
            if (!isAlerted && distanceToTarget > detectionRange * 1.25f)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            if (distanceToTarget <= (IsHeavy ? heavyAttack.triggerDistance : attackRange))
            {
                if (IsHeavy)
                {
                    FaceTarget();
                    recoveryBonus = 0;
                    recoveryStaggerUsed = false;
                    ChangeState(EnemyState.Windup);
                }
                else ChangeState(EnemyState.Attack);
                return;
            }

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(target.position);
            }
        }

        private void UpdateAttack(float distanceToTarget)
        {
            if (distanceToTarget > attackRange)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            FaceTarget();

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                attackDamagePending = true;
                Attacked?.Invoke();
            }
        }

        public void ApplyAttackDamage()
        {
            // Heavy attacks are committed by their phase clock, never by legacy clip events.
            if (IsHeavy || !isActiveAndEnabled || (health != null && health.IsDead)) return;
            if (!attackDamagePending || target == null)
            {
                return;
            }

            attackDamagePending = false;

            if (currentState != EnemyState.Attack ||
                Vector3.Distance(transform.position, target.position) >
                attackRange * 1.25f)
            {
                return;
            }

            targetDamageable?.TakeDamage(damage);
            HitImpactVfx.Spawn(
                target.position + Vector3.up * 1.2f,
                new Color(1f, 0.32f, 0.22f, 1f));
        }

        public void ApplyKnockback(
            Vector3 direction,
            float distance,
            float duration)
        {
            TryApplyKnockback(direction, distance, duration);
        }

        public bool TryApplyKnockback(Vector3 direction, float distance, float duration)
        {
            if (!isActiveAndEnabled ||
                (health != null && health.IsDead) || IsArmored ||
                distance <= 0f ||
                duration <= 0f ||
                direction.sqrMagnitude < 0.001f)
            {
                return false;
            }

            if (IsHeavy && currentState == EnemyState.Recovery)
            {
                if (recoveryStaggerUsed) return false;
                recoveryStaggerUsed = true;
                recoveryBonus = heavyAttack.recoveryStaggerBonus;
                distance *= .65f;
            }

            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
            }

            knockbackCoroutine = StartCoroutine(
                KnockbackRoutine(direction.normalized, distance, duration));
            return true;
        }

        private IEnumerator KnockbackRoutine(
            Vector3 direction,
            float distance,
            float duration)
        {
            isKnockedBack = true;
            attackDamagePending = false;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            float elapsed = 0f;
            float previousProgress = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / duration);
                float progress = 1f - (1f - time) * (1f - time);
                float stepDistance = (progress - previousProgress) * distance;
                previousProgress = progress;

                if (agent.isOnNavMesh)
                {
                    agent.Move(direction * stepDistance);
                }

                yield return null;
            }

            isKnockedBack = false;
            knockbackCoroutine = null;
            SetAgentMotion();
        }

        private void ChangeState(EnemyState newState)
        {
            currentState = newState;
            phaseElapsed = 0;

            if (newState != EnemyState.Attack)
            {
                attackDamagePending = false;
            }

            SetAgentMotion();
            StateChanged?.Invoke(newState);
        }

        private void SetAgentMotion()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = currentState != EnemyState.Chase;

                if (agent.isStopped)
                {
                    agent.ResetPath();
                }
            }
        }

        private void UpdateHeavyAttack()
        {
            phaseElapsed += Time.deltaTime;
            if (currentState == EnemyState.Windup)
            {
                if (phaseElapsed < heavyAttack.trackingTime && target != null) FaceTarget();
                if (phaseElapsed >= heavyAttack.windup)
                {
                    ChangeState(EnemyState.Strike);
                    CommitHeavyStrike();
                    HeavyImpact?.Invoke();
                }
            }
            else if (currentState == EnemyState.Strike && phaseElapsed >= heavyAttack.strikeDuration)
                ChangeState(EnemyState.Recovery);
            else if (currentState == EnemyState.Recovery && phaseElapsed >= RecoveryDuration && !isKnockedBack)
                ChangeState(EnemyState.Chase);
        }

        public bool IsInsideHeavySector(Vector3 point)
        {
            if (!IsHeavy) return false;
            Vector3 offset = point - transform.position;
            if (Mathf.Abs(offset.y) > 2.5f) return false;
            offset.y = 0;
            return offset.sqrMagnitude <= heavyAttack.range * heavyAttack.range &&
                (offset.sqrMagnitude < .001f || Vector3.Angle(transform.forward, offset) <= heavyAttack.angle * .5f);
        }

        private void CommitHeavyStrike()
        {
            if (target == null || !IsInsideHeavySector(target.position)) return;
            Vector3 origin = transform.position + Vector3.up * .5f;
            Vector3 offset = target.position + Vector3.up * .5f - origin;
            int count = Physics.RaycastNonAlloc(origin, offset.normalized, obstructionHits,
                offset.magnitude, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = obstructionHits[i].collider;
                if (hit.transform.root != transform.root && hit.transform.root != target.root &&
                    hit.GetComponentInParent<Health>() == null) return;
            }
            targetDamageable?.TakeDamage(heavyAttack.damage);
        }

        private void FaceTarget()
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        private void CacheTargetDamageable()
        {
            if (target != null)
            {
                targetDamageable = target.GetComponent<IDamageable>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}

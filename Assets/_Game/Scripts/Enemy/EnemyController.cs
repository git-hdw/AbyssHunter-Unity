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

        private NavMeshAgent agent;
        private IDamageable targetDamageable;
        private EnemyState currentState;
        private float nextAttackTime;
        private bool isAlerted;
        private bool attackDamagePending;
        private bool isKnockedBack;
        private Coroutine knockbackCoroutine;

        public EnemyState CurrentState => currentState;
        public event Action Attacked;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            nextAttackTime = 0f;
            isAlerted = false;
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

            if (distanceToTarget <= attackRange)
            {
                ChangeState(EnemyState.Attack);
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
            if (!isActiveAndEnabled ||
                distance <= 0f ||
                duration <= 0f ||
                direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
            }

            knockbackCoroutine = StartCoroutine(
                KnockbackRoutine(direction.normalized, distance, duration));
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
            ChangeState(currentState);
        }

        private void ChangeState(EnemyState newState)
        {
            currentState = newState;

            if (newState != EnemyState.Attack)
            {
                attackDamagePending = false;
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = newState != EnemyState.Chase;

                if (agent.isStopped)
                {
                    agent.ResetPath();
                }
            }
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

using System;
using System.Collections.Generic;
using AbyssHunter.Enemy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.Combat
{
    public sealed class PlayerMeleeAttack : MonoBehaviour
    {
        private const int HitBufferSize = 16;

        [SerializeField] private Transform attackPoint;
        [SerializeField, Min(0.1f)] private float attackRadius = 1.5f;
        [SerializeField, Min(0.1f)] private float autoAimRange = 3f;
        [SerializeField, Min(1)] private int damage = 25;
        [SerializeField, Min(0.01f)] private float attackCooldown = 0.5f;
        [SerializeField, Min(0f)] private float inputBufferTime = 0.35f;
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header("Knockback")]
        [SerializeField, Min(0f)] private float knockbackDistance = 1.2f;
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.15f;

        private readonly Collider[] hitBuffer = new Collider[HitBufferSize];
        private readonly HashSet<IDamageable> damagedTargets = new();
        private Camera mainCamera;
        private float nextAttackTime;
        private float queuedAttackUntil = float.NegativeInfinity;
        private bool attackDamagePending;

        public event Action Attacked;
        public event Action<bool> HitConfirmed;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                queuedAttackUntil = Time.time + inputBufferTime;
            }

            if (Time.time <= queuedAttackUntil && Time.time >= nextAttackTime)
            {
                queuedAttackUntil = float.NegativeInfinity;
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            nextAttackTime = Time.time + attackCooldown;

            if (!FaceNearestTarget())
            {
                FaceMousePointer();
            }

            attackDamagePending = true;
            Attacked?.Invoke();
        }

        public void ApplyAttackDamage()
        {
            if (!attackDamagePending)
            {
                return;
            }

            attackDamagePending = false;
            Vector3 attackPosition = GetAttackPosition();
            damagedTargets.Clear();
            bool hitAnyTarget = false;
            bool killedAnyTarget = false;

            int hitCount = Physics.OverlapSphereNonAlloc(
                attackPosition,
                attackRadius,
                hitBuffer,
                targetLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hitBuffer[i];
                if (hit.transform.root == transform.root)
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damagedTargets.Add(damageable))
                {
                    Health targetHealth = hit.GetComponentInParent<Health>();
                    int previousHealth = targetHealth != null
                        ? targetHealth.CurrentHealth
                        : 1;

                    damageable.TakeDamage(damage);

                    bool dealtDamage = targetHealth == null ||
                        targetHealth.CurrentHealth < previousHealth;
                    if (dealtDamage)
                    {
                        hitAnyTarget = true;
                        killedAnyTarget |= targetHealth != null &&
                            targetHealth.IsDead;
                        TryKnockbackEnemy(hit, damageable);
                        HitImpactVfx.Spawn(
                            hit.ClosestPoint(attackPosition),
                            new Color(1f, 0.82f, 0.28f, 1f));
                    }
                }
            }

            if (hitAnyTarget)
            {
                HitConfirmed?.Invoke(killedAnyTarget);
            }

            Debug.DrawLine(transform.position, attackPosition, Color.red, attackCooldown);
        }

        private void TryKnockbackEnemy(Collider hit, IDamageable damageable)
        {
            if (knockbackDistance <= 0f ||
                damageable is Health health && health.IsDead)
            {
                return;
            }

            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy == null)
            {
                return;
            }

            Vector3 direction = enemy.transform.position - transform.position;
            direction.y = 0f;
            enemy.ApplyKnockback(direction, knockbackDistance, knockbackDuration);
        }

        private bool FaceNearestTarget()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                autoAimRange,
                hitBuffer,
                targetLayers,
                QueryTriggerInteraction.Ignore);

            Collider nearestTarget = null;
            float nearestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hitBuffer[i];
                if (hit.transform.root == transform.root ||
                    hit.GetComponentInParent<IDamageable>() == null)
                {
                    continue;
                }

                Vector3 offset = hit.bounds.center - transform.position;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;

                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestTarget = hit;
                }
            }

            if (nearestTarget == null)
            {
                return false;
            }

            Vector3 direction = nearestTarget.bounds.center - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                return false;
            }

            transform.rotation = Quaternion.LookRotation(direction);
            return true;
        }

        private void FaceMousePointer()
        {
            Mouse mouse = Mouse.current;
            if (mainCamera == null || mouse == null)
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            Plane groundPlane = new(Vector3.up, transform.position);

            if (!groundPlane.Raycast(ray, out float enter))
            {
                return;
            }

            Vector3 direction = ray.GetPoint(enter) - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private Vector3 GetAttackPosition()
        {
            if (attackPoint != null)
            {
                return attackPoint.position;
            }

            return transform.position + Vector3.up + transform.forward * 1.2f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetAttackPosition(), attackRadius);
        }
    }
}

using System;
using System.Collections.Generic;
using AbyssHunter.Enemy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.Combat
{
    public sealed partial class PlayerMeleeAttack : MonoBehaviour
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
        private Collider clickedCollider;
        private bool attackDamagePending;
        private AbyssHunter.Character.PlayerMovement movement;
        private Health health;

        private HunterWeapons weapons;
        public bool HoldsAttackFacing => (attackDamagePending || releaseAfterPose) && (UsesAxe || UsesCrossbow);
        public bool UsesAxe => weapons != null && weapons.UsesAxe;
        [SerializeField, Min(1)] private int axeDamage = 42;
        [SerializeField, Min(.1f)] private float axeCooldown = .92f;
        [SerializeField, Min(.1f)] private float axeRadius = 1.75f;
        [SerializeField, Min(0)] private float axeKnockback = 1.8f;

        public event Action Attacked;
        public event Action<bool> HitConfirmed;
        public bool LastHitStaggeredHeavy { get; private set; }

        private void Awake()
        {
            weapons = GetComponentInChildren<HunterWeapons>();
            mainCamera = Camera.main;
            movement = GetComponent<AbyssHunter.Character.PlayerMovement>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (movement != null) movement.DodgeStarted += CancelAttack;
            if (health != null) health.Died += CancelAttack;
        }

        private void OnDisable()
        {
            if (movement != null) movement.DodgeStarted -= CancelAttack;
            if (health != null) health.Died -= CancelAttack;
            StopBolt();
            CancelAttack();
        }

        private void CancelAttack()
        {
            releaseAfterPose = false;
            attackDamagePending = false;
            queuedAttackUntil = float.NegativeInfinity;
            clickedCollider = null;
        }

        private void Update()
        {
            if (AbyssHunter.UI.GamePresentation.BlocksGameplay || AbyssHunter.UI.MatchResult.HasEnded)
            {
                queuedAttackUntil = float.NegativeInfinity;
                clickedCollider = null;
                return;
            }
            AdvanceBolt();
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                queuedAttackUntil = Time.time + inputBufferTime;
                // The camera can move during a buffered dodge. Pick on press, then validate on swing.
                clickedCollider = mainCamera != null && Physics.Raycast(
                    mainCamera.ScreenPointToRay(mouse.position.ReadValue()), out var pick,
                    mainCamera.farClipPlane, mainCamera.cullingMask & Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) ? pick.collider : null;
            }

            if (movement != null && movement.IsDodging) return;
            if (Time.time <= queuedAttackUntil && Time.time >= nextAttackTime)
            {
                queuedAttackUntil = float.NegativeInfinity;
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            nextAttackTime = Time.time + (UsesCrossbow ? shotCooldown : UsesAxe ? axeCooldown : attackCooldown);

            if (UsesCrossbow) AimShot();
            else if (!FaceAttackTarget())
            {
                FaceMousePointer();
            }
            clickedCollider = null;

            attackDamagePending = true;
            Attacked?.Invoke();
        }

        public void ApplyAttackDamage()
        {
            if (!attackDamagePending || (movement != null && movement.IsDodging) ||
                (health != null && health.IsDead) || AbyssHunter.UI.MatchResult.HasEnded)
            {
                return;
            }

            attackDamagePending = false;
            if (UsesCrossbow) { releaseAfterPose = true; return; }
            Vector3 attackPosition = GetAttackPosition();
            damagedTargets.Clear();
            bool hitAnyTarget = false;
            bool killedAnyTarget = false;
            bool staggeredHeavy = false;

            int hitCount = Physics.OverlapSphereNonAlloc(
                attackPosition,
                UsesAxe ? axeRadius : attackRadius,
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

                    damageable.TakeDamage(UsesAxe ? axeDamage : damage);

                    bool dealtDamage = targetHealth == null ||
                        targetHealth.CurrentHealth < previousHealth;
                    if (dealtDamage)
                    {
                        hitAnyTarget = true;
                        killedAnyTarget |= targetHealth != null &&
                            targetHealth.IsDead;
                        bool counter = KnockbackStaggeredHeavy(hit, damageable);
                        staggeredHeavy |= counter;
                        // Sample the guard's sword-facing surface; the attack sphere centre can be inside its body.
                        Vector3 impactPosition = counter
                            ? hit.ClosestPoint(transform.position + Vector3.up * .5f)
                            : hit.ClosestPoint(attackPosition);
                        HitImpactVfx.Spawn(impactPosition,
                            counter ? new Color(.35f, 1f, .85f) : new Color(1f, .82f, .28f));
                    }
                }
            }

            if (hitAnyTarget)
            {
                LastHitStaggeredHeavy = staggeredHeavy;
                HitConfirmed?.Invoke(killedAnyTarget);
            }

            Debug.DrawLine(transform.position, attackPosition, Color.red, attackCooldown);
        }

        private bool KnockbackStaggeredHeavy(Collider hit, IDamageable damageable)
        {
            float distance = UsesAxe ? axeKnockback : knockbackDistance;
            if (distance <= 0f ||
                damageable is Health health && health.IsDead)
            {
                return false;
            }

            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy == null)
            {
                return false;
            }

            Vector3 direction = enemy.transform.position - transform.position;
            direction.y = 0f;
            return enemy.TryApplyKnockback(direction, distance, knockbackDuration)
                && enemy.IsRecoveryStaggered;
        }

        private bool FaceAttackTarget()
        {
            // A full crowd query must not drop the enemy explicitly picked on press.
            if (clickedCollider != null && clickedCollider.enabled && clickedCollider.gameObject.activeInHierarchy &&
                (targetLayers.value & (1 << clickedCollider.gameObject.layer)) != 0 &&
                IsLiveTarget(clickedCollider) &&
                (clickedCollider.ClosestPoint(transform.position) - transform.position).sqrMagnitude <= autoAimRange * autoAimRange &&
                FaceTarget(clickedCollider))
                return true;

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
                if (!IsLiveTarget(hit)) continue;

                Vector3 offset = hit.bounds.center - transform.position;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;

                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestTarget = hit;
                }
            }

            return nearestTarget != null && FaceTarget(nearestTarget);
        }

        private bool IsLiveTarget(Collider hit) =>
            hit.transform.root != transform.root && hit.GetComponentInParent<IDamageable>() != null &&
            !(hit.GetComponentInParent<Health>() is Health candidate && candidate.IsDead);

        private bool FaceTarget(Collider target)
        {
            Vector3 direction = target.bounds.center - transform.position;
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

using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.Combat
{
    public sealed partial class PlayerMeleeAttack
    {
        [Header("Crossbow")]
        [SerializeField, Min(1)] private int shotDamage = 20;
        [SerializeField, Min(.1f)] private float shotCooldown = .85f;
        [SerializeField, Min(1)] private float shotRange = 12f;
        [SerializeField, Min(1)] private float boltSpeed = 19f;
        private const float BoltRadius = .09f;
        private readonly RaycastHit[] boltHits = new RaycastHit[32];
        private Vector3 aimPoint, boltPosition, boltDirection;
        private float distanceLeft;
        private bool releaseAfterPose;

        private void LateUpdate()
        {
            if (!releaseAfterPose || AbyssHunter.UI.GamePresentation.BlocksGameplay) return;
            releaseAfterPose = false;
            if (UsesCrossbow && !AbyssHunter.UI.MatchResult.HasEnded &&
                (health == null || !health.IsDead) && (movement == null || !movement.IsDodging))
            {
                ReleaseBolt();
                BoltReleased?.Invoke();
            }
        }
        public bool UsesCrossbow => weapons != null && weapons.UsesCrossbow;
        public event System.Action BoltReleased;
        public bool BoltInFlight => distanceLeft > 0;

        private void AimShot()
        {
            aimPoint = transform.position + transform.forward * shotRange;
            if (clickedCollider != null && IsLiveTarget(clickedCollider))
                aimPoint = clickedCollider.bounds.center;
            else if (mainCamera != null && Mouse.current != null)
            {
                var ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                var plane = new Plane(Vector3.up, transform.position);
                if (plane.Raycast(ray, out float enter)) aimPoint = ray.GetPoint(enter);
            }
            var facing = Vector3.ProjectOnPlane(aimPoint - transform.position, Vector3.up);
            if (facing.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(facing);
        }

        private void ReleaseBolt()
        {
            Vector3 muzzle = weapons.Muzzle;
            Vector3 inside = transform.position;
            inside.y = muzzle.y;
            Vector3 toMuzzle = muzzle - inside;
            int count = Physics.OverlapSphereNonAlloc(inside, BoltRadius, hitBuffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Collider contact = null;
            for (int i = 0; i < count; i++)
            {
                var candidate = hitBuffer[i];
                if (candidate.transform.IsChildOf(transform)) continue;
                contact = candidate;
                if (candidate.GetComponentInParent<Health>() == null) break;
            }
            if (contact) { ImpactBolt(contact, inside); return; }
            if (FirstBoltHit(inside, toMuzzle.normalized, toMuzzle.magnitude, out var obstruction))
            {
                ImpactBolt(obstruction.collider, obstruction.point);
                return;
            }
            boltPosition = muzzle;
            var delta = aimPoint - muzzle;
            boltDirection = Vector3.Dot(delta, weapons.BarrelDirection) > 0 ? delta.normalized : weapons.BarrelDirection;
            distanceLeft = shotRange;
            var bolt = weapons.Bolt;
            bolt.transform.SetPositionAndRotation(muzzle, Quaternion.LookRotation(boltDirection));
            bolt.SetActive(true);
            if (weapons.BoltTrail) weapons.BoltTrail.Clear();
        }

        private void AdvanceBolt()
        {
            if (!BoltInFlight || Time.deltaTime <= 0) return;
            float step = Mathf.Min(distanceLeft, boltSpeed * Time.deltaTime);
            if (FirstBoltHit(boltPosition, boltDirection, step, out var hit))
            {
                ImpactBolt(hit.collider, hit.point);
                return;
            }
            boltPosition += boltDirection * step;
            weapons.Bolt.transform.SetPositionAndRotation(boltPosition, Quaternion.LookRotation(boltDirection));
            distanceLeft -= step;
            if (!BoltInFlight) StopBolt();
        }

        private bool FirstBoltHit(Vector3 origin, Vector3 direction, float length, out RaycastHit hit)
        {
            int count = Physics.SphereCastNonAlloc(origin, BoltRadius, direction, boltHits, length,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                if (boltHits[i].transform.IsChildOf(transform)) continue;
                if (nearest < 0 || boltHits[i].distance < boltHits[nearest].distance) nearest = i;
            }
            hit = nearest >= 0 ? boltHits[nearest] : default;
            return nearest >= 0;
        }

        private void ImpactBolt(Collider collider, Vector3 point)
        {
            StopBolt();
            var target = collider.GetComponentInParent<Health>();
            if (target && !target.IsDead && !target.IsInvulnerable && (targetLayers.value & (1 << collider.gameObject.layer)) != 0)
            {
                target.TakeDamage(shotDamage);
                LastHitStaggeredHeavy = false;
                HitConfirmed?.Invoke(target.IsDead);
            }
            HitImpactVfx.Spawn(point, new Color(.7f, .82f, .9f));
        }

        private void StopBolt()
        {
            distanceLeft = 0;
            if (weapons && weapons.Bolt) weapons.Bolt.SetActive(false);
        }
    }
}

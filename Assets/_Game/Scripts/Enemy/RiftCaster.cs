using AbyssHunter.Combat;
using UnityEngine;

namespace AbyssHunter.Enemy
{
    public sealed class RiftCaster : MonoBehaviour
    {
        [SerializeField] private Transform focus, bolt, staff, impact;
        [SerializeField] private LineRenderer warning;
        [SerializeField] private float windup = .9f, cooldown = 2.5f, speed = 8f, reach = 12f;
        [SerializeField] private int damage = 12;
        private const float Radius = .15f;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Collider[] overlaps = new Collider[16];
        private Vector3 direction, position, impactPoint;
        private Transform cameraTransform;
        private Animator mage;
        private Renderer impactRenderer;
        private CinderEmbers embers;
        private MaterialPropertyBlock impactProperties;
        private static readonly int ReleaseFire = Animator.StringToHash("Base Layer.ReleaseFire");
        private static readonly int Age = Shader.PropertyToID("_Age");
        private const float BurstDuration = .32f;
        private float impactTime;
        private Transform victim;
        private Health victimHealth;
        private float releaseAt, readyAt, remaining;
        public bool IsCharging { get; private set; }
        public bool InFlight => remaining > 0;
        private Vector3 Origin => transform.position + Vector3.up * .15f;

        private void Awake()
        {
            mage = staff.GetComponentInParent<Animator>();
            impactProperties = new MaterialPropertyBlock();
            impactProperties.SetFloat("_Effect", 2);
            warning.SetPropertyBlock(impactProperties);
            impactProperties.SetFloat("_Effect", 1);
            impactRenderer = impact.GetComponent<Renderer>();
            impactRenderer.SetPropertyBlock(impactProperties);
            var emitter = new GameObject("飞火与余烬");
            emitter.transform.SetParent(transform, false);
            embers = emitter.AddComponent<CinderEmbers>();
            embers.Initialize(impactRenderer.sharedMaterial);
        }
        private void OnEnable() { ResetCast(); readyAt = Time.time + .5f; }
        private void OnDisable() => ResetCast();

        public bool CanSee(Transform target)
        {
            var delta = target.position - Origin;
            return !FirstHit(Origin, delta.normalized, delta.magnitude, out var hit) || hit.transform.IsChildOf(target);
        }

        public bool TryBegin(Transform target)
        {
            if (IsCharging || InFlight || Time.time < readyAt) return false;
            victim = target;
            victimHealth = target.GetComponent<Health>();
            direction = (target.position - Origin).normalized;
            releaseAt = Time.time + windup;
            readyAt = releaseAt + cooldown;
            IsCharging = true;
            warning.SetPosition(0, Origin);
            warning.SetPosition(1, Origin);
            warning.enabled = true;
            focus.gameObject.SetActive(true);
            return true;
        }

        public void Interrupt()
        {
            IsCharging = false;
            warning.enabled = false;
            focus.gameObject.SetActive(false);
        }

        public void ResetCast()
        {
            Interrupt();
            remaining = 0;
            bolt.gameObject.SetActive(false);
            impact.gameObject.SetActive(false);
            if (embers != null) embers.Clear();
            victim = null; victimHealth = null;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0) return;
            if (IsCharging)
            {
                float progress = Mathf.Clamp01(1 - (releaseAt - Time.time) / windup);
                warning.startWidth = warning.endWidth = Mathf.Lerp(.09f, .17f, progress);
                warning.SetPosition(0, Origin);
                warning.SetPosition(1, FirstHit(Origin, direction, reach, out var stop) ? stop.point : Origin + direction * reach);
                if (Time.time < releaseAt) return;
                Interrupt();
                position = Origin;
                remaining = reach;
                bolt.gameObject.SetActive(true);
                mage.CrossFadeInFixedTime(ReleaseFire, .075f);
                int count = Physics.OverlapSphereNonAlloc(position, Radius, overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                bool touchesVictim = false;
                for (int i = 0; i < count; i++)
                {
                    var t = overlaps[i].transform;
                    if (t.IsChildOf(transform)) continue;
                    if (victim != null && t.IsChildOf(victim)) touchesVictim = true;
                    else { FinishImpact(); return; }
                }
                if (touchesVictim) { HitVictim(); FinishImpact(); return; }
            }
            if (!InFlight) return;
            float step = Mathf.Min(speed * Time.deltaTime, remaining);
            if (FirstHit(position, direction, step, out var hit))
            {
                embers.Trail(position, direction, hit.distance);
                position = hit.point;
                if (victim != null && hit.transform.IsChildOf(victim)) HitVictim();
                FinishImpact();
            }
            else
            {
                embers.Trail(position, direction, step);
                position += direction * step;
                remaining -= step;
            }
            if (!InFlight) bolt.gameObject.SetActive(false);
        }

        private void HitVictim()
        {
            if (victimHealth == null || victimHealth.IsInvulnerable || victimHealth.IsDead) return;
            victimHealth.TakeDamage(damage);
        }

        private void FinishImpact()
        {
            remaining = 0;
            bolt.gameObject.SetActive(false);
            impactPoint = position;
            impactTime = Time.time;
            impact.gameObject.SetActive(true);
            embers.Burst(position);
        }

        private void LateUpdate()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (cameraTransform == null) return;
            if (IsCharging)
            {
                float charge = Mathf.Clamp01(1 - (releaseAt - Time.time) / windup);
                Vector3 tip = staff.position + staff.up * 1.06f;
                Vector3 launch = Vector3.Lerp(tip, Origin, Mathf.SmoothStep(0, 1, (charge - .78f) / .22f));
                float size = Mathf.Lerp(.42f, 1.25f, charge);
                ShowFlame(focus, launch, Vector3.up, size, size * 1.4f);
                embers.Illuminate(launch, 1 + 2.6f * charge);
            }
            else if (InFlight)
            {
                ShowFlame(bolt, position, -direction, 1.18f, 2.15f);
                embers.Illuminate(position, 3.2f);
            }
            else if (!impact.gameObject.activeSelf) embers.Illuminate(transform.position, 0);
            if (!impact.gameObject.activeSelf) return;
            float age = Mathf.Clamp01((Time.time - impactTime) / BurstDuration);
            impact.SetPositionAndRotation(impactPoint - cameraTransform.forward * .55f, cameraTransform.rotation);
            impact.localScale = Vector3.one * 2.8f;
            embers.Illuminate(impactPoint + Vector3.up * .25f, 5 * (1 - age));
            impactProperties.SetFloat(Age, age);
            impactRenderer.SetPropertyBlock(impactProperties);
            if (age >= 1) impact.gameObject.SetActive(false);
        }

        private void ShowFlame(Transform sheet, Vector3 core, Vector3 tail, float width, float length)
        {
            Vector3 up = Vector3.ProjectOnPlane(tail, cameraTransform.forward).normalized;
            if (up.sqrMagnitude < .01f) up = cameraTransform.up;
            sheet.SetPositionAndRotation(core + up * (length * .17f), Quaternion.LookRotation(cameraTransform.forward, up));
            sheet.localScale = new Vector3(width, length, 1);
        }

        private bool FirstHit(Vector3 start, Vector3 aim, float distance, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.SphereCastNonAlloc(start, Radius, aim, hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform)) continue;
                if (nearest.collider == null || hit.distance < nearest.distance) nearest = hit;
            }
            return nearest.collider != null;
        }
    }
}

using AbyssHunter.Combat;
using AbyssHunter.UI;
using UnityEngine;

namespace AbyssHunter.World
{
    // A carried flame reacts to the hunter's state without introducing a fuel timer.
    public sealed class LastLight : MonoBehaviour
    {
        [SerializeField] private Transform flame;
        [SerializeField] private Light glow;
        [SerializeField] private ParticleSystem sparks;
        private Health health;
        private AbyssExpedition mission;
        private Vector3 flameScale, previousPosition;
        private Quaternion restRotation;
        private Renderer flameRenderer;
        private MaterialPropertyBlock properties;
        private float strength, hurt, clock;
        private bool extinguished;
        public float Strength => strength;
        public Light Glow => glow;

        private void Awake()
        {
            health = GetComponentInParent<Health>();
            mission = FindFirstObjectByType<AbyssExpedition>();
            flameScale = flame.localScale;
            restRotation = transform.localRotation;
            previousPosition = transform.position;
            flameRenderer = flame.GetComponent<Renderer>();
            properties = new MaterialPropertyBlock();
            if (health != null) health.Damaged += OnDamaged;
        }
        private void OnDamaged() => hurt = .72f;
        private void LateUpdate()
        {
            var state = GamePresentation.Current;
            bool paused = state != null && (state.State == GamePresentation.ScreenState.Paused || state.State == GamePresentation.ScreenState.Inventory);
            float dt = paused ? 0 : GamePresentation.AnimationDelta;
            clock += dt;
            hurt = Mathf.MoveTowards(hurt, 0, dt * 1.5f);
            bool dead = health != null && health.IsDead;
            if (dead && !extinguished)
            {
                extinguished = true;
                sparks.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                sparks.Emit(7);
            }
            float target = dead ? 0 : (mission != null && mission.HasCore ? 1.12f : .9f) * (1 - hurt);
            strength = Mathf.MoveTowards(strength, target, dt * (dead ? 2.2f : 1.8f));
            float breathing = 1 + Mathf.Sin(clock * 2.4f) * .045f + Mathf.Sin(clock * 11.3f) * .025f;
            flame.localScale = Vector3.Scale(flameScale, new Vector3(1, breathing, 1)) * Mathf.Max(.001f, strength);
            glow.intensity = strength * breathing * 1.5f;
            glow.range = Mathf.Lerp(2.6f, 3.8f, strength);
            properties.SetColor("_EmissionColor", new Color(2.4f, .95f, .12f) * strength * breathing);
            flameRenderer.SetPropertyBlock(properties);
            float movement = dt > 0 ? Mathf.Min(2, Vector3.Distance(transform.position, previousPosition) / dt) : 0;
            previousPosition = transform.position;
            if (!paused) transform.localRotation = restRotation * Quaternion.Euler(Mathf.Sin(clock * 4) * movement * 3, 0, Mathf.Sin(clock * 3.2f) * (2 + movement * 3));
        }
        private void OnDestroy() { if (health != null) health.Damaged -= OnDamaged; }
    }
}

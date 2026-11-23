using UnityEngine;

namespace AbyssHunter.Enemy
{
    // One emitter per pooled caster. World-space cinders linger at the places the fireball passed.
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class CinderEmbers : MonoBehaviour
    {
        private ParticleSystem sparks;
        private Light glow;
        private float phase, trailDistance;

        public void Initialize(Material fire)
        {
            sparks = GetComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 44;
            var emission = sparks.emission;
            emission.enabled = false;
            var shape = sparks.shape;
            shape.enabled = false;
            var color = sparks.colorOverLifetime;
            color.enabled = true;
            var burn = new Gradient();
            burn.SetKeys(new[] { new GradientColorKey(new Color(1, .92f, .34f), 0), new GradientColorKey(new Color(1, .28f, .025f), .64f) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.9f, .5f), new GradientAlphaKey(0, 1) });
            color.color = burn;
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = fire;
            var properties = new MaterialPropertyBlock();
            properties.SetFloat("_Effect", 3);
            renderer.SetPropertyBlock(properties);
            glow = gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1, .32f, .06f);
            glow.range = 3.4f;
            glow.shadows = LightShadows.None;
            glow.enabled = false;
        }

        public void Illuminate(Vector3 point, float strength)
        {
            transform.position = point;
            glow.intensity = strength;
            glow.enabled = strength > 0;
        }

        public void Clear()
        {
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            glow.enabled = false;
            trailDistance = 0;
        }

        public void Trail(Vector3 start, Vector3 direction, float length)
        {
            const float spacing = .18f;
            float step = spacing - trailDistance;
            while (step <= length)
            {
                Vector3 drift = NextDirection();
                Emit(start + direction * step + drift * .14f, drift * .82f + Vector3.up * .45f, .34f, .17f);
                step += spacing;
            }
            trailDistance = length - (step - spacing);
        }

        public void Burst(Vector3 point)
        {
            for (int i = 0; i < 18; i++)
            {
                Vector3 outward = NextDirection();
                Emit(point + outward * .23f, outward * (2.8f + i % 4 * .54f), .28f + i % 3 * .07f, .21f + i % 3 * .04f);
            }
            trailDistance = 0;
        }

        private Vector3 NextDirection()
        {
            phase = Mathf.Repeat(phase + 2.39996f, 2 * Mathf.PI);
            return new Vector3(Mathf.Cos(phase), .35f + .55f * Mathf.Sin(phase * 3), Mathf.Sin(phase));
        }

        private void Emit(Vector3 point, Vector3 velocity, float life, float size)
        {
            if (!sparks.isPlaying) sparks.Play();
            sparks.Emit(new ParticleSystem.EmitParams { position = point, velocity = velocity, startLifetime = life,
                startSize = size, startColor = Color.white, rotation = phase * Mathf.Rad2Deg }, 1);
        }
    }
}

using UnityEngine;

namespace AbyssHunter.Combat
{
    public static class HitImpactVfx
    {
        public static void Spawn(Vector3 position, Color color)
        {
            GameObject instance = new("HitImpact");
            instance.transform.position = position;

            ParticleSystem particles = instance.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ConfigureRenderer(particles, color);
            ConfigureMain(particles, color);
            ConfigureEmission(particles);
            ConfigureShape(particles);
            ConfigureSize(particles);
            ConfigureColorOverLifetime(particles, color);

            particles.Play(true);
            Object.Destroy(instance, 0.8f);
        }

        private static void ConfigureRenderer(ParticleSystem particles, Color color)
        {
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial(color);
        }

        private static void ConfigureMain(ParticleSystem particles, Color color)
        {
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.duration = 0.2f;
            main.startLifetime = 0.28f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = color;
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
        }

        private static void ConfigureEmission(ParticleSystem particles)
        {
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 14, 18)
            });
        }

        private static void ConfigureShape(ParticleSystem particles)
        {
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
        }

        private static void ConfigureSize(ParticleSystem particles)
        {
            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        private static void ConfigureColorOverLifetime(ParticleSystem particles, Color color)
        {
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;
        }

        private static Material CreateParticleMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new(shader)
            {
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }
    }
}

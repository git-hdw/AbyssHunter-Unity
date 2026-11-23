using UnityEngine;

namespace AbyssHunter.Combat
{
    public static class CombatSfx
    {
        private static AudioClip swingClip;
        private static AudioClip bowClip;
        public static void PlayBowstring(AudioSource source) => Play(source, ref bowClip, CreateBowstring);

        private static AudioClip CreateBowstring() => CreateClip("Sfx_Bowstring", .17f, (time, duration) =>
        {
            float twang = Mathf.Sin(2 * Mathf.PI * time * (560 - 1300 * time));
            return .56f * twang * Mathf.Exp(-28 * time) + .25f * Mathf.Sin(8900 * time) * Mathf.Exp(-135 * time);
        });
        private static AudioClip hitClip;
        private static AudioClip counterClip;
        private static AudioClip killClip;
        private static AudioClip hurtClip;

        public static void PlaySwing(AudioSource source)
        {
            Play(source, ref swingClip, CreateSwing);
        }

        public static void PlayHit(AudioSource source)
        {
            Play(source, ref hitClip, CreateHit);
        }

        public static void PlayKill(AudioSource source)
        {
            Play(source, ref killClip, CreateKill);
        }

        public static void PlayCounter(AudioSource source)
        {
            Play(source, ref counterClip, CreateCounter);
        }

        public static void PlayHurt(AudioSource source)
        {
            Play(source, ref hurtClip, CreateHurt);
        }

        private static void Play(
            AudioSource source,
            ref AudioClip clip,
            System.Func<AudioClip> factory)
        {
            if (source == null)
            {
                return;
            }

            if (clip == null)
            {
                clip = factory();
            }

            source.PlayOneShot(clip);
        }

        private static AudioClip CreateSwing()
        {
            return CreateClip("Sfx_Swing", 0.12f, (time, duration) =>
            {
                float envelope = (1f - time / duration) * 0.35f;
                float noise = Random.value * 2f - 1f;
                float whoosh = Mathf.Sin(time * 900f * (1.6f - time / duration));
                return (noise * 0.45f + whoosh * 0.2f) * envelope;
            });
        }

        private static AudioClip CreateHit()
        {
            return CreateClip("Sfx_Hit", 0.14f, (time, duration) =>
            {
                float envelope = Mathf.Exp(-time * 18f);
                float punch = Mathf.Sin(2f * Mathf.PI * 180f * time);
                float click = Mathf.Sin(2f * Mathf.PI * 720f * time) * Mathf.Exp(-time * 40f);
                float noise = (Random.value * 2f - 1f) * 0.35f;
                return (punch * 0.7f + click * 0.4f + noise) * envelope * 0.7f;
            });
        }

        private static AudioClip CreateCounter()
        {
            return CreateClip("Sfx_Counter", .20f, (time, duration) =>
            {
                float body = .49f * Mathf.Sin(2f * Mathf.PI * 154f * time) * Mathf.Exp(-32f * time);
                float ring = .26f * Mathf.Sin(2f * Mathf.PI * 913f * time)
                    + .13f * Mathf.Sin(2f * Mathf.PI * 1477f * time);
                return (body + ring * Mathf.Exp(-23f * time)) * (1f - time / duration);
            });
        }

        private static AudioClip CreateKill()
        {
            return CreateClip("Sfx_Kill", 0.22f, (time, duration) =>
            {
                float envelope = Mathf.Exp(-time * 10f);
                float punch = Mathf.Sin(2f * Mathf.PI * 90f * time);
                float body = Mathf.Sin(2f * Mathf.PI * 140f * time) * 0.5f;
                float noise = (Random.value * 2f - 1f) * 0.4f;
                return (punch + body + noise) * envelope * 0.8f;
            });
        }

        private static AudioClip CreateHurt()
        {
            return CreateClip("Sfx_Hurt", 0.16f, (time, duration) =>
            {
                float envelope = Mathf.Exp(-time * 14f);
                float thud = Mathf.Sin(2f * Mathf.PI * 240f * time);
                float noise = (Random.value * 2f - 1f) * 0.5f;
                return (thud * 0.6f + noise) * envelope * 0.65f;
            });
        }

        private static AudioClip CreateClip(
            string clipName,
            float duration,
            System.Func<float, float, float> sample)
        {
            const int Frequency = 22050;
            int length = Mathf.CeilToInt(Frequency * duration);
            float[] data = new float[length];

            for (int i = 0; i < length; i++)
            {
                float time = i / (float)Frequency;
                data[i] = Mathf.Clamp(sample(time, duration), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, length, 1, Frequency, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

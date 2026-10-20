using System.Collections.Generic;
using AbyssHunter.Character;
using AbyssHunter.UI;
using UnityEngine;

namespace AbyssHunter.World
{
    public sealed class AbyssSoundscape : MonoBehaviour
    {
        private readonly List<AudioClip> clips = new();
        private readonly List<AudioSource> beds = new();
        private AudioSource stones, fire;
        private AudioClip fall;
        private AbyssExpedition mission;
        private LastLight lantern;
        private float nextFall = 9;
        private void Start()
        {
            mission = GetComponent<AbyssExpedition>();
            lantern = FindFirstObjectByType<LastLight>();
            var wind = Make("深渊穿堂风", 0, 8);
            foreach (float z in new[] {12f, 36f}) beds.Add(Source("桥下风声", new Vector3(0, -2, z), wind, .22f, 20, true));
            beds.Add(Source("庭院锁链", new Vector3(-7, 2, 25), Make("远处锁链", 1, 8), .16f, 15, true));
            beds.Add(Source("祭坛低鸣", new Vector3(0, 2, 52), Make("深处共鸣", 2, 8), .16f, 17, true));
            fall = Make("坠落碎石", 3, 2);
            stones = Source("裂隙落石", new Vector3(13, -4, 24), fall, .18f, 28, false);
            if (lantern != null)
            {
                fire = Source("护火灯燃烧", lantern.transform.position, Make("火苗轻响", 4, 8), .09f, 18, true);
                fire.transform.SetParent(lantern.transform, true);
            }
        }
        private AudioSource Source(string name, Vector3 position, AudioClip clip, float volume, float range, bool loop)
        {
            var go = new GameObject(name); go.transform.SetParent(transform); go.transform.position = position;
            var source = go.AddComponent<AudioSource>(); source.clip = clip; source.loop = loop; source.playOnAwake = false;
            source.volume = volume; source.spatialBlend = .85f; source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2; source.maxDistance = range; source.dopplerLevel = 0;
            if (loop) source.Play();
            return source;
        }
        private AudioClip Make(string name, int kind, int duration)
        {
            const int rate = 16000;
            var samples = new float[rate * duration]; var noise = new System.Random(440 + kind); float filtered = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate, n = (float)noise.NextDouble() * 2 - 1;
                filtered = Mathf.Lerp(filtered, n, .025f);
                float value = kind switch
                {
                    0 => filtered * (1.2f + .6f * Mathf.Sin(t * Mathf.PI / 4)) + Mathf.Sin(t * 2 * Mathf.PI * 43) * .025f,
                    1 => (Mathf.Sin(t * 2 * Mathf.PI * 740) + Mathf.Sin(t * 2 * Mathf.PI * 1127) * .45f) * Mathf.Exp(-(t % 2.7f) * 14) * .18f,
                    2 => (Mathf.Sin(t * 2 * Mathf.PI * 65) + Mathf.Sin(t * 2 * Mathf.PI * 98) * .4f) * .1f,
                    3 => n * Mathf.Exp(-t * 5) * .35f + Mathf.Sin(t * 2 * Mathf.PI * 85) * Mathf.Exp(-t * 3) * .1f,
                    _ => filtered * .5f + (noise.NextDouble() > .999 ? n * .13f : 0)
                };
                samples[i] = value * Mathf.Min(1, Mathf.Min(t, duration - t) * 6);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); clips.Add(clip); return clip;
        }
        private void Update()
        {
            bool active = GamePresentation.Current == null || GamePresentation.Current.State == GamePresentation.ScreenState.Playing;
            if (active) nextFall -= Time.deltaTime;
            if (nextFall <= 0) { stones.PlayOneShot(fall); nextFall = 14; }
            float quiet = mission != null && mission.BeaconLit ? .3f : 1;
            for (int i = 0; i < beds.Count; i++) beds[i].volume = Mathf.MoveTowards(beds[i].volume, (i < 2 ? .22f : .16f) * quiet * (active ? 1 : .45f), Time.unscaledDeltaTime * .1f);
            if (fire != null) fire.volume = .09f * lantern.Strength;
        }
        private void OnDestroy() { foreach (var clip in clips) if (clip != null) Destroy(clip); }
    }
}

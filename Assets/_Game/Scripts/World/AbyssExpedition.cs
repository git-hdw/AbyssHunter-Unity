using System;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using AbyssHunter.Inventory;
using AbyssHunter.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.World
{
    public sealed class AbyssExpedition : MonoBehaviour
    {
        [SerializeField] private EnemyWaveSpawner encounters;
        [SerializeField] private PlayerMovement hunter;
        [SerializeField] private ItemData shard;
        [SerializeField] private Transform core, beacon;
        [SerializeField] private GameObject coreVisual;
        [SerializeField] private Light[] returnLights;
        [SerializeField] private Renderer[] returnRunes;
        [SerializeField] private Light altarLight, beaconLight;
        [SerializeField] private Material awakenedRune;
        [SerializeField, Min(1)] private int requiredShards = 4;
        public bool HasCore { get; private set; }
        public bool BeaconLit { get; private set; }
        public bool Extracted { get; private set; }
        public int DepositedShards { get; private set; }
        public int RequiredShards => requiredShards;
        public Transform Core => core;
        public Transform Beacon => beacon;
        public int CarriedShards => inventory == null ? 0 : inventory.GetTotalQuantity(shard);
        public bool HuntComplete => encounters.CompletedEncounters == encounters.TotalWaves;
        public string Objective => !HuntComplete ? (encounters.CompletedEncounters == 0 ? "穿过遗迹入口" : encounters.CompletedEncounters == 1 ? "猎杀侵蚀守卫" : "肃清核心祭坛") : !HasCore ? "取回深渊核心" : !BeaconLit ? "点亮归途信标" : "撤离沉没遗迹";
        public string Detail => encounters.ActiveEnemies > 0 ? $"区域威胁  {encounters.EnemiesLeftInWave}  /  留意落锤前摇" : !HuntComplete ? "沿微光前进，寻找猎物" : !HasCore ? "靠近祭坛，按 E 取回核心" : !BeaconLit ? $"碎片 {CarriedShards} / {requiredShards}  ·  返回入口" : "信标已点亮，按 E 撤离";
        public Transform Destination => !HuntComplete ? encounters.NextApproach : !HasCore ? core : beacon;
        public string InteractionHint
        {
            get
            {
                if (CanReach(core) && !HasCore) return HuntComplete ? "E  /  取回深渊核心" : "核心仍被封印 · 先肃清三处威胁";
                if (CanReach(beacon)) return !HasCore ? "归途信标沉寂 · 需要深渊核心" : BeaconLit ? "E  /  带着核心撤离" : CarriedShards >= requiredShards ? $"E  /  投入 {requiredShards} 枚碎片，点亮信标" : $"碎片不足 · 还需 {requiredShards-CarriedShards} 枚";
                return "";
            }
        }
        public event Action<string, string> Announced;
        public event Action Completed;
        private PlayerInventory inventory;
        private Health health;
        private AudioSource source;
        private AudioClip resonance;
        private void Awake()
        {
            inventory = hunter.GetComponent<PlayerInventory>(); health = hunter.GetComponent<Health>();
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = .35f;
            resonance = MakeResonance();
            encounters.EncounterCompleted += OnAreaCleared;
        }
        private void OnAreaCleared(int area)
        {
            if (area == 2) Announce("侵蚀守卫已猎杀", "它的微光仍在祭坛深处回响。继续追猎。");
            else if (area == 3) Announce("核心封印已解除", "靠近祭坛，按 E 取回深渊核心。");
            else Announce("入口已肃清", "拾取碎片，穿过断桥，追猎庭院中的守卫。");
        }
        private bool CanReach(Transform point) => hunter != null && Vector3.Distance(hunter.transform.position, point.position) < 2.8f;
        public bool TryInteract()
        {
            if (health == null || health.IsDead || Extracted || MatchResult.HasEnded || GamePresentation.BlocksGameplay) return false;
            if (!HasCore && HuntComplete && CanReach(core))
            {
                HasCore = true; coreVisual.SetActive(false);
                var atmosphere = GetComponent<AbyssAtmosphere>();
                if (atmosphere != null) atmosphere.RecoverCore();
                else
                {
                    altarLight.color = new Color(.2f, .9f, 1); altarLight.intensity = 6;
                    foreach (var lamp in returnLights) { lamp.color = new Color(.2f, .95f, 1); lamp.intensity = 3; }
                    foreach (var rune in returnRunes) rune.sharedMaterial = awakenedRune;
                }
                Announce("已取得深渊核心", "归途的符文亮起了。返回入口，点亮信标。");
                return true;
            }
            if (!HasCore || !CanReach(beacon)) return false;
            if (!BeaconLit)
            {
                if (!inventory.TryConsume(shard, requiredShards)) return false;
                DepositedShards = requiredShards; BeaconLit = true;
                beaconLight.color = new Color(.3f, 1, .8f); beaconLight.intensity = 10;
                Announce("归途信标已点亮", "核心已稳定。再次按 E，完成撤离。");
            }
            else { Extracted = true; Completed?.Invoke(); }
            return true;
        }
        private void Announce(string title, string detail) { source.PlayOneShot(resonance); Announced?.Invoke(title, detail); }
        private void Update()
        {
            if (GamePresentation.BlocksGameplay || health == null || health.IsDead) return;
            if (coreVisual.activeSelf) coreVisual.transform.Rotate(0, 35 * Time.deltaTime, 0, Space.World);
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
        }
        private static AudioClip MakeResonance()
        {
            const int rate = 24000; var samples = new float[rate*2];
            for (int i=0;i<samples.Length;i++) { float t=(float)i/rate; samples[i]=(Mathf.Sin(t*2*Mathf.PI*220)+Mathf.Sin(t*2*Mathf.PI*330)*.5f)*.2f*Mathf.Exp(-t*2)*Mathf.Min(t*30,1); }
            var clip=AudioClip.Create("遗迹共鸣",samples.Length,1,rate,false); clip.SetData(samples,0); return clip;
        }
        private void OnDestroy() { if(encounters!=null)encounters.EncounterCompleted-=OnAreaCleared; if(resonance!=null)Destroy(resonance); }
    }
}

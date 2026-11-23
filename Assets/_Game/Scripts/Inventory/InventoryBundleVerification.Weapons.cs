#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using AbyssHunter.Combat;
using AbyssHunter.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AbyssHunter.Inventory
{
    public sealed partial class InventoryBundleVerification
    {
        private static bool MatchesWeapon(PlayerMeleeAttack attack, HunterWeapon weapon) =>
            attack.UsesAxe == (weapon == HunterWeapon.Axe) && attack.UsesCrossbow == (weapon == HunterWeapon.Crossbow);

        private IEnumerator ChooseDeparture(LevelDefinition level, HunterWeapon weapon)
        {
            var menu = GamePresentation.Current;
            menu.BeginRun();
            void Click(string name)
            {
                var button = menu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == name);
                if (!button.interactable) throw new InvalidOperationException(name);
                button.onClick.Invoke();
            }
            Click("Level " + level.Id);
            Check(menu.State == GamePresentation.ScreenState.WeaponSelect, "选关后等待选择出战武器");
            var capture = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--weapon-review=", StringComparison.Ordinal));
            if (capture != null)
            {
                var folder = capture.Substring("--weapon-review=".Length);
                Directory.CreateDirectory(folder);
                yield return new WaitForSecondsRealtime(.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "departure.png"));
                yield return new WaitForEndOfFrame();
                yield return null;
            }
            Click("Equip " + weapon);
        }

        private IEnumerator VerifyHunterWeapons(HunterWeapon expected)
        {
            var attack = player.GetComponent<PlayerMeleeAttack>();
            Check(player.GetComponentInChildren<HunterWeapons>() && MatchesWeapon(attack, expected), "入场持有出发时指定的武器");
            const string option = "--weapon-review=";
            var arg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(option, StringComparison.Ordinal));
            if (arg == null) yield break;
            var folder = arg.Substring(option.Length);
            Directory.CreateDirectory(folder);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var targets = new GameObject[2];
            var weapons = player.GetComponentInChildren<HunterWeapons>();
            bool record = Environment.GetCommandLineArgs().Contains("--weapon-motion");
            int originalCaptureRate = Time.captureFramerate;
            try
            {
                if (record) Time.captureFramerate = 30;
                GamePresentation.Current.ShowPause();
                yield return PressQ();
                Check(MatchesWeapon(attack, expected), "暂停时武器锁定");
                GamePresentation.Current.Resume();
                yield return PressQ();
                Check(MatchesWeapon(attack, expected), "战斗中按 Q 保持出战武器");
                Vector3 center = player.transform.position + Vector3.right * 1.35f;
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = new GameObject("武器回归靶");
                    targets[i].transform.position = center + Vector3.forward * i * .85f;
                    targets[i].AddComponent<SphereCollider>().radius = .35f;
                    targets[i].AddComponent<Health>();
                }
                Physics.SyncTransforms();
                var hp = targets[0].GetComponent<Health>();
                int before = hp.CurrentHealth;
                Vector2 aim = Camera.main.WorldToScreenPoint(center);
                if (expected == HunterWeapon.Sword)
                {
                    yield return Shoot(aim);
                    yield return new WaitForSeconds(.65f);
                    Check(hp.CurrentHealth == before - 25, "出战猎剑的动画事件造成 25 点伤害");
                    yield break;
                }
                if (expected == HunterWeapon.Axe)
                {
                    mouse.MakeCurrent();
                    if (record) StartCoroutine(RecordSwing(folder, "axe"));
                    InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, position = aim });
                    yield return null;
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = aim });
                    yield return PressQ();
                    Check(attack.UsesAxe && hp.CurrentHealth == before, "战斧前摇锁定武器，命中前无伤害");
                    yield return Until(() => hp.CurrentHealth < before, "等待重斧接触目标");
                    yield return new WaitForEndOfFrame();
                    var handle = weapons.AxeHandle;
                    float separation = Vector3.Cross(weapons.SupportGrip.position - handle.position, handle.up).magnitude;
                    float facing = Vector3.Dot(handle.up, player.transform.forward);
                    Check(separation < .06f && facing > .75f, $"横扫命中时双手贴柄，斧头经过身前（离柄 {separation:F2} 米，朝前点积 {facing:F2}）");
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder, "axe.png"));
                    yield return new WaitForSeconds(.64f);
                    Check(targets.All(t => t.GetComponent<Health>().CurrentHealth == before - 42), "真实横扫动画事件对两个目标各结算一次 42 点伤害");
                    yield break;
                }
                Vector3 far = player.transform.position + Vector3.forward * 6;
                targets[0].transform.position = far;
                targets[1].transform.position = far + Vector3.forward;
                aim = Camera.main.WorldToScreenPoint(far);
                Physics.SyncTransforms();
                float muzzleGap = -1, barrelAngle = 180;
                void MeasureRelease()
                {
                    if (!attack.BoltInFlight) return;
                    muzzleGap = Vector3.Distance(weapons.Bolt.transform.position, weapons.Muzzle);
                    barrelAngle = Vector3.Angle(weapons.Bolt.transform.forward, weapons.BarrelDirection);
                }
                attack.BoltReleased += MeasureRelease;
                try
                {
                    if (record) StartCoroutine(RecordSwing(folder, "crossbow"));
                    yield return Shoot(aim);
                    yield return Until(() => attack.BoltInFlight, "等待动画事件发射轻弩");
                }
                finally { attack.BoltReleased -= MeasureRelease; }
                Check(muzzleGap >= 0 && muzzleGap < .02f && barrelAngle < 15,
                    $"弩矢从弩口沿瞄准方向飞出（起点误差 {muzzleGap:F2} 米，夹角 {barrelAngle:F1} 度）");
                Check(hp.CurrentHealth == before, "发射与远处受击之间有飞行时间");
                yield return new WaitForSeconds(.13f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "crossbow.png"));
                yield return new WaitForSeconds(.85f);
                Check(hp.CurrentHealth == before - 20 && targets[1].GetComponent<Health>().CurrentHealth == before,
                    "六米外首个目标受一次 20 点伤害，弩矢未穿透");
                var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    cover.transform.position = (player.transform.position + far) * .5f;
                    cover.transform.localScale = new Vector3(2, 2, .45f);
                    Physics.SyncTransforms();
                    yield return Shoot(aim);
                    yield return Until(() => attack.BoltInFlight, "等待第二支弩矢");
                    yield return new WaitForSeconds(1);
                    Check(!attack.BoltInFlight && hp.CurrentHealth == before - 20, "实体掩体截停射向远处目标的弩矢");
                    cover.transform.position = player.transform.position + Vector3.forward * .58f;
                    cover.transform.localScale = new Vector3(2, 3, .10f);
                    Physics.SyncTransforms();
                    bool blockedAtRelease = false;
                    void MeasureObstruction() => blockedAtRelease = !attack.BoltInFlight;
                    attack.BoltReleased += MeasureObstruction;
                    try
                    {
                        yield return Shoot(aim);
                        yield return new WaitForSeconds(1);
                        Check(blockedAtRelease && hp.CurrentHealth == before - 20, "弩口伸过近墙时弩矢仍被墙截停");
                    }
                    finally { attack.BoltReleased -= MeasureObstruction; }
                }
                finally { Destroy(cover); }
                int releases = 0;
                void CountRelease() => releases++;
                attack.BoltReleased += CountRelease;
                try
                {
                    yield return Shoot(aim);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                    yield return null;
                    yield return null;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return new WaitForSeconds(1);
                    Check(releases == 0 && hp.CurrentHealth == before - 20, "起手时闪避可取消本次发射");
                }
                finally { attack.BoltReleased -= CountRelease; }
                Place(center - Vector3.right * 1.35f);
                player.GetComponent<Health>().SetInvulnerable(true);
            }
            finally
            {
                if (record) Time.captureFramerate = originalCaptureRate;
                foreach (var target in targets) if (target) Destroy(target);
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
            }

            IEnumerator Shoot(Vector2 pointer)
            {
                mouse.MakeCurrent();
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, position = pointer });
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            }

            IEnumerator PressQ()
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
            }
        }

        private IEnumerator RecordSwing(string folder, string weapon)
        {
            string frames = Path.Combine(folder, weapon);
            Directory.CreateDirectory(frames);
            for (int frame = 0; frame < 27; frame++)
            {
                yield return new WaitForEndOfFrame();
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                try { File.WriteAllBytes(Path.Combine(frames, $"{frame:D3}.jpg"), texture.EncodeToJPG(92)); }
                finally { Destroy(texture); }
            }
        }
    }
}
#endif

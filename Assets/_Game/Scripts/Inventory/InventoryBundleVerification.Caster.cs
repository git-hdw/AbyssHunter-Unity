#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AbyssHunter.Inventory
{
    public sealed partial class InventoryBundleVerification
    {
        private RiftCaster watchCaster;
        private IEnumerator VerifyCaster(int area)
        {
            var caster = FindObjectsByType<RiftCaster>(FindObjectsSortMode.None).Single();
            if (area == 3)
            {
                Check(caster == watchCaster && !caster.GetComponent<Health>().IsDead, "祭坛复用存活的法师实例");
                yield break;
            }
            watchCaster = caster;
            const string prefix = "--caster-review=";
            string directory = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length);
            if (directory == null) yield break;
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Fire mage review requires a graphical Player");
            Directory.CreateDirectory(directory);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "00-encounter.png"));
            var guard = FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Single(c => c.IsHeavy);
            guard.GetComponent<Health>().TakeDamage(1000);
            yield return null;
            var threatLabel = AbyssHunter.UI.GamePresentation.Current.GetComponentsInChildren<UnityEngine.UI.Text>().Single(t => t.name == "Remaining");
            Check(threatLabel.text == "场上敌人  1  /  杖头聚火时横移", "守卫倒下即撤去落锤提示，只显示存活法师");
            var hp = player.GetComponent<Health>();
            Vector3 stand = caster.transform.position + Vector3.back * 5;
            stand.y = 2.55f;
            Place(stand);
            yield return new WaitForSeconds(2);
            yield return Until(() => !caster.IsCharging && !caster.InFlight, "等待火球结束");
            hp.SetInvulnerable(false);
            int initial = hp.CurrentHealth;
            yield return Until(() => caster.IsCharging, "等待火球锁向");
            yield return RecordFireball(caster, hp, initial, directory);
            Check(hp.CurrentHealth == initial - 12, "站在弹道上只受一次12点伤害");
            int remaining = hp.CurrentHealth;
            yield return Until(() => caster.IsCharging, "等待下一次锁向");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                yield return new WaitForSeconds(.7f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return new WaitForSeconds(1.9f);
                Check(Vector3.Distance(player.transform.position, stand) > 2 && hp.CurrentHealth == remaining, "锁向后真实横移可躲开火球");
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            Place(stand);
            yield return Until(() => caster.IsCharging, "等待掩体验证锁向");
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                cover.transform.position = (caster.transform.position + player.transform.position) * .5f;
                cover.transform.localScale = new Vector3(2, 3, .4f);
                Physics.SyncTransforms();
                Check(!caster.CanSee(player.transform), "实体掩体遮断视线");
                yield return Until(() => caster.InFlight, "等待向掩体发射");
                yield return Until(() => !caster.InFlight, "等待掩体截断火球");
                Check(hp.CurrentHealth == remaining, "实体掩体挡住已锁向火球");
            }
            finally { Destroy(cover); }
            hp.SetInvulnerable(true);
            yield return Until(() => caster.IsCharging, "等待击退打断");
            caster.GetComponent<EnemyController>().ApplyKnockback(Vector3.right, .45f, .15f);
            Check(!caster.IsCharging && !caster.InFlight, "击退打断蓄力");
            yield return Until(() => caster.IsCharging, "等待死亡打断");
            caster.GetComponent<Health>().TakeDamage(1000);
            Check(!caster.IsCharging && !caster.InFlight, "死亡清除蓄力和弹体");
        }

        private IEnumerator RecordFireball(RiftCaster caster, Health hp, int initial, string directory)
        {
            var mage = caster.GetComponentInChildren<Animator>();
            Check(mage.isHuman && mage.avatar.isValid && !mage.applyRootMotion &&
                mage.HasState(0, Animator.StringToHash("Base Layer.ReleaseFire")), "法师使用有效的人形骨架与放火动作");
            int previous = Time.captureFramerate;
            const int fps = 24;
            string frames = Path.Combine(directory, "cast");
            Directory.CreateDirectory(frames);
            bool sawFlight = false, sawImpact = false;
            int flightFrame = 0;
            try
            {
                Time.captureFramerate = fps;
                for (int i = 0; i < 48; i++)
                {
                    yield return new WaitForEndOfFrame();
                    var screen = ScreenCapture.CaptureScreenshotAsTexture();
                    try
                    {
                        byte[] png = screen.EncodeToPNG();
                        File.WriteAllBytes(Path.Combine(frames, $"{i:D3}.png"), png);
                        if (i == 10)
                        {
                            Check(caster.IsCharging && hp.CurrentHealth == initial, "举杖聚火期间未提前造成伤害");
                            File.WriteAllBytes(Path.Combine(directory, "01-windup.png"), png);
                        }
                        if (caster.InFlight)
                        {
                            sawFlight = true;
                            if (++flightFrame == 7) File.WriteAllBytes(Path.Combine(directory, "02-flight.png"), png);
                        }
                        else if (sawFlight && !sawImpact)
                        {
                            sawImpact = true;
                            File.WriteAllBytes(Path.Combine(directory, "03-impact.png"), png);
                        }
                    }
                    finally { Destroy(screen); }
                }
            }
            finally { Time.captureFramerate = previous; }
            Check(sawFlight && sawImpact, "连续画面覆盖放火与碰撞");
        }
    }
}
#endif

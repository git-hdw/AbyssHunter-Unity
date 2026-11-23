#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AbyssHunter.Inventory
{
    public sealed partial class InventoryBundleVerification
    {
        private IEnumerator CaptureRecoveryCounter()
        {
            const string prefix = "--counter-capture=";
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal));
            if (option == null) yield break;
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Counter capture requires a graphical Player");

            string directory = Path.GetFullPath(option.Substring(prefix.Length));
            var wave = FindObjectsByType<PooledEnemy>(FindObjectsSortMode.None);
            var brute = wave.Single(e => e.Controller.IsHeavy);
            var skeletons = wave.Where(e => e != brute).ToArray();
            bool mixed = Environment.GetCommandLineArgs().Contains("--mixed-counter");
            if (!mixed)
                foreach (var enemy in skeletons) enemy.GetComponent<Health>().TakeDamage(1000);
            Vector3 approach = mixed ? new Vector3(-1.7f, 0, -.9f) : Vector3.back * 1.7f;
            Place(brute.transform.position + approach);
            var controller = brute.Controller;
            var profile = controller.HeavyAttack;
            var attack = player.GetComponent<PlayerMeleeAttack>();
            var hunterHealth = player.GetComponent<Health>();
            var guardHealth = brute.GetComponent<Health>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            int hits = 0;
            void CountHit(bool killed) => hits++;
            bool dodgedSlam = false;
            void RecordSlam() => dodgedSlam = player.IsDodging && hunterHealth.IsInvulnerable;
            bool aimingAtGuard = true;
            int swings = 0;
            void CheckAim()
            {
                swings++;
                var origin = player.transform.position;
                Vector3 Offset(PooledEnemy e) => Vector3.ProjectOnPlane(e.GetComponent<Collider>().bounds.center - origin, Vector3.up);
                var nearest = wave.Where(e => !e.GetComponent<Health>().IsDead).OrderBy(e => Offset(e).sqrMagnitude).First();
                var expected = aimingAtGuard ? brute : nearest;
                float angle = Vector3.Angle(player.transform.forward, Offset(expected));
                float separation = Vector3.Angle(Offset(nearest), Offset(brute));
                Check(angle < 2 && (!aimingAtGuard || (nearest != brute && separation > 12)),
                    $"混战出刀朝向{(aimingAtGuard ? "点中的重锤" : "空地点击时最近的活敌")}，夹角 {angle:F1}°，最近为 {nearest.name}");
            }
            if (mixed) attack.Attacked += CheckAim;
            attack.HitConfirmed += CountHit;
            bool wasInvulnerable = hunterHealth.IsInvulnerable;
            try
            {
                if (mixed)
                {
                    yield return new WaitForSecondsRealtime(4);
                    yield return Until(() => controller.CurrentState == EnemyState.Windup &&
                        controller.PhaseElapsed >= profile.trackingTime, "等待混战锁向");
                    yield return new WaitForEndOfFrame();
                    Check(skeletons.Length == 2 && skeletons.All(e => e.isActiveAndEnabled && !e.GetComponent<Health>().IsDead),
                        "第三遭遇锁向时两名普通骷髅均存活");
                    WriteFrame(directory, "00-mixed-lock");
                    var label = brute.GetComponentInChildren<TextMesh>();
                    float deadline = Time.realtimeSinceStartup + 8f;
                    do { yield return new WaitForEndOfFrame(); }
                    while (label.text != "即将落锤" && Time.realtimeSinceStartup < deadline);
                    Check(label.text == "即将落锤" && controller.CurrentState == EnemyState.Windup && profile.windup - controller.PhaseElapsed <= .2f &&
                        label.text.All(ch => label.font.HasCharacter(ch)), "落锤前提示在实际伤害前出现且字体有效");
                    WriteFrame(directory, "00-mixed-imminent");
                }
                yield return Until(() => controller.CurrentState == EnemyState.Windup &&
                    controller.PhaseElapsed < profile.trackingTime, "等待完整蓄力");
                int initialHealth = hunterHealth.CurrentHealth;
                controller.HeavyImpact += RecordSlam;
                hunterHealth.SetInvulnerable(false);
                yield return Until(() => controller.CurrentState == EnemyState.Windup &&
                    controller.PhaseElapsed >= profile.windup - .11f, "等待落锤");
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
                yield return Until(() => player.IsDodging, "等待空格闪避");
                if (mixed)
                {
                    // Put one live attacker on the landing flank to reproduce a closer competing target.
                    var flank = player.transform.position + new Vector3(-.9f, 0, 2.5f);
                    Check(skeletons[0].GetComponent<NavMeshAgent>().Warp(flank), "普通骷髅就位于闪避侧翼");
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                mouse.MakeCurrent();
                var body = brute.GetComponent<Collider>();
                Vector2 pointer = Camera.main.WorldToScreenPoint(body.bounds.center + Vector3.up * body.bounds.extents.y * .65f);
                if (mixed)
                {
                    Physics.Raycast(Camera.main.ScreenPointToRay(pointer), out var picked, Camera.main.farClipPlane,
                        Camera.main.cullingMask & Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    Check(picked.collider == body, $"按下前屏幕射线命中 {picked.collider?.name}，目标为 {body.name}");
                }
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, position = mixed ? pointer : Vector2.zero });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return Until(() => hits > 0, "等待闪避后第一刀动画命中");
                var contactPosition = brute.transform.position;
                Check(dodgedSlam && (mixed || hunterHealth.CurrentHealth == initialHealth) && !player.IsDodging &&
                    hits == 1 && guardHealth.CurrentHealth == guardHealth.MaximumHealth - 25 &&
                    controller.IsRecoveryStaggered && attack.LastHitStaggeredHeavy,
                    "空格闪过落锤，闪避中点击一次，在后摇造成一次反击");
                yield return new WaitForEndOfFrame();
                Check(brute.GetComponentInChildren<TextMesh>().text == "反击命中" &&
                    Mathf.Approximately(controller.RecoveryDuration, profile.recovery + profile.recoveryStaggerBonus),
                    "反击当帧切换提示并延长后摇");
                var counterSound = Resources.FindObjectsOfTypeAll<AudioClip>().FirstOrDefault(clip => clip.name == "Sfx_Counter");
                Check(counterSound != null && player.GetComponent<AudioSource>().isPlaying,
                    "反击命中时专属音频已生成且玩家音源正在播放");
                WriteFrame(directory, "01-counter");
                yield return new WaitForSeconds(.10f);
                yield return new WaitForEndOfFrame();
                WriteFrame(directory, "02-recoil");
                yield return new WaitForSeconds(.09f);
                Check(Vector3.Distance(contactPosition, brute.transform.position) > .15f && hits == 1,
                    "第一刀击退重锤且没有重复命中");
                if (mixed)
                {
                    Check(skeletons.All(e => !e.GetComponent<Health>().IsDead), "反击命中时两个普通敌人仍在战斗");
                    aimingAtGuard = false;
                    yield return new WaitForSeconds(.5f);
                    Vector2 ground = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.right * 4);
                    InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, position = ground });
                    yield return null;
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = ground });
                    yield return Until(() => swings == 2, "等待空地点击后的普通出刀");
                }
            }
            finally
            {
                attack.HitConfirmed -= CountHit;
                controller.HeavyImpact -= RecordSlam;
                if (mixed) attack.Attacked -= CheckAim;
                hunterHealth.SetInvulnerable(wasInvulnerable);
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
            }
        }
    }
}
#endif

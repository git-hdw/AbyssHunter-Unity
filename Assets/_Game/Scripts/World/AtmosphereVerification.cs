#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using AbyssHunter.UI;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace AbyssHunter.World
{
    public sealed class AtmosphereVerification : MonoBehaviour
    {
        private readonly List<string> report = new();
        private PlayerMovement player;
        private Health health;
        private LastLight lantern;
        private AbyssExpedition mission;
        private int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!SessionState.GetBool("AbyssHunter.ArtReview", false)) return;
            SessionState.EraseBool("AbyssHunter.ArtReview");
            new GameObject("环境与微光验证").AddComponent<AtmosphereVerification>();
        }
        private void Check(bool pass, string text) { report.Add((pass ? "PASS | " : "FAIL | ") + text); Save(); }
        private void Save() => File.WriteAllLines("Logs/atmosphere-verification.txt", report);
        private void Log(string message, string stack, LogType type) { if (type is LogType.Error or LogType.Exception) { errors++; Check(false, message); } }
        private void Place(Vector3 position)
        {
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false; player.transform.position = position; cc.enabled = true;
        }
        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Logs/atmosphere-review/" + name + ".png");
            yield return new WaitForEndOfFrame(); yield return null;
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory("Logs/atmosphere-review"); Application.runInBackground = true;
            Application.logMessageReceived += Log;
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                bool moved; object value = null; var top = stack.Peek();
                try { moved = top.MoveNext(); if (moved) value = top.Current; }
                catch (Exception e) { Check(false, e.ToString()); break; }
                if (!moved) { stack.Pop(); continue; }
                if (value is IEnumerator nested) stack.Push(nested); else yield return value;
            }
            Check(errors == 0, "环境验证无运行错误"); report.Add("DONE"); Save();
            EditorApplication.isPlaying = false;
        }
        private IEnumerator Run()
        {
            yield return null;
            player = FindFirstObjectByType<PlayerMovement>(); health = player.GetComponent<Health>();
            lantern = player.GetComponentInChildren<LastLight>(); mission = FindFirstObjectByType<AbyssExpedition>();
            health.SetInvulnerable(true);
            Check(lantern != null && lantern.transform.IsChildOf(player.transform), "护火灯随猎手骨骼挂载");
            Check(FindObjectsByType<LastLight>(FindObjectsSortMode.None).Length == 1, "只有一盏随身微光");
            Check(!ShaderUtil.ShaderHasError(Shader.Find("AbyssHunter/DepthMist")), "深渊薄雾着色器编译正常");
            Check(GameObject.Find("深渊环境美术").GetComponentsInChildren<Collider>().Length == 0, "环境装饰未新增阻路碰撞体");
            Check(FindObjectsByType<Renderer>(FindObjectsSortMode.None).All(r => r.sharedMaterials.All(m => m != null && m.shader.name != "Hidden/InternalErrorShader")), "材质引用与着色器有效");
            yield return new WaitForSecondsRealtime(.5f); yield return Shot("00-opening");
            float openingDistance = Vector3.Distance(Camera.main.transform.position, player.transform.position);
            var fragment = GameObject.Find("祭坛悬石").transform;
            Vector3 fragmentPosition = fragment.localPosition;
            yield return new WaitForSecondsRealtime(1.5f);
            Check(GamePresentation.Current.State == GamePresentation.ScreenState.Intro && Time.timeScale == 0 && Vector3.Distance(Camera.main.transform.position, player.transform.position) > openingDistance + 1, "开场暂停游戏时间时镜头仍平滑拉远");
            Check(Vector3.Distance(fragmentPosition, fragment.localPosition) > .01f && fragment.GetComponent<Renderer>().enabled, "祭坛悬石独立渲染并缓慢浮动");
            Check(FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Count(p => p.name == "深渊缓落碎屑" && p.isPlaying && p.main.maxParticles == 12) == 2, "深渊两侧低数量落屑正常运行");
            yield return Shot("00-opening-wide");
            yield return new WaitForSecondsRealtime(1.5f);
            Check(lantern.Strength > .7f && lantern.Glow.range < 4, "微光点燃且照明范围局部");
            yield return Shot("01-entrance");
            var cam = Camera.main; var follow = cam.GetComponent<TopDownCamera>();
            var canvas = GamePresentation.Current.GetComponent<Canvas>();
            if (canvas == null) canvas = GamePresentation.Current.GetComponentInChildren<Canvas>();
            follow.enabled = false; if (canvas != null) canvas.enabled = false;
            cam.transform.position = player.transform.position + new Vector3(-3, 1.9f, -3.8f); cam.transform.LookAt(player.transform.position + Vector3.up * .1f);
            yield return Shot("02-lantern-close");
            follow.enabled = true; if (canvas != null) canvas.enabled = true;
            float before = lantern.Strength; health.SetInvulnerable(false); health.TakeDamage(10);
            yield return new WaitForSecondsRealtime(.14f);
            Check(lantern.Strength < before, "受击压低火苗");
            yield return new WaitForSecondsRealtime(1);
            Check(lantern.Strength > .8f, "微光在受击后恢复"); health.Heal(10); health.SetInvulnerable(true);
            GamePresentation.Current.ShowPause(); before = lantern.Strength;
            yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Abs(lantern.Strength - before) < .01f, "暂停冻结微光状态"); GamePresentation.Current.Resume();
            Place(new Vector3(-3.8f, 1, -1)); yield return new WaitForSecondsRealtime(1);
            Check(FindObjectsByType<AbyssOccluder>(FindObjectsSortMode.None).Any(o => o.IsFaded), "镜头前的建筑自动淡化");
            yield return Shot("03-occlusion");
            Place(new Vector3(0, 1, -5)); yield return new WaitForSecondsRealtime(1.3f);
            Check(FindObjectsByType<AbyssOccluder>(FindObjectsSortMode.None).All(o => !o.IsFaded), "移出遮挡后建筑恢复");
            var timings = new List<double>(); int maxBatches = 0, maxTriangles = 0;
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1))
            {
                Check(recorder.Valid, "Profiler 主线程采样器可用");
                foreach (float z in new[] {-3f, 12f, 21f, 36f, 45f})
                {
                    Place(new Vector3(0, 1, z)); yield return new WaitForSecondsRealtime(3.8f);
                    yield return Shot("zone-" + z);
                    for (int i = 0; i < 90; i++)
                    {
                        yield return null;
                        if (recorder.Valid && recorder.LastValue > 0) timings.Add(recorder.LastValue / 1000000.0);
                        maxBatches = Mathf.Max(maxBatches, UnityStats.batches); maxTriangles = Mathf.Max(maxTriangles, UnityStats.triangles);
                    }
                    var encounters = FindFirstObjectByType<EnemyWaveSpawner>();
                    int current = encounters.CurrentWave;
                    foreach (var enemy in FindObjectsByType<PooledEnemy>(FindObjectsSortMode.None)) enemy.GetComponent<Health>().TakeDamage(1000);
                    float deadline = Time.realtimeSinceStartup + 4;
                    while (encounters.CompletedEncounters < current && Time.realtimeSinceStartup < deadline) yield return null;
                }
            }
            timings.Sort();
            if (timings.Count > 0) report.Add($"INFO | 编辑器 Main Thread：{timings.Count} 帧，中位 {timings[timings.Count / 2]:F2} ms，P95 {timings[(int)(timings.Count * .95f)]:F2} ms；含编辑器及等待时间，不等同独立包性能。");
            report.Add($"INFO | 五处镜头峰值 Batches={maxBatches}, Triangles={maxTriangles}, AllocatedMemory={Profiler.GetTotalAllocatedMemoryLong()/1048576} MB"); Save();
            Check(mission.HuntComplete, "三处遭遇完成并回收到对象池后解封祭坛");
            Place(mission.Core.position + Vector3.up); yield return new WaitForSeconds(.1f); mission.TryInteract();
            var atmosphere = mission.GetComponent<AbyssAtmosphere>();
            Check(mission.HasCore && !atmosphere.RouteAwakened, "取回核心先播放火种传递");
            yield return new WaitForSeconds(.3f); yield return Shot("04-core-transfer");
            yield return new WaitForSeconds(4);
            Check(atmosphere.RouteAwakened && lantern.Strength > 1, "归途依次苏醒，护火灯稳定");
            var fire = FindObjectsByType<AudioSource>(FindObjectsSortMode.None).FirstOrDefault(a => a.name == "护火灯燃烧");
            Check(fire != null && fire.isPlaying && fire.clip.samples > 0 && fire.volume > 0, "随身火焰空间音源正常播放");
            Check(FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(a => a.name == "桥下风声") == 2, "两段断桥具有独立风声位置");
            // Existing test driver supplies the progression fixture's four shards.
            ExpeditionTestDriver.Advance(player); yield return new WaitForSeconds(1.2f);
            Check(mission.BeaconLit && atmosphere.BeaconStrength > .95f, "信标逐渐燃起暖色火焰");
            yield return Shot("05-beacon");
            health.SetInvulnerable(false); health.TakeDamage(1000); yield return new WaitForSecondsRealtime(.6f);
            Check(lantern.Strength < .01f && lantern.Glow.intensity < .01f, "死亡后微光熄灭");
            yield return Shot("06-extinguished");
        }
        private void OnDestroy() => Application.logMessageReceived -= Log;
    }
}
#endif

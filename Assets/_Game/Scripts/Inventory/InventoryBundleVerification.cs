#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using AbyssHunter.UI;
using AbyssHunter.World;
using UnityEngine;
using UnityEngine.AI;

namespace AbyssHunter.Inventory
{
    // Opt-in development-player regression. Uses real bundled enemy drops and controlled kills.
    // Compiled out of non-development releases; no test inventory or cheats enter the release.
    public sealed partial class InventoryBundleVerification : MonoBehaviour
    {
        private readonly List<string> report = new();
        private string reportPath;
        private bool failed;
        private PlayerMovement player;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--inventory-report=", StringComparison.Ordinal));
            if (option == null) return;
            var driver = new GameObject("独立包碎片回归").AddComponent<InventoryBundleVerification>();
            driver.reportPath = option.Substring("--inventory-report=".Length);
            DontDestroyOnLoad(driver.gameObject);
        }
        private void Check(bool value, string message)
        {
            failed |= !value; report.Add((value ? "PASS | " : "FAIL | ") + message);
            File.WriteAllLines(reportPath, report);
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type is LogType.Error or LogType.Exception) Check(false, message + "\n" + stack);
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            Application.runInBackground = true; Application.logMessageReceived += OnLog;
            var steps = new Stack<IEnumerator>(); steps.Push(Run());
            while (steps.Count > 0)
            {
                var step = steps.Peek(); bool moved; object value = null;
                try { moved = step.MoveNext(); if (moved) value = step.Current; }
                catch (Exception e) { Check(false, e.ToString()); break; }
                if (!moved) { steps.Pop(); continue; }
                if (value is IEnumerator nested) steps.Push(nested); else yield return value;
            }
            report.Add(failed ? "DONE | FAILED" : "DONE | PASSED"); File.WriteAllLines(reportPath, report);
            Application.logMessageReceived -= OnLog;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(failed ? 1 : 0);
#endif
        }
        private IEnumerator Until(Func<bool> condition, string name)
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(name);
                yield return null;
            }
        }
        private void Place(Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false; player.transform.position = position; controller.enabled = true;
        }
        private void CheckIdentity(ItemData sceneShard)
        {
            var duplicate = Instantiate(sceneShard);
            var other = ScriptableObject.CreateInstance<ItemData>();
            var emptyA = ScriptableObject.CreateInstance<ItemData>();
            var emptyB = ScriptableObject.CreateInstance<ItemData>();
            JsonUtility.FromJsonOverwrite("{\"itemId\":\"\"}", emptyA);
            JsonUtility.FromJsonOverwrite("{\"itemId\":\"\"}", emptyB);
            var fixture = new GameObject("背包身份回归"); var inventory = fixture.AddComponent<PlayerInventory>();
            Check(sceneShard != duplicate && sceneShard.HasSameIdentity(duplicate), "同 ID 的不同实例识别为同种物品");
            Check(!sceneShard.HasSameIdentity(other) && !sceneShard.HasSameIdentity(null) && !emptyA.HasSameIdentity(emptyB), "不同 ID、空引用及无效空 ID 不混同");
            inventory.TryAdd(sceneShard, 98, out _); inventory.TryAdd(duplicate, 3, out int remaining);
            Check(remaining == 0 && inventory.Slots[0].Quantity == 99 && inventory.Slots[1].Quantity == 2, "不同实例共享堆叠并正确溢出到下一格");
            Check(inventory.GetTotalQuantity(sceneShard) == 101 && inventory.GetTotalQuantity(duplicate) == 101 && inventory.GetTotalQuantity(null) == 0, "计数使用稳定 ID 跨格汇总");
            Check(!inventory.TryConsume(other, 1) && !inventory.TryConsume(duplicate, 102) && inventory.GetTotalQuantity(sceneShard) == 101, "错误物品或数量不足时不扣除任何碎片");
            Check(inventory.TryConsume(duplicate, 100) && inventory.GetTotalQuantity(sceneShard) == 1, "按 ID 跨格扣除且保留正确余量");
            Destroy(fixture); Destroy(duplicate); Destroy(other); Destroy(emptyA); Destroy(emptyB);
        }
        private IEnumerator Run()
        {
            string selected = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--inventory-level=", StringComparison.Ordinal))?.Substring("--inventory-level=".Length) ?? LevelCatalog.All[0].Id;
            var level = LevelCatalog.Find(selected);
            if (level == null) throw new ArgumentException("Unknown verification level");
            bool depths = level.Id == "abyss-depths";
            bool visitSupply = depths && Environment.GetCommandLineArgs().Contains("--supply-walk");
            int total = depths ? 9 : 6;
            const string weaponOption = "--start-weapon=";
            string choice = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(weaponOption, StringComparison.Ordinal));
            HunterWeapon expectedWeapon = HunterWeapon.Sword;
            if (choice != null)
            {
                expectedWeapon = (HunterWeapon)Enum.Parse(typeof(HunterWeapon), choice.Substring(weaponOption.Length), true);
                yield return ChooseDeparture(level, expectedWeapon);
            }
            else SceneFlow.LoadLevel(level.Id);
            yield return null;
            yield return Until(() => GamePresentation.Current != null && GamePresentation.Current.State == GamePresentation.ScreenState.Playing, "等待开场结束");
            player = FindFirstObjectByType<PlayerMovement>(); player.GetComponent<Health>().SetInvulnerable(true);
            if (depths)
            {
                VerifyGatePassage();
                yield return CaptureEntrance();
            }
            else yield return CaptureFoyer();
            GamePresentation.Current.ShowPause();
            Check(Time.timeScale == 0 && GamePresentation.BlocksGameplay, "暂停阻断玩法输入");
            GamePresentation.Current.Resume();
            GamePresentation.Current.ShowInventory();
            Check(Time.timeScale == 0 && GamePresentation.Current.State == GamePresentation.ScreenState.Inventory, "背包打开暂停游戏");
            GamePresentation.Current.Resume();
            var inventory = player.GetComponent<PlayerInventory>();
            var mission = FindFirstObjectByType<AbyssExpedition>(); var spawner = FindFirstObjectByType<EnemyWaveSpawner>();
            var sceneShard = (ItemData)typeof(AbyssExpedition).GetField("shard", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mission);
            yield return VerifyHunterWeapons(expectedWeapon);
            CheckIdentity(sceneShard);
            var navigation = new UnityEngine.AI.NavMeshPath();
            Check(UnityEngine.AI.NavMesh.CalculatePath(mission.Beacon.position, mission.Core.position, UnityEngine.AI.NavMesh.AllAreas, navigation) && navigation.status == UnityEngine.AI.NavMeshPathStatus.PathComplete, "信标至核心导航连通");
            if (depths)
            {
                UnityEngine.AI.NavMesh.SamplePosition(new Vector3(-38,1.5f,30), out var side, 2, UnityEngine.AI.NavMesh.AllAreas);
                Check(UnityEngine.AI.NavMesh.CalculatePath(mission.Beacon.position, side.position, UnityEngine.AI.NavMesh.AllAreas, navigation) && navigation.status == UnityEngine.AI.NavMeshPathStatus.PathComplete, "侧廊补给可达");
            }
            ItemData bundledShard = null;
            var hudLabels = GamePresentation.Current.GetComponentsInChildren<UnityEngine.UI.Text>();
            var threatLabel = hudLabels.Single(t => t.name == "Remaining");
            var routeLabel = hudLabels.Single(t => t.name == "Destination");
            for (int area = 1; area <= 3; area++)
            {
                Place(spawner.NextApproach.position + Vector3.up);
                int expected = depths ? (area == 1 ? 3 : area == 2 ? 2 : 4) : (area == 1 ? 2 : area == 2 ? 1 : 3);
                yield return Until(() => spawner.CurrentWave == area && spawner.ActiveEnemies == expected, "等待遭遇 " + area);
                yield return null;
                string hint = area == 1 ? "留意挥砍，闪避后反击" : depths ? "横移躲火，留意落锤" : "躲开落锤，趁隙反击";
                Check(threatLabel.text == $"场上敌人  {expected}  /  {hint}" && routeLabel.text == "",
                    $"遭遇 {area} 按在场阵容提示，隐藏触发点方向");
                if (depths && area == 2) yield return CaptureWatch();
                if (!depths && area == 2) yield return CaptureBridges();
                if (area == 2) yield return CaptureHeavyWindup();
                if (depths && area > 1) yield return VerifyCaster(area);
                if (depths && area == 3) yield return CaptureAltar();
                if (!depths && area == 3)
                {
                    yield return CaptureSunkenAltar("01-bridge", new Vector3(0, 1.05f, 40));
                    yield return CaptureSunkenAltar("02-flank", new Vector3(-4, 1.05f, 48));
                    yield return CaptureRecoveryCounter();
                }
                if (visitSupply && area == 2)
                {
                    var hp = player.GetComponent<Health>();
                    hp.SetInvulnerable(false);
                    hp.TakeDamage(40);
                    hp.SetInvulnerable(true);
                }
                foreach (var enemy in FindObjectsByType<PooledEnemy>(FindObjectsSortMode.None)) enemy.GetComponent<Health>().TakeDamage(1000);
                yield return Until(() => spawner.CompletedEncounters == area, "等待敌人死亡与回收");
                yield return null;
                Check(!string.IsNullOrEmpty(routeLabel.text), $"遭遇 {area} 清场后恢复寻路方向");
                if (visitSupply && area == 2)
                    Check(GamePresentation.Current.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text.Contains("西侧小桥通往生命药水")), "受伤清场显示可选药水支路");
                foreach (var pickup in FindObjectsByType<PickupItem>(FindObjectsSortMode.None).Where(p => p.Item.ItemId == "abyss_shard").ToArray())
                {
                    if (pickup == null) continue;
                    bundledShard = pickup.Item; Place(pickup.transform.position);
                    yield return new WaitForSeconds(.2f);
                }
                Check(spawner.CompletedEncounters == area, "真实 Addressables 遭遇完成：" + area);
                if (visitSupply && area == 2) yield return VisitWatchSupply(inventory);
            }
            Check(!Application.isEditor && bundledShard != null && bundledShard != sceneShard && bundledShard.ItemId == sceneShard.ItemId, "独立包真实复现：场景和 AssetBundle 各有一份同 ID 碎片实例");
            Check(inventory.GetTotalQuantity(bundledShard) == total && mission.CarriedShards == total, $"拾取 {total} 枚实际掉落后背包与任务一致");
            Place(mission.Core.position + Vector3.up);
            if (!depths) yield return CaptureSunkenAltar("03-core", mission.Core.position + Vector3.up + Vector3.back);
            const string walkOption = "--return-walk=";
            string walk = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(walkOption, StringComparison.Ordinal));
            if (depths && walk != null)
            {
                yield return WalkReturn(mission, walk.Substring(walkOption.Length));
            }
            else
            {
                Check(mission.TryInteract() && mission.HasCore, "清除三处遭遇后能取回核心");
                if (depths) yield return VerifyReturnGuide(mission);
                if (!depths) yield return CaptureSunkenAltar("04-return", new Vector3(0, 1.05f, 41));
                Place(mission.Beacon.position + Vector3.up);
                Check(mission.Detail.Contains($"{total} / {level.Shards}") && mission.InteractionHint.Contains($"投入 {level.Shards}"), "任务与交互提示认可碎片足够");
                Check(mission.TryInteract() && mission.BeaconLit && mission.DepositedShards == level.Shards && mission.CarriedShards == total-level.Shards && inventory.GetTotalQuantity(bundledShard) == total-level.Shards, $"信标扣除 {level.Shards} 枚，余量 {total-level.Shards} 枚");
                Check(mission.TryInteract() && mission.Extracted, "再次交互可以完成撤离");
            }
            yield return Until(() => GamePresentation.Current.State == GamePresentation.ScreenState.Result, "等待胜利结算");
            Check(MatchResult.HasEnded, "独立运行包显示胜利结算");
            GamePresentation.Current.Restart();
            yield return Until(() => GamePresentation.Current != null && GamePresentation.Current.State == GamePresentation.ScreenState.Playing, "等待重开");
            player = FindFirstObjectByType<PlayerMovement>();
            Check(LevelCatalog.Current.Id == level.Id && !MatchResult.HasEnded && player.GetComponent<Health>().CurrentHealth == 100 && player.GetComponent<PlayerInventory>().GetTotalQuantity(sceneShard) == 0, "重开当前关恢复生命和空背包");
            Check(MatchesWeapon(player.GetComponent<PlayerMeleeAttack>(), expectedWeapon), "胜利重开沿用本局武器");
            player.GetComponent<Health>().TakeDamage(1000);
            yield return Until(() => GamePresentation.Current.State == GamePresentation.ScreenState.Result, "等待死亡结算");
            Check(MatchResult.HasEnded, "当前关支持死亡结算");
            GamePresentation.Current.Restart();
            yield return Until(() => GamePresentation.Current != null && GamePresentation.Current.State == GamePresentation.ScreenState.Playing, "等待死亡后重开");
            Check(LevelCatalog.Current.Id == level.Id && !FindFirstObjectByType<PlayerMovement>().GetComponent<Health>().IsDead && !MatchResult.HasEnded, "死亡后仍重开当前关");
            Check(MatchesWeapon(FindFirstObjectByType<PlayerMeleeAttack>(), expectedWeapon), "失败重开沿用本局武器");
            GamePresentation.Current.ReturnToMenu();
            yield return Until(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu", "等待返回菜单");
            Check(Time.timeScale == 1, "返回菜单恢复时间状态");
        }
        private IEnumerator VerifyReturnGuide(AbyssExpedition mission)
        {
            const string option = "--return-guide-capture=";
            string capture = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(option, StringComparison.Ordinal))?.Substring(option.Length);
            if (capture != null) Directory.CreateDirectory(capture);
            var label = GamePresentation.Current.GetComponentsInChildren<UnityEngine.UI.Text>().Single(t => t.name == "Destination");
            yield return Probe(new Vector3(-4, 3.3f, 44), "向左", "01-upper-bridge");
            yield return Probe(new Vector3(-8, 1.8f, 14), "向右", "02-lower-bridge");
            yield return Probe(mission.Beacon.position + Vector3.up, "已抵达归途信标", "03-beacon");

            IEnumerator Probe(Vector3 point, string expected, string name)
            {
                Place(point);
                yield return new WaitForSeconds(.65f);
                Check(label.text.Contains(expected), $"取核回程 {name}：{label.text}");
                if (expected != "已抵达归途信标")
                {
                    string[] parts = label.text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Check(parts.Length >= 2 && int.TryParse(parts[parts.Length - 2], out int meters) &&
                        meters > Vector3.Distance(point, mission.Beacon.position) + 1, "显示沿桥剩余路程");
                }
                if (capture == null) yield break;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(capture, name + ".png"));
                yield return new WaitForSecondsRealtime(.3f);
            }
        }

        private IEnumerator CaptureHeavyWindup()
        {
            const string prefix = "--heavy-capture=";
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal));
            if (option == null) yield break;
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Heavy capture requires a graphical Player");
            string directory = Path.GetFullPath(option.Substring(prefix.Length));
            var brute = FindObjectsByType<EnemyController>(FindObjectsSortMode.None).First(c => c.IsHeavy);
            var sector = brute.GetComponentInChildren<HeavyAttackPresentation>().transform.Find("Heavy attack sector").GetComponent<MeshRenderer>();
            var block = new MaterialPropertyBlock();
            yield return new WaitForSecondsRealtime(4);
            foreach (float progress in new[] { .25f, .82f })
            {
                yield return Until(() => brute.CurrentState == EnemyState.Windup &&
                    Mathf.Abs(brute.PhaseElapsed / brute.HeavyAttack.windup - progress) < .065f, "等待重锤蓄力取景");
                yield return new WaitForEndOfFrame();
                sector.GetPropertyBlock(block);
                Check(sector.enabled && sector.sharedMaterial.HasProperty("_ChargeRadius") &&
                    Mathf.Abs(block.GetFloat("_ChargeRadius") - brute.HeavyAttack.range * brute.PhaseElapsed / brute.HeavyAttack.windup) < .01f,
                    "打包预警内弧与蓄力时钟同步 " + progress);
                WriteFrame(directory, progress < .5f ? "01-tracking" : "02-locked");
            }
        }

        private IEnumerator CaptureSunkenAltar(string view, Vector3 position)
        {
            const string prefix = "--sunken-altar-capture=";
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal));
            if (option == null) yield break;
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Sunken altar capture requires a graphical Player");
            var art = GameObject.Find("沉没祭坛祈火壁");
            if (art == null) throw new InvalidOperationException("Missing sunken altar architecture");
            if (view == "04-return")
            {
                var controller = player.GetComponent<CharacterController>();
                player.transform.rotation = Quaternion.LookRotation(Vector3.back);
                float deadline = Time.realtimeSinceStartup + 8;
                while (player.transform.position.z > position.z && Time.realtimeSinceStartup < deadline)
                {
                    controller.Move(new Vector3(0, -2, -3) * Time.deltaTime);
                    yield return null;
                }
                Check(Mathf.Abs(player.transform.position.z - position.z) < .35f && player.transform.position.y > 0,
                    "携核从承座沿主路走回桥口");
            }
            else Place(position);
            yield return new WaitForSecondsRealtime(4.3f);
            var delta = player.transform.position + Vector3.up * .35f - Camera.main.transform.position;
            var ray = new Ray(Camera.main.transform.position, delta.normalized);
            var renderers = art.GetComponentsInChildren<MeshRenderer>();
            Check(renderers.Length == 11 && renderers.All(r => r.enabled && r.sharedMaterial != null &&
                r.sharedMaterial.shader.name != "Hidden/InternalErrorShader" &&
                !(r.bounds.IntersectRay(ray, out float d) && d < delta.magnitude)), "祭坛材质与猎手视线 " + view);
            yield return new WaitForEndOfFrame();
            WriteFrame(Path.GetFullPath(option.Substring(prefix.Length)), view);
        }

        private static void WriteFrame(string directory, string view)
        {
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, view + ".png"), frame.EncodeToPNG());
            }
            finally { Destroy(frame); }
        }

        private IEnumerator CaptureBridges()
        {
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--bridge-capture=", StringComparison.Ordinal));
            if (option == null) yield break;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Application.isBatchMode)
                throw new InvalidOperationException("Bridge capture requires graphical Player");
            string directory = Path.GetFullPath(option.Substring("--bridge-capture=".Length));
            Directory.CreateDirectory(directory);
            var art = GameObject.Find("沉没桥廊模块建筑");
            if (art == null) throw new InvalidOperationException("Missing bridge architecture");
            var renderers = art.GetComponentsInChildren<MeshRenderer>();
            Check(art.GetComponentsInChildren<Collider>(true).Length == 0, "桥廊显示层无新增碰撞");
            Check(renderers.Length == 18 && renderers.All(r => r.sharedMaterial != null && r.sharedMaterial.shader.name != "Hidden/InternalErrorShader"), "桥廊十八处模型与打包材质有效");
            var original = player.transform.position;
            var controller = player.GetComponent<CharacterController>();
            foreach (float z in new[] { 12f, 36f })
            foreach (int direction in new[] { -1, 1 })
            {
                // Place the capsule bottom above the floor; its origin is at the capsule centre.
                Place(new Vector3(0, controller.height * .5f - controller.center.y + .05f, z - direction * 6));
                yield return null;
                float travelled = 0;
                bool stayedOnDeck = true;
                while (travelled < 12)
                {
                    float step = Mathf.Min(3 * Time.deltaTime, 12 - travelled);
                    controller.Move(new Vector3(0, -2 * Time.deltaTime, direction * step));
                    travelled += step;
                    stayedOnDeck &= player.transform.position.y > -.3f;
                    yield return null;
                }
                Check(stayedOnDeck && Mathf.Abs(player.transform.position.z - (z + direction * 6)) < .35f, "桥面实际碰撞往返通行：" + z + "/" + direction + "，终点 " + player.transform.position);
            }
            var points = new[] { new Vector3(0, .1f, 9), new Vector3(1.7f, .1f, 14), new Vector3(0, .1f, 33), new Vector3(-1.7f, .1f, 38) };
            for (int i = 0; i < points.Length; i++)
            {
                Place(points[i]); yield return new WaitForSecondsRealtime(1.5f);
                var delta = player.transform.position + Vector3.up * .35f - Camera.main.transform.position;
                var ray = new Ray(Camera.main.transform.position, delta.normalized);
                Check(renderers.Where(r => r.enabled && r.bounds.IntersectRay(ray, out float d) && d < delta.magnitude)
                    .All(r => r.GetComponent<AbyssOccluder>()?.IsFaded == true), "桥廊保持猎手视线：" + i);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "0" + (i + 1) + "-bridge.png"));
                yield return new WaitForSecondsRealtime(.5f);
            }
            Place(original);
        }

        private IEnumerator CaptureFoyer()
        {
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--foyer-capture=", StringComparison.Ordinal));
            if (option == null) yield break;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Application.isBatchMode)
                throw new InvalidOperationException("Foyer capture requires graphical Player");
            string directory = Path.GetFullPath(option.Substring("--foyer-capture=".Length));
            Directory.CreateDirectory(directory);
            var foyer = GameObject.Find("沉没前厅模块建筑");
            if (foyer == null) throw new InvalidOperationException("Missing foyer architecture");
            var renderers = foyer.GetComponentsInChildren<MeshRenderer>();
            Check(foyer.GetComponentsInChildren<Collider>(true).Length == 0, "前厅新建筑无新增碰撞");
            Check(renderers.All(r => r.sharedMaterial != null && r.sharedMaterial.shader.name != "Hidden/InternalErrorShader"), "前厅打包材质有效");
            var original = player.transform.position;
            var points = new[] { new Vector3(0, .1f, -7.5f), new Vector3(0, .1f, -2), new Vector3(-5, .1f, 3), new Vector3(0, .1f, 7) };
            for (int i = 0; i < points.Length; i++)
            {
                Place(points[i]); yield return new WaitForSecondsRealtime(1.5f);
                var delta = player.transform.position + Vector3.up * .35f - Camera.main.transform.position;
                var ray = new Ray(Camera.main.transform.position, delta.normalized);
                Check(renderers.Where(r => r.enabled && r.bounds.IntersectRay(ray, out float d) && d < delta.magnitude)
                    .All(r => r.GetComponent<AbyssOccluder>()?.IsFaded == true), "前厅建筑保持猎手可见：" + i);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "0" + (i + 1) + "-foyer.png"));
                yield return new WaitForSecondsRealtime(.5f);
            }
            Place(original);
        }

        private IEnumerator CaptureAltar()
        {
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--altar-capture=", StringComparison.Ordinal));
            if (option == null) yield break;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Application.isBatchMode)
                throw new InvalidOperationException("Altar capture requires a graphical, non-batch Player");
            string directory = Path.GetFullPath(option.Substring("--altar-capture=".Length));
            Directory.CreateDirectory(directory);
            var altar = GameObject.Find("KayKit核心圣龛");
            if (altar == null) throw new InvalidOperationException("Missing altar architecture");
            var renderers = altar.GetComponentsInChildren<MeshRenderer>();
            Check(altar.GetComponentsInChildren<Collider>(true).Length == 0, "独立包圣龛无新增碰撞");
            Check(renderers.All(r => r.sharedMaterials.All(m => m != null && m.shader.name != "Hidden/InternalErrorShader")), "独立包圣龛材质与 Shader 有效");
            var original = player.transform.position;
            var points = new[] { new Vector3(8, 3, 52), new Vector3(8, 3, 62), new Vector3(8, 3, 71), new Vector3(-2, 3, 62) };
            for (int i = 0; i < points.Length; i++)
            {
                Place(points[i] + Vector3.up * .1f); yield return new WaitForSecondsRealtime(1.5f);
                var delta = player.transform.position + Vector3.up * .35f - Camera.main.transform.position;
                var ray = new Ray(Camera.main.transform.position, delta.normalized);
                Check(renderers.Where(r => r.enabled && r.bounds.IntersectRay(ray, out float d) && d < delta.magnitude)
                    .All(r => r.GetComponent<AbyssOccluder>()?.IsFaded == true), "独立包圣龛保持猎手可见：" + i);
                var crystalFaders = FindObjectsByType<AbyssOccluder>(FindObjectsSortMode.None)
                    .Where(f => f.name == "悬浮裂隙核心" || f.name == "祭坛环绕晶体" || (f.name == "边缘晶簇" && f.transform.position.z > 47)).ToArray();
                Check(crystalFaders.Length == 13, "祭坛十三处晶体具有独立淡化");
                if (i == 1)
                {
                    Check(crystalFaders.Any(f => f.name == "悬浮裂隙核心" && f.IsFaded), "贴近核心时核心淡化");
                    GamePresentation.Current.ShowPause();
                    yield return new WaitForSecondsRealtime(.4f);
                    Check(Time.timeScale == 0 && crystalFaders.Any(f => f.name == "悬浮裂隙核心" && f.IsFaded), "暂停时核心保持淡化");
                    GamePresentation.Current.Resume();
                }
                if (i == 2) Check(crystalFaders.Where(f => f.name == "悬浮裂隙核心").All(f => !f.IsFaded), "离开核心后恢复显示");
                if (i == 3) Check(crystalFaders.Any(f => f.name == "边缘晶簇" && f.IsFaded), "西侧晶簇遮挡时独立淡化");
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "0" + (i + 1) + "-altar.png")); yield return new WaitForSecondsRealtime(.5f);
            }
            Place(original);
        }

        private IEnumerator CaptureWatch()
        {
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--watch-capture=", StringComparison.Ordinal));
            if (option == null) yield break;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Application.isBatchMode)
                throw new InvalidOperationException("Watch visual capture requires a graphical, non-batch Player");
            string directory = Path.GetFullPath(option.Substring("--watch-capture=".Length));
            Directory.CreateDirectory(directory);
            var watch = GameObject.Find("KayKit守望遗迹");
            if (watch == null) throw new InvalidOperationException("Missing watch architecture");
            var renderers = watch.GetComponentsInChildren<MeshRenderer>();
            Check(watch.GetComponentsInChildren<Collider>(true).Length == 0, "独立包守望建筑无新增碰撞");
            Check(renderers.All(r => r.sharedMaterials.All(m => m != null && m.shader.name != "Hidden/InternalErrorShader")), "独立包守望材质与 Shader 有效");
            var inscription = renderers.FirstOrDefault(r => r.name == "下沉封印铭文");
            Check(inscription != null && inscription.sharedMaterial.IsKeywordEnabled("_EMISSION"), "独立包封印铭文保留 URP 发光变体");
            var original = player.transform.position;
            var points = new[] { new Vector3(-16, 1.5f, 30), new Vector3(-26, 1.5f, 30), new Vector3(-9, 1.5f, 30), new Vector3(-30.5f, 1.5f, 35), new Vector3(-30.5f, 1.5f, 25) };
            var names = new[] { "01-arena", "02-well", "03-arcade", "04-well-north", "05-well-south" };
            for (int i = 0; i < points.Length; i++)
            {
                Place(points[i] + Vector3.up * .1f); yield return new WaitForSecondsRealtime(1.5f);
                var camera = Camera.main;
                var delta = player.transform.position + Vector3.up * .35f - camera.transform.position;
                var ray = new Ray(camera.transform.position, delta);
                Check(renderers.Where(r => r.enabled && r.bounds.IntersectRay(ray, out float distance) && distance < delta.magnitude)
                    .All(r => r.GetComponent<AbyssOccluder>()?.IsFaded == true), "独立包守望建筑不遮住猎手：" + names[i]);
                var well = watch.transform.Find("裂隙封印井");
                if (i == 3) Check(well != null && well.GetComponentsInChildren<AbyssOccluder>().Any(f => f.IsFaded), "独立包封印井在北侧桥遮挡时淡化");
                if (i == 4) Check(well != null && well.GetComponentsInChildren<AbyssOccluder>().All(f => !f.IsFaded), "独立包封印井在南侧桥恢复");
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, names[i] + ".png")); yield return new WaitForSecondsRealtime(.5f);
            }
            Place(original);
        }
        private void VerifyGatePassage()
        {
            var controller = player.GetComponent<CharacterController>();
            var original = player.transform.position;
            float standingY = controller.height * .5f - controller.center.y + .06f;
            foreach (int direction in new[] { 1, -1 })
            {
                string approach = direction == 1 ? "去程" : "回程";
                foreach (float x in new[] { -4.4f, -2.25f, 0, 2.25f, 4.4f })
                {
                    bool pier = Mathf.Abs(x) > 3;
                    var start = new Vector3(x, standingY, 7.3f - direction * 1.5f);
                    var travel = Vector3.forward * direction * 3;
                    Check(Walk(start, travel, pier), $"入口{approach} x={x:F2} " + (pier ? "拱墩挡住角色" : "门洞可通行"));
                    var navStart = new Vector3(x, 0, 7.3f - direction * 1.5f);
                    var navEnd = navStart + travel;
                    if (direction < 0 && pier)
                    {
                        // The bridge is narrower than the gate. Approach the pier diagonally from its walkable deck.
                        navStart = new Vector3(Mathf.Sign(x) * 2, 0, 10);
                        navEnd = new Vector3(Mathf.Sign(x) * 5.7f, 0, 5.5f);
                    }
                    bool onMesh = NavMesh.SamplePosition(navStart, out var sample, .4f, NavMesh.AllAreas);
                    bool blocked = onMesh && NavMesh.Raycast(sample.position, navEnd, out _, NavMesh.AllAreas);
                    Check(onMesh && blocked == pier, $"入口{approach} x={x:F2} 导航与石墩一致 (起点有效={onMesh}, 阻挡={blocked})");
                }
            }
            foreach (int side in new[] { -1, 1 })
            {
                Check(Walk(new Vector3(side * 1.5f, standingY, 7.3f), Vector3.right * (side * 3), true),
                    side < 0 ? "从门内横移被左墩挡住" : "从门内横移被右墩挡住");
                Check(Walk(new Vector3(side * 7.7f, standingY, 4.8f), Vector3.forward * 4, true),
                    side < 0 ? "左侧独立石柱挡住角色" : "右侧独立石柱挡住角色");
            }
            Place(original);

            bool Walk(Vector3 start, Vector3 travel, bool expectContact)
            {
                Place(start);
                Physics.SyncTransforms();
                var flags = CollisionFlags.None;
                for (int i = 0; i < 12; i++) flags |= controller.Move(travel / 12);
                float distance = Vector3.Dot(player.transform.position - start, travel.normalized);
                return expectContact
                    ? (flags & CollisionFlags.Sides) != 0 && distance > .1f && distance < travel.magnitude * .5f
                    : distance > travel.magnitude - .15f;
            }
        }

        private IEnumerator CaptureEntrance()
        {
            string option = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--entrance-capture=", StringComparison.Ordinal));
            if (option == null) yield break;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Application.isBatchMode)
                throw new InvalidOperationException("Entrance visual capture requires a graphical, non-batch Player");
            string directory = Path.GetFullPath(option.Substring("--entrance-capture=".Length));
            Directory.CreateDirectory(directory);
            var entrance = GameObject.Find("KayKit入口示范");
            if (entrance == null) throw new InvalidOperationException("Missing entrance architecture");
            var faders = entrance.GetComponentsInChildren<AbyssOccluder>();
            Place(new Vector3(0, 1, -5)); yield return new WaitForSecondsRealtime(1.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "01-entry.png")); yield return new WaitForSecondsRealtime(.5f);
            Place(new Vector3(0, 1, 9)); yield return new WaitForSecondsRealtime(1.5f);
            Check(faders.Any(f => f.IsFaded), "独立包入口建筑遮挡时淡化");
            Check(entrance.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterials.All(m => m != null && m.shader.name != "Hidden/InternalErrorShader")), "独立包入口材质与 Shader 有效");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "02-gate-faded.png")); yield return new WaitForSecondsRealtime(.5f);
            var gate = entrance.transform.Find("wall_doorway").GetComponentInChildren<AbyssOccluder>();
            var controller = player.GetComponent<CharacterController>();
            Place(new Vector3(3.15f, controller.height * .5f - controller.center.y + .06f, 9.3f));
            yield return new WaitForSecondsRealtime(1.5f);
            float before = player.transform.position.z;
            var collision = controller.Move(Vector3.back * 2.4f);
            float after = player.transform.position.z;
            Check(gate.IsFaded && (collision & CollisionFlags.Sides) != 0 && after < before && after > 7.85f,
                $"拱门虚化时朝右墩快速移动仍被挡住 (淡化={gate.IsFaded}, 接触={collision}, z={before:F2}→{after:F2})");
            Place(new Vector3(0, 1, -4)); yield return new WaitForSecondsRealtime(1.5f);
            Check(faders.Length > 0 && faders.All(f => !f.IsFaded), "独立包入口建筑离开遮挡后恢复");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "03-gate-restored.png")); yield return new WaitForSecondsRealtime(.5f);
        }
    }
}
#endif

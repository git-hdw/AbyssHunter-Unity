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

namespace AbyssHunter.Inventory
{
    // Opt-in development-player regression. Uses real bundled enemy drops and controlled kills.
    // Compiled out of non-development releases; no test inventory or cheats enter the release.
    public sealed class InventoryBundleVerification : MonoBehaviour
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
            int total = depths ? 9 : 6;
            SceneFlow.LoadLevel(level.Id); yield return null;
            yield return Until(() => GamePresentation.Current != null && GamePresentation.Current.State == GamePresentation.ScreenState.Playing, "等待开场结束");
            player = FindFirstObjectByType<PlayerMovement>(); player.GetComponent<Health>().SetInvulnerable(true);
            GamePresentation.Current.ShowPause();
            Check(Time.timeScale == 0 && GamePresentation.BlocksGameplay, "暂停阻断玩法输入");
            GamePresentation.Current.Resume();
            GamePresentation.Current.ShowInventory();
            Check(Time.timeScale == 0 && GamePresentation.Current.State == GamePresentation.ScreenState.Inventory, "背包打开暂停游戏");
            GamePresentation.Current.Resume();
            var inventory = player.GetComponent<PlayerInventory>();
            var mission = FindFirstObjectByType<AbyssExpedition>(); var spawner = FindFirstObjectByType<EnemyWaveSpawner>();
            var sceneShard = (ItemData)typeof(AbyssExpedition).GetField("shard", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mission);
            CheckIdentity(sceneShard);
            var navigation = new UnityEngine.AI.NavMeshPath();
            Check(UnityEngine.AI.NavMesh.CalculatePath(mission.Beacon.position, mission.Core.position, UnityEngine.AI.NavMesh.AllAreas, navigation) && navigation.status == UnityEngine.AI.NavMeshPathStatus.PathComplete, "信标至核心导航连通");
            if (depths)
            {
                UnityEngine.AI.NavMesh.SamplePosition(new Vector3(-38,1.5f,30), out var side, 2, UnityEngine.AI.NavMesh.AllAreas);
                Check(UnityEngine.AI.NavMesh.CalculatePath(mission.Beacon.position, side.position, UnityEngine.AI.NavMesh.AllAreas, navigation) && navigation.status == UnityEngine.AI.NavMeshPathStatus.PathComplete, "侧廊补给可达");
            }
            ItemData bundledShard = null;
            for (int area = 1; area <= 3; area++)
            {
                Place(spawner.NextApproach.position + Vector3.up);
                int expected = depths ? (area == 1 ? 3 : area == 2 ? 2 : 4) : (area == 1 ? 2 : area == 2 ? 1 : 3);
                yield return Until(() => spawner.CurrentWave == area && spawner.ActiveEnemies == expected, "等待遭遇 " + area);
                foreach (var enemy in FindObjectsByType<PooledEnemy>(FindObjectsSortMode.None)) enemy.GetComponent<Health>().TakeDamage(1000);
                yield return Until(() => spawner.CompletedEncounters == area, "等待敌人死亡与回收");
                foreach (var pickup in FindObjectsByType<PickupItem>(FindObjectsSortMode.None).Where(p => p.Item.ItemId == "abyss_shard").ToArray())
                {
                    if (pickup == null) continue;
                    bundledShard = pickup.Item; Place(pickup.transform.position);
                    yield return new WaitForSeconds(.2f);
                }
                Check(spawner.CompletedEncounters == area, "真实 Addressables 遭遇完成：" + area);
            }
            Check(!Application.isEditor && bundledShard != null && bundledShard != sceneShard && bundledShard.ItemId == sceneShard.ItemId, "独立包真实复现：场景和 AssetBundle 各有一份同 ID 碎片实例");
            Check(inventory.GetTotalQuantity(bundledShard) == total && mission.CarriedShards == total, $"拾取 {total} 枚实际掉落后背包与任务一致");
            Place(mission.Core.position + Vector3.up);
            Check(mission.TryInteract() && mission.HasCore, "清除三处遭遇后能取回核心");
            Place(mission.Beacon.position + Vector3.up);
            Check(mission.Detail.Contains($"{total} / {level.Shards}") && mission.InteractionHint.Contains($"投入 {level.Shards}"), "任务与交互提示认可碎片足够");
            Check(mission.TryInteract() && mission.BeaconLit && mission.DepositedShards == level.Shards && mission.CarriedShards == total-level.Shards && inventory.GetTotalQuantity(bundledShard) == total-level.Shards, $"信标扣除 {level.Shards} 枚，余量 {total-level.Shards} 枚");
            Check(mission.TryInteract() && mission.Extracted, "再次交互可以完成撤离");
            yield return Until(() => GamePresentation.Current.State == GamePresentation.ScreenState.Result, "等待胜利结算");
            Check(MatchResult.HasEnded, "独立运行包显示胜利结算");
            GamePresentation.Current.Restart();
            yield return Until(() => GamePresentation.Current != null && GamePresentation.Current.State == GamePresentation.ScreenState.Playing, "等待重开");
            player = FindFirstObjectByType<PlayerMovement>();
            Check(LevelCatalog.Current.Id == level.Id && !MatchResult.HasEnded && player.GetComponent<Health>().CurrentHealth == 100 && player.GetComponent<PlayerInventory>().GetTotalQuantity(sceneShard) == 0, "重开当前关恢复生命和空背包");
            player.GetComponent<Health>().TakeDamage(1000);
            yield return Until(() => GamePresentation.Current.State == GamePresentation.ScreenState.Result, "等待死亡结算");
            Check(MatchResult.HasEnded, "当前关支持死亡结算");
            GamePresentation.Current.Restart();
            yield return Until(() => GamePresentation.Current != null && GamePresentation.Current.State == GamePresentation.ScreenState.Playing, "等待死亡后重开");
            Check(LevelCatalog.Current.Id == level.Id && !FindFirstObjectByType<PlayerMovement>().GetComponent<Health>().IsDead && !MatchResult.HasEnded, "死亡后仍重开当前关");
            GamePresentation.Current.ReturnToMenu();
            yield return Until(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu", "等待返回菜单");
            Check(Time.timeScale == 1, "返回菜单恢复时间状态");
        }
    }
}
#endif

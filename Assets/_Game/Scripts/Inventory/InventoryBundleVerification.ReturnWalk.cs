#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using AbyssHunter.UI;
using AbyssHunter.Combat;
using AbyssHunter.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace AbyssHunter.Inventory
{
    public sealed partial class InventoryBundleVerification
    {
        private IEnumerator VisitWatchSupply(PlayerInventory inventory)
        {
            var supply = GameObject.Find("侧廊补给 · 生命药水").GetComponent<PickupItem>();
            var potion = (HealingItemData)supply.Item;
            int before = inventory.GetTotalQuantity(potion);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return WalkAxis(false, 25);
                yield return WalkAxis(true, -38);
                yield return WalkAxis(false, 30);
                yield return Until(() => supply == null, "等待固定药水拾取");
                Check(inventory.GetTotalQuantity(potion) == before + 1, "南侧桥通往固定药水，背包增加一瓶");
                int slot = Enumerable.Range(0, inventory.Slots.Count).First(i => potion.HasSameIdentity(inventory.Slots[i].Item));
                var health = player.GetComponent<Health>();
                int hp = health.CurrentHealth;
                GamePresentation.Current.ShowInventory();
                GamePresentation.Current.GetComponentsInChildren<Button>().Single(b => b.name == "Slot " + slot).onClick.Invoke();
                GamePresentation.Current.Resume();
                Check(health.CurrentHealth == Mathf.Min(health.MaximumHealth, hp + potion.HealingAmount) &&
                    inventory.GetTotalQuantity(potion) == before, "从背包使用药水并恢复生命");
                yield return WalkAxis(false, 35);
                yield return WalkAxis(true, -30.5f);
                Check(Mathf.Abs(player.transform.position.z - 35) < 2.5f && player.transform.position.y > 1.5f,
                    "经过北侧桥跨中：" + player.transform.position);
                yield return WalkAxis(true, -16);
                yield return WalkAxis(false, 35);
                Check(Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z), new Vector2(-16, 35)) < 1 &&
                    player.transform.position.y > 1.5f, "沿北侧桥重新汇入守望台主路：" + player.transform.position);
            }
            finally { InputSystem.RemoveDevice(keyboard); }

            IEnumerator WalkAxis(bool horizontal, float goal)
            {
                float limit = Time.realtimeSinceStartup + 18;
                while (true)
                {
                    float remaining = goal - (horizontal ? player.transform.position.x : player.transform.position.z);
                    if (Mathf.Abs(remaining) < .3f) break;
                    if (Time.realtimeSinceStartup > limit) throw new TimeoutException("Supply walk blocked at " + player.transform.position);
                    Key key = horizontal ? (remaining > 0 ? Key.D : Key.A) : (remaining > 0 ? Key.W : Key.S);
                    keyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                    yield return null;
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
            }
        }

        private IEnumerator WalkReturn(AbyssExpedition mission, string directory)
        {
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Return walk recording requires a graphical Player");
            Directory.CreateDirectory(directory);
            if (Directory.EnumerateFiles(directory).Any()) throw new IOException("Choose an empty recording directory");
            var label = GamePresentation.Current.GetComponentsInChildren<Text>().Single(t => t.name == "Destination");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            int previousRate = Time.captureFramerate;
            int frame = 0;
            const int fps = 24;
            var endOfFrame = new WaitForEndOfFrame();
            using var trace = new StreamWriter(Path.Combine(directory, "route.tsv"));
            trace.WriteLine("frame\tx\ty\tz\tguide");
            try
            {
                Time.captureFramerate = fps;
                yield return Hold(Key.None, fps);
                yield return PressE();
                Check(mission.HasCore, "E 在祭坛取核");
                yield return Hold(Key.None, fps);
                yield return Move(Key.S, 53);
                yield return Observe("祭坛出口直行段", "向后");
                yield return Move(Key.S, 44);
                yield return Move(Key.A, -4);
                yield return Observe("上层横桥", "向左");
                yield return Move(Key.D, 2);
                yield return Observe("走反六米后", "向左");
                yield return Move(Key.A, -16);
                yield return Move(Key.S, 14);
                yield return Move(Key.D, -8);
                yield return Observe("下层横桥", "向右");
                yield return Move(Key.D, 0);
                yield return Move(Key.S, -6);
                yield return Observe("入口拱门后", "已抵达归途信标");
                int shards = mission.CarriedShards;
                yield return PressE();
                Check(mission.BeaconLit && mission.DepositedShards == 6 && mission.CarriedShards == shards - 6, "E 点亮信标并投入六枚碎片");
                yield return Hold(Key.None, fps);
                yield return PressE();
                Check(mission.Extracted, "再次按 E 撤离");
                yield return Hold(Key.None, fps * 2);
            }
            finally
            {
                Time.captureFramerate = previousRate;
                InputSystem.RemoveDevice(keyboard);
            }

            IEnumerator Observe(string place, string expected)
            {
                yield return Hold(Key.None, fps / 2);
                Check(label.text.Contains(expected), place + "：" + label.text);
            }

            IEnumerator Move(Key key, float target)
            {
                bool horizontal = key == Key.A || key == Key.D;
                int sign = key == Key.A || key == Key.S ? -1 : 1;
                int start = frame;
                while ((target - (horizontal ? player.transform.position.x : player.transform.position.z)) * sign > .2f)
                {
                    if (frame - start > fps * 15) throw new TimeoutException($"Return walk blocked at {player.transform.position}, key {key}");
                    yield return Hold(key, 1);
                }
            }

            IEnumerator PressE()
            {
                yield return Hold(Key.E, 3);
                yield return Hold(Key.None, 4);
            }

            IEnumerator Hold(Key key, int frames)
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, key == Key.None ? new KeyboardState() : new KeyboardState(key));
                for (int i = 0; i < frames; i++)
                {
                    yield return endOfFrame;
                    var screen = ScreenCapture.CaptureScreenshotAsTexture();
                    try { File.WriteAllBytes(Path.Combine(directory, $"{frame:D5}.jpg"), screen.EncodeToJPG(88)); }
                    finally { Destroy(screen); }
                    var p = player.transform.position;
                    trace.WriteLine(FormattableString.Invariant($"{frame}\t{p.x:F2}\t{p.y:F2}\t{p.z:F2}\t{label.text}"));
                    frame++;
                    yield return null;
                }
            }
        }
    }
}
#endif

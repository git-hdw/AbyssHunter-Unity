#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.Combat;
using UnityEditor;
using UnityEngine;

namespace AbyssHunter.Character
{
    // Isolated pose/skin review; never instantiated during normal gameplay or in a player build.
    public sealed class CharacterModelVerification : MonoBehaviour
    {
        private readonly List<string> report = new();
        private readonly List<GameObject> models = new();
        private int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SessionState.GetBool("AbyssHunter.ModelTest", false))
            {
                SessionState.EraseBool("AbyssHunter.ModelTest");
                new GameObject("角色模型验证").AddComponent<CharacterModelVerification>();
            }
        }
        private void Check(bool pass, string text)
        {
            report.Add((pass ? "PASS | " : "FAIL | ") + text);
            File.WriteAllLines("Logs/character-model-playtest.txt", report);
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception) { errors++; Check(false, message); } }
        private IEnumerator Start()
        {
            Directory.CreateDirectory("Logs/character-review");
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            string[] names = { "HunterVisual", "SkeletonVisual", "GuardianVisual" };
            for (int i = 0; i < names.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Game/Prefabs/Characters/{names[i]}.prefab");
                var go = Instantiate(prefab, new Vector3(1000 + (i - 1) * 3.2f, 0, 0), Quaternion.identity);
                models.Add(go);
                foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer = 30;
                var animator = go.GetComponent<Animator>();
                Check(animator.isHuman && animator.avatar.isValid, names[i] + "：Humanoid 骨骼有效");
                Check(animator.GetBoneTransform(HumanBodyBones.RightHand) != null, names[i] + "：右手骨骼有效");
                Check(go.GetComponent<AttackAnimationEventRelay>() != null, names[i] + "：伤害动画事件接收器存在");
                Check(go.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => r.bones.All(b => b != null)), names[i] + "：蒙皮无缺失骨骼");
                Check(go.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterials.All(m => m != null && m.shader.name != "Hidden/InternalErrorShader")), names[i] + "：材质无丢失");
            }
            var light = new GameObject("角色检视补光").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 2.2f;
            light.transform.rotation = Quaternion.Euler(35, -35, 0); light.cullingMask = 1 << 30;
            var camera = new GameObject("角色检视镜头").AddComponent<Camera>();
            camera.transform.position = new Vector3(1005, 3.8f, 10);
            camera.transform.LookAt(new Vector3(1000, 1.2f, 0));
            camera.orthographic = true; camera.orthographicSize = 3.1f;
            camera.backgroundColor = new Color(.025f, .045f, .065f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.cullingMask = 1 << 30; camera.enabled = false;
            var rt = new RenderTexture(1600, 900, 24); camera.targetTexture = rt;
            yield return null;
            var idleHands = new Vector3[3];
            foreach (string pose in new[] { "待机", "奔跑", "攻击前摇", "攻击命中", "受击", "死亡", "闪避" })
            {
                for (int i = 0; i < models.Count; i++)
                {
                    var animator = models[i].GetComponent<Animator>();
                    string state = pose switch {
                        "奔跑" => "Running_A", "受击" => "Hit_A", "死亡" => "Death_A",
                        "闪避" when i == 0 => "Dodge_Forward",
                        "攻击前摇" or "攻击命中" => i == 0 ? "Melee_1H_Attack_Chop" : i == 1 ? "Melee_2H_Attack" : "HeavySwing",
                        _ => "Idle_A"
                    };
                    float time = pose == "攻击前摇" ? .22f : pose == "攻击命中" ? (i == 0 ? .59f : .35f) : pose == "死亡" ? .95f : .25f;
                    animator.speed = 1; animator.Play(state, 0, time); animator.Update(0); animator.speed = 0;
                    var hand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                    if (pose == "待机") idleHands[i] = hand;
                    if (pose == "攻击前摇") Check(Vector3.Distance(hand, idleHands[i]) > .08f, names[i] + "：攻击动作驱动手部骨骼");
                    var meshes = models[i].GetComponentsInChildren<SkinnedMeshRenderer>();
                    var bounds = new Bounds(); bool firstVertex = true;
                    foreach (var renderer in meshes)
                    {
                        var baked = new Mesh(); renderer.BakeMesh(baked);
                        foreach (var vertex in baked.vertices)
                        {
                            var point = renderer.transform.TransformPoint(vertex);
                            if (firstVertex) { bounds = new Bounds(point, Vector3.zero); firstVertex = false; }
                            else bounds.Encapsulate(point);
                        }
                        Destroy(baked);
                    }
                    if (pose != "闪避" || i == 0)
                        Check(bounds.size.magnitude < 10 && bounds.size.y > .2f, names[i] + "：" + pose + "实际蒙皮顶点范围正常");
                    if (pose == "待机")
                    {
                        report.Add($"INFO | {names[i]} render bounds {bounds}");
                        foreach (var r in models[i].GetComponentsInChildren<MeshRenderer>())
                            report.Add($"INFO | {r.name}: bounds={r.bounds}, scale={r.transform.lossyScale}, position={r.transform.position}");
                        Check(Mathf.Abs(bounds.min.y) < .15f, names[i] + "：待机脚底接近角色原点");
                        var weapon = models[i].GetComponentsInChildren<Transform>().First(t => t.name == (i == 2 ? "Hammer shaft" : "右手武器"));
                        Check(weapon.parent.name == "handslot.r", names[i] + "：武器固定在右手挂点");
                    }
                }
                yield return null;
                camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = rt;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                File.WriteAllBytes("Logs/character-review/" + pose + ".png", image.EncodeToPNG());
                Destroy(image); RenderTexture.active = previous;
            }
            Check(errors == 0, "模型检视无运行异常");
            report.Add("DONE"); File.WriteAllLines("Logs/character-model-playtest.txt", report);
            camera.targetTexture = null; rt.Release(); Destroy(rt);
            Application.logMessageReceived -= OnLog;
            EditorApplication.isPlaying = false;
        }
    }
}
#endif

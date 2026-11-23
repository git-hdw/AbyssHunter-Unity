using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    // Owns only the middle arena's display. Entrance, bridges, navigation and sanctum remain untouched.
    public static partial class KayKitWatchSetup
    {
        private const string Folder = "Assets/_Game/Art/KayKitWatch";
        private const string Prefabs = "Assets/_Game/Art/KayKitEntrance";
        private const string RootName = "KayKit守望遗迹";

        [MenuItem("Tools/深渊猎手/环境/精修第二关守望战区 %&#j")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Logs");
            File.Copy("Assets/Scenes/AbyssDepths.unity", "Logs/AbyssDepths-before-watch-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AbyssDepths.unity");
            var root = Object.FindFirstObjectByType<AbyssExpedition>().transform;
            string physics = PhysicsSnapshot(), gameplay = GameplaySnapshot(), siblings = SiblingSnapshot(root);
            ApplyToScene(root);
            VerifyUnchanged(root, physics, gameplay, siblings);
            ApplyToScene(root);
            VerifyUnchanged(root, physics, gameplay, siblings);
            if (root.GetComponentsInChildren<Transform>(true).Count(t => t.name == RootName) != 1) throw new InvalidOperationException("Duplicate watch art root");
            FloatingRiftMapBuilder.Verify();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            var art = root.Find(RootName);
            File.WriteAllLines("Logs/watch-art-verification.txt", new[] {
                "PASS | Original colliders and transforms unchanged",
                "PASS | Original gameplay and entrance/sanctum display unchanged",
                "PASS | Repeated generation: one middle display root, no colliders",
                "PASS | Main and supply navigation complete",
                "INFO | Active renderers: " + art.GetComponentsInChildren<MeshRenderer>().Count(r => r.enabled),
                "INFO | Fading renderers: " + art.GetComponentsInChildren<AbyssOccluder>().Length });
            Debug.Log("Watch arena art saved; physics, gameplay, entrance and sanctum preserved.");
        }

        public static void ApplyToScene(Transform root)
        {
            if (root.gameObject.scene.name != "AbyssDepths") throw new InvalidOperationException("Watch art requires AbyssDepths");
            if (!KayKitEntranceSetup.Available) return;
            var template = AssetDatabase.LoadAssetAtPath<Material>(Prefabs + "/DungeonURP.mat");
            var beddingTemplate = AssetDatabase.LoadAssetAtPath<Material>(Prefabs + "/入口石缝底层.mat");
            if (template == null || beddingTemplate == null) throw new InvalidOperationException("Generate the existing KayKit entrance assets first");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var previous = root.Find(RootName); if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var art = new GameObject(RootName).transform; art.SetParent(root, false);
            var stone = Material("守望冷石", template, new Color(.47f, .54f, .63f));
            var paving = Material("守望旧铺石", template, new Color(.42f, .49f, .58f));
            var worn = Material("守望暗铺石", template, new Color(.40f, .47f, .56f));
            var bedding = Material("守望石缝底层", beddingTemplate, new Color(.17f, .215f, .28f));
            root.GetComponentsInChildren<MeshRenderer>().First(r => r.name == "双层守望台").sharedMaterial = bedding;
            Paving(art, paving, worn);

            // A broken arcade stands on the outer eastern rim, not in either bridge or the fighting floor.
            Module(art, "wall_doorway", new Vector3(-3.35f, 1.5f, 30.4f), new Vector3(8.5f, 4.6f, .85f), 90, stone);
            Module(art, "pillar_decorated", new Vector3(-3.4f, -2.1f, 25.5f), new Vector3(1.4f, 7.2f, 1.4f), 15, stone);
            Module(art, "pillar_decorated", new Vector3(-3.4f, -2.1f, 35.2f), new Vector3(1.4f, 9.2f, 1.4f), -8, stone);
            foreach (float z in new[] { 25.5f, 35.2f })
                Module(art, "column", new Vector3(-3.4f, 1.5f, z), new Vector3(1.8f, .42f, 1.8f), 0, stone);

            Well(art, template);
            RiftFloorSetup.ApplyToScene(root);
            if (art.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Watch display gained collision");
            AssetDatabase.SaveAssets();
        }

        private static void Paving(Transform art, Material paving, Material worn)
        {
            var filters = new List<MeshFilter>();
            for (int z = -4; z <= 4; z++) for (int x = -5; x <= 5; x++)
            {
                int hash = (x + 7) * 19 + (z + 7) * 31;
                float edge = x * x / 34f + z * z / 23f;
                if (edge > 1.26f || (edge > .88f && hash % 4 == 0)) continue;
                string model = edge > .9f && hash % 7 == 0 ? "floor_tile_small_broken_A" : "floor_tile_small";
                var go = KayKitEntranceSetup.Place(art, model,
                    new Vector3(-16 + x * 1.78f + (z % 2 == 0 ? -.24f : .24f), 1.508f, 30 + z * 1.78f), new Vector3(1.72f, .038f, 1.72f));
                go.rotation = Quaternion.Euler(0, hash % 4 * 90 + (edge > .8f ? (hash % 5 - 2) * .6f : 0), 0);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                {
                    r.sharedMaterial = hash % 5 == 0 ? worn : paving; r.shadowCastingMode = ShadowCastingMode.Off;
                    filters.Add(r.GetComponent<MeshFilter>());
                }
            }
            int index = 0;
            foreach (var group in filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = art.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray());
                mesh.RecalculateBounds();
                string name = "守望错缝铺石_" + index++;
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(art, false);
                go.GetComponent<MeshFilter>().sharedMesh = SaveMesh(name, mesh);
                go.GetComponent<MeshRenderer>().sharedMaterial = group.Key; go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                foreach (var f in group) f.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        private static void Module(Transform parent, string model, Vector3 position, Vector3 size, float yaw, Material material)
        {
            var go = KayKitEntranceSetup.Place(parent, model, position, size); go.rotation = Quaternion.Euler(0, yaw, 0);
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterial = material;
        }

        private static Material Material(string name, Material template, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null) { result = new Material(template); AssetDatabase.CreateAsset(result, path); }
            else EditorUtility.CopySerialized(template, result);
            result.name = name; result.SetColor("_BaseColor", color); EditorUtility.SetDirty(result); return result;
        }
        private static Mesh SaveMesh(string name, Mesh mesh)
        {
            mesh.name = name; string path = Folder + "/" + name + ".asset";
            var result = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (result == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, result); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(result); return result;
        }
        private static string PhysicsSnapshot() => string.Join("\n", Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Select(c => c.GetInstanceID() + EditorJsonUtility.ToJson(c) + EditorJsonUtility.ToJson(c.transform)));
        private static string GameplaySnapshot() => string.Join("\n", Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Where(c => c != null && !(c is AbyssOccluder && c.GetComponentsInParent<Transform>(true).Any(t => t.name == RootName))).Select(c => c.GetInstanceID() + EditorJsonUtility.ToJson(c)));
        private static string SiblingSnapshot(Transform root)
        {
            var entrance = root.Find("KayKit入口示范");
            return string.Join("\n", root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.transform.IsChildOf(entrance) || r.bounds.center.z > 47).Select(r => r.GetInstanceID() + EditorJsonUtility.ToJson(r) + EditorJsonUtility.ToJson(r.transform) + EditorJsonUtility.ToJson(r.GetComponent<MeshFilter>())));
        }
        private static void VerifyUnchanged(Transform root, string physics, string gameplay, string siblings)
        {
            if (physics != PhysicsSnapshot() || gameplay != GameplaySnapshot() || siblings != SiblingSnapshot(root)) throw new InvalidOperationException("Watch art changed protected gameplay or another region");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    // The entrance owns its gate footings separately from the fading display models.
    public static partial class KayKitEntranceSetup
    {
        private const string Source = "Assets/ThirdParty/KayKitDungeon";
        private const string Output = "Assets/_Game/Art/KayKitEntrance";
        private static readonly Vector3 GateBase = new(0, .02f, 7.3f);
        private static readonly Vector3 GateSize = new(11.8f, 4.9f, 1.15f);
        private static Vector3 PostBase(int side) => new(side * 7.7f, .02f, 7);
        private static Vector3 PostSize(int side) => new(2, side < 0 ? 5.8f : 4.2f, 2);
        public static bool Available => File.Exists(Source + "/pillar_decorated.fbx");
        private static readonly string[] Models = { "pillar_decorated", "column", "floor_tile_small_decorated", "floor_tile_small", "floor_tile_small_broken_A", "wall_half", "wall_doorway" };

        [MenuItem("Tools/深渊猎手/环境/接入 KayKit 入口示范 %#k")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Logs");
            if (!File.Exists("Logs/AbyssDepths-before-kaykit.unity"))
                File.Copy("Assets/Scenes/AbyssDepths.unity", "Logs/AbyssDepths-before-kaykit.unity");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AbyssDepths.unity");
            var root = Object.FindFirstObjectByType<AbyssExpedition>().transform;
            if (Available) EnsureGateFootings(root);
            var before = PhysicsSnapshot();
            var gameplay = GameplaySnapshot();
            RiftArtPolish.ApplyToScene();
            if (before != PhysicsSnapshot() || gameplay != GameplaySnapshot()) throw new InvalidOperationException("KayKit pass changed physics or gameplay");
            // Repeat the display pass to detect duplicate roots or unstable placements.
            ApplyToScene(root);
            if (root.GetComponentsInChildren<Transform>(true).Count(t => t.name == "KayKit入口示范") != 1) throw new InvalidOperationException("Duplicate entrance");
            if (before != PhysicsSnapshot() || gameplay != GameplaySnapshot()) throw new InvalidOperationException("Repeated pass changed physics or gameplay");
            FloatingRiftMapBuilder.Verify();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/kaykit-entrance-verification.txt", "PASS | Original colliders, transforms and gameplay serialized components unchanged\nPASS | Repeated display pass: one root, no colliders\nPASS | Main route and supply route navigation\n" + string.Join("\n", Models.Select(n => n + " | " + AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/" + n + ".prefab").GetComponentsInChildren<Renderer>().Length + " renderers")));
            Debug.Log("KayKit entrance applied and structural/navigation checks passed.");
        }

        public static void ApplyToScene(Transform root)
        {
            if (root.gameObject.scene.name != "AbyssDepths") throw new InvalidOperationException("KayKit entrance requires AbyssDepths");
            if (!Available) return;
            PrepareAssets();
            var old = root.Find("KayKit入口示范");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var parent = new GameObject("KayKit入口示范").transform;
            parent.SetParent(root, false);
            Paving(parent, root);
            Place(parent, "wall_doorway", GateBase, GateSize);
            EnsureGateFootings(root);
            foreach (int side in new[] { -1, 1 })
            {
                Place(parent, "column", new Vector3(side * 7, .02f, 3), new Vector3(2.5f, .7f, 2.5f));
                Place(parent, "pillar_decorated", PostBase(side), PostSize(side));
                Place(parent, "pillar_decorated", new Vector3(side * 9.6f, .02f, -.6f), new Vector3(1.65f, side < 0 ? 2.1f : 3.1f, 1.65f));
                Place(parent, "wall_half", new Vector3(side * 9, .02f, 3.2f), new Vector3(.85f, 1.25f, 4));
            }
            RiftFloorSetup.ApplyToScene(root);
        }

        private static void EnsureGateFootings(Transform root)
        {
            if (root.gameObject.scene.name != "AbyssDepths") throw new InvalidOperationException("Gate footings require AbyssDepths");
            var footings = Child(root, "起航拱门实体");
            foreach (int side in new[] { -1, 1 })
            {
                // The open arch spans x = -1..1 in a four-unit-wide source mesh.
                var pierSize = new Vector3(GateSize.x * .25f, GateSize.y * .5f, GateSize.z);
                var pierBase = GateBase + Vector3.right * (side * GateSize.x * .375f);
                Footing(side < 0 ? "左拱墩" : "右拱墩", pierBase, pierSize);
                Footing(side < 0 ? "左侧柱" : "右侧柱", PostBase(side), PostSize(side));
            }

            void Footing(string name, Vector3 bottom, Vector3 size)
            {
                var part = Child(footings, name);
                part.position = bottom + Vector3.up * (size.y * .5f);
                if (!part.TryGetComponent<BoxCollider>(out var collider)) collider = part.gameObject.AddComponent<BoxCollider>();
                collider.size = size;
                if (!part.TryGetComponent<NavMeshObstacle>(out var obstacle)) obstacle = part.gameObject.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = size;
                obstacle.carving = true;
            }

            static Transform Child(Transform parent, string name)
            {
                var child = parent.Find(name);
                if (child != null) return child;
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
                return child;
            }
        }

        private static void PrepareAssets()
        {
            Directory.CreateDirectory(Output); AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit missing");
            string path = Output + "/DungeonURP.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/dungeon_texture.png"));
            material.SetColor("_BaseColor", new Color(.55f, .61f, .68f));
            material.SetFloat("_Smoothness", .14f); EditorUtility.SetDirty(material);
            foreach (string model in Models)
            {
                string sourcePath = Source + "/" + model + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(sourcePath);
                if (importer.addCollider || importer.importAnimation) { importer.addCollider = false; importer.importAnimation = false; importer.SaveAndReimport(); }
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (asset == null) throw new InvalidOperationException("Missing model: " + sourcePath);
                var prefab = new GameObject(model);
                try
                {
                    var visual = new GameObject("VisualRoot").transform; visual.SetParent(prefab.transform, false);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset); instance.transform.SetParent(visual, false);
                    // The official doorway includes a separate wooden door; retain an open arch.
                    foreach (var door in instance.GetComponentsInChildren<Transform>(true).Where(t => t.name == "wall_doorway_door").ToArray()) Object.DestroyImmediate(door.gameObject);
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                    if (model == "wall_doorway" || model == "pillar_decorated") WeatheredModel(prefab, model);
                    if (model == "wall_doorway" || model == "pillar_decorated" || model == "wall_half")
                        foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>()) renderer.gameObject.AddComponent<AbyssOccluder>();
                    if (prefab.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Unexpected model collider");
                    PrefabUtility.SaveAsPrefabAsset(prefab, Output + "/" + model + ".prefab");
                }
                finally { Object.DestroyImmediate(prefab); }
            }
        }

        internal static Transform Place(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/" + name + ".prefab"));
            go.transform.SetParent(parent, false);
            var visual = go.transform.Find("VisualRoot");
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0) throw new InvalidOperationException("Empty model bounds");
            visual.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
            visual.localPosition = -Vector3.Scale(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), visual.localScale);
            go.transform.position = position;
            return go.transform;
        }

        private static string PhysicsSnapshot() => string.Join("\n", Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Select(c => c.GetInstanceID() + EditorJsonUtility.ToJson(c) + EditorJsonUtility.ToJson(c.transform)));
        // Modular display roots and altar crystals may gain fading; gameplay stays protected.
        private static string GameplaySnapshot() => string.Join("\n", Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Where(c => c != null && !(c is AbyssOccluder && (c.name == "悬浮裂隙核心" || c.name == "祭坛环绕晶体" || (c.name == "边缘晶簇" && c.transform.position.z > 47) || c.GetComponentsInParent<Transform>(true).Any(t => t.name == "KayKit入口示范" || t.name == "KayKit守望遗迹" || t.name == "KayKit核心圣龛")))).Select(c => c.GetInstanceID() + EditorJsonUtility.ToJson(c)));
    }
}

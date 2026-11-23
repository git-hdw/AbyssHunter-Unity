using System;
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
    // Display-only bridge architecture. The original deck, rails and navigation stay authoritative.
    [InitializeOnLoad]
    public static class SunkenBridgeSetup
    {
        internal const string RootName = "沉没桥廊模块建筑";
        private const string Folder = "Assets/_Game/Art/SunkenBridge";
        private const string ReviewKey = "AbyssHunter.BridgeReview";

        static SunkenBridgeSetup()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ReviewKey, false)) return;
                SessionState.EraseBool(ReviewKey);
                SessionState.EraseBool("AbyssHunter.LocalVerification");
                EditorApplication.delayCall += () =>
                {
                    bool success = false;
                    try
                    {
                        string report = File.ReadAllText("Logs/atmosphere-verification.txt");
                        if (!report.Contains("DONE") || report.Contains("FAIL |")) throw new InvalidOperationException("Environment review failed");
                        File.Copy("Logs/atmosphere-verification.txt", "Logs/bridge-atmosphere.txt", true);
                        PortfolioTools.BuildInventoryVerification();
                        File.Copy("Logs/portfolio-build.txt", "Logs/bridge-development-build.txt", true);
                        success = true;
                    }
                    catch (Exception e) { Debug.LogException(e); }
                    if (Environment.GetCommandLineArgs().Contains("--bridge-review-exit")) EditorApplication.Exit(success ? 0 : 1);
                };
            };
        }

        // Invoke with a graphical editor: environment screenshots require an actual rendered frame.
        [MenuItem("Tools/深渊猎手/环境/桥廊集中验证与开发包 %&#h")]
        public static void ReviewAndBuild()
        {
            Apply();
            Directory.CreateDirectory("Logs/bridge-baseline");
            if (File.Exists("Logs/atmosphere-verification.txt")) File.Copy("Logs/atmosphere-verification.txt", "Logs/bridge-baseline/previous-atmosphere.txt", true);
            SessionState.SetBool(ReviewKey, true);
            AbyssArtSetup.Verify();
        }

        [MenuItem("Tools/深渊猎手/环境/精修第一关桥廊 %&#b")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var root = GameObject.Find("Sunken Ruins").transform;
            string before = Physics();
            ApplyToScene(root); ApplyToScene(root);
            if (before != Physics()) throw new InvalidOperationException("Bridge changed original physics");
            foreach (float z in new[] { 12f, 36f })
            foreach (float x in new[] { -2f, 0f, 2f })
            foreach (int direction in new[] { -1, 1 })
            {
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(new Vector3(x, 0, z - direction * 6), new Vector3(x, 0, z + direction * 6), NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("Bridge navigation incomplete");
            }
            if (root.Cast<Transform>().Count(t => t.name == RootName) != 1) throw new InvalidOperationException("Duplicate bridge display");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/bridge-structure.txt", "PASS | Repeated generation; single display root; original colliders/transforms unchanged\nPASS | Both bridges: three lanes, both directions, complete NavMesh paths\n");
        }

        public static void ApplyToScene(Transform root)
        {
            if (root.gameObject.scene.name != "SampleScene") throw new InvalidOperationException("Bridge requires SampleScene");
            var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/KayKitEntrance/DungeonURP.mat");
            if (template == null) throw new InvalidOperationException("Missing licensed KayKit material");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var old = root.Find(RootName); if (old != null) Object.DestroyImmediate(old.gameObject);
            var art = new GameObject(RootName).transform; art.SetParent(root, false);
            var stone = Material("桥廊潮蚀石", template, new Color(.49f, .57f, .59f), Color.black);
            var submerged = Material("桥下冷石", template, new Color(.34f, .43f, .47f), new Color(.018f, .028f, .035f));
            var distant = Material("远层残拱", template, new Color(.20f, .27f, .32f), new Color(.008f, .013f, .02f));
            var bronze = Material("桥头旧铜", template, new Color(.44f, .34f, .20f), Color.black);
            for (int bridge = 0; bridge < 2; bridge++)
            {
                float z = 12 + bridge * 24;
                foreach (int side in new[] { -1, 1 })
                {
                    // Below the bridge and outside its rails: an open arch reveals the drop underneath.
                    Module(art, "wall_doorway", "桥下承重残拱", new Vector3(side * 4.15f, -5.25f, z), new Vector3(9.2f, 4.7f, .85f), 90, submerged);
                    for (int end = -1; end <= 1; end += 2)
                    {
                        float height = side == end ? 2.75f : 1.65f;
                        if (bridge == 1) height += side < 0 ? .35f : -.2f;
                        Module(art, "pillar_decorated", "桥头断柱", new Vector3(side * 4.2f, -.35f, z + end * 3.35f), new Vector3(1.05f, height, 1.05f), side * (end * 7 + bridge * 4), stone);
                    }
                    var crest = Module(art, "floor_tile_small_decorated", "桥头铜浮雕", new Vector3(side * 4.2f, .55f, z - 3.94f), new Vector3(.58f, .06f, .65f), 0, bronze);
                    crest.rotation = Quaternion.Euler(-90, 0, 0);
                }
                // Alternate the distant silhouette between crossings, leaving the forward sightline open.
                int farSide = bridge == 0 ? 1 : -1;
                Module(art, "wall_doorway", "深渊远层遗迹", new Vector3(farSide * 13.8f, -8.8f, z + 1.8f), new Vector3(7.8f, 5.5f, 1.05f), farSide * 22, distant);
            }
            if (art.GetComponentsInChildren<Collider>(true).Length != 0 || art.GetComponentsInChildren<Light>(true).Length != 0)
                throw new InvalidOperationException("Bridge art must not add collision or lights");
            foreach (var renderer in art.GetComponentsInChildren<MeshRenderer>())
            {
                var bounds = renderer.bounds;
                if (bounds.min.x < 3.35f && bounds.max.x > -3.35f && bounds.max.y > -.35f)
                    throw new InvalidOperationException("Bridge art intrudes into the deck sightline");
            }
            AssetDatabase.SaveAssets();
        }

        private static Transform Module(Transform parent, string model, string label, Vector3 position, Vector3 size, float yaw, Material material)
        {
            var go = KayKitEntranceSetup.Place(parent, model, position, size); go.name = label;
            go.rotation = Quaternion.Euler(0, yaw, 0);
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = material;
                if (renderer.GetComponent<AbyssOccluder>() == null) renderer.gameObject.AddComponent<AbyssOccluder>();
            }
            return go;
        }
        private static Material Material(string name, Material template, Color color, Color emission)
        {
            string path = Folder + "/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null) { result = new Material(template); AssetDatabase.CreateAsset(result, path); }
            result.SetColor("_BaseColor", color); result.SetFloat("_Smoothness", .18f);
            result.SetColor("_EmissionColor", emission);
            result.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            MaterialEditor.FixupEmissiveFlag(result);
            if (emission.maxColorComponent > 0) result.EnableKeyword("_EMISSION"); else result.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(result); return result;
        }
        private static string Physics() => string.Join("\n", Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
            .Select(c => c.GetInstanceID() + EditorJsonUtility.ToJson(c) + EditorJsonUtility.ToJson(c.transform)));
    }
}

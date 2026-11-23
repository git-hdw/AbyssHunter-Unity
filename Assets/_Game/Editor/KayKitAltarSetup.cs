using System;
using System.IO;
using System.Linq;
using AbyssHunter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    // A ruined apse beyond the altar's existing perimeter. The fighting floor and core stay intact.
    public static class KayKitAltarSetup
    {
        private const string Folder = "Assets/_Game/Art/KayKitAltar";
        private const string RootName = "KayKit核心圣龛";

        [MenuItem("Tools/深渊猎手/环境/精修第二关核心圣龛 %&#l")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Logs");
            File.Copy("Assets/Scenes/AbyssDepths.unity", "Logs/AbyssDepths-before-altar-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AbyssDepths.unity");
            var root = Object.FindFirstObjectByType<AbyssExpedition>().transform;
            string before = ProtectedSnapshot(root);
            ApplyToScene(root);
            if (before != ProtectedSnapshot(root)) throw new InvalidOperationException("Altar art changed existing scene content");
            ApplyToScene(root);
            if (before != ProtectedSnapshot(root)) throw new InvalidOperationException("Repeated altar art changed existing scene content");
            if (root.Cast<Transform>().Count(t => t.name == RootName) != 1) throw new InvalidOperationException("Duplicate altar root");
            FloatingRiftMapBuilder.Verify();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            var art = root.Find(RootName);
            File.WriteAllLines("Logs/altar-art-verification.txt", new[] {
                "PASS | All original transforms, colliders, gameplay and render components preserved",
                "PASS | Repeated generation: one altar root, no colliders, lights or particles",
                "PASS | All masonry remains outside the original 14-metre island radius",
                "PASS | Main and supply navigation complete",
                "INFO | Active renderers: " + art.GetComponentsInChildren<MeshRenderer>().Count(r => r.enabled),
                "INFO | Faders: " + art.GetComponentsInChildren<AbyssOccluder>().Length,
                "INFO | Triangles: " + art.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3) });
        }

        public static void ApplyToScene(Transform root)
        {
            if (root.gameObject.scene.name != "AbyssDepths") throw new InvalidOperationException("Altar art requires AbyssDepths");
            if (!KayKitEntranceSetup.Available) return;
            var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/KayKitEntrance/DungeonURP.mat");
            if (template == null) throw new InvalidOperationException("Missing existing KayKit material");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var previous = root.Find(RootName); if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var art = new GameObject(RootName).transform; art.SetParent(root, false);
            var stone = Material("圣龛风化冷石", template, new Color(.58f, .60f, .66f));
            var deep = Material("圣龛深层石", template, new Color(.24f, .29f, .37f));
            var relief = Material("圣龛旧铜浮雕", template, new Color(.46f, .35f, .23f));

            // The rear opening frames the orange core. Unequal piers and a lower sunken arch
            // give the island a structural silhouette without placing obstacles on its floor.
            Module(art, "wall_doorway", new Vector3(8, 3.05f, 75), new Vector3(11.2f, 5.8f, 1.15f), stone);
            Module(art, "wall_doorway", new Vector3(8, -4.8f, 75.25f), new Vector3(10.2f, 7.6f, 1.25f), deep);
            var crest = KayKitEntranceSetup.Place(art, "floor_tile_small_decorated", new Vector3(8, 7.85f, 74.35f), new Vector3(1.9f, .15f, 1.5f));
            crest.rotation = Quaternion.Euler(-90, 0, 0);
            foreach (var surface in crest.GetComponentsInChildren<MeshRenderer>()) surface.sharedMaterial = relief;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 8 + side * 6.4f;
                Module(art, "pillar_decorated", new Vector3(x, -4.8f, 74.3f), new Vector3(1.7f, side < 0 ? 14.3f : 12.8f, 1.7f), stone);
                Module(art, "column", new Vector3(x, 2.65f, 74.3f), new Vector3(2.15f, .62f, 2.15f), stone);
                // Lower detached supports establish depth at the edges, below the main arch.
                Module(art, "pillar_decorated", new Vector3(8 + side * 15.1f, -4.8f, 62), new Vector3(1.4f, side < 0 ? 10.5f : 9.5f, 1.4f), stone);
            }
            foreach (var surface in art.GetComponentsInChildren<Renderer>())
            {
                var closest = surface.bounds.ClosestPoint(new Vector3(8, surface.bounds.center.y, 60));
                if (Vector2.Distance(new Vector2(closest.x, closest.z), new Vector2(8, 60)) < 14.05f)
                    throw new InvalidOperationException("Altar display intrudes into the walking island: " + surface.name);
            }
            if (art.GetComponentsInChildren<Collider>(true).Length != 0 || art.GetComponentsInChildren<Light>(true).Length != 0
                || art.GetComponentsInChildren<ParticleSystem>(true).Length != 0)
                throw new InvalidOperationException("Altar display gained physics, lights or particles");
            AssetDatabase.SaveAssets();
        }

        private static void Module(Transform parent, string model, Vector3 position, Vector3 size, Material material)
        {
            var go = KayKitEntranceSetup.Place(parent, model, position, size);
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

        private static string ProtectedSnapshot(Transform root)
        {
            var art = root.Find(RootName);
            return string.Join("\n", Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
                .Where(c => c != null && (art == null || !c.transform.IsChildOf(art)))
                // The mission root's child list necessarily gains the display root; its own pose must not change.
                .Select(c => c.transform == root && c is Transform
                    ? c.GetInstanceID() + root.localToWorldMatrix.ToString("R")
                    : c.GetInstanceID() + EditorJsonUtility.ToJson(c)));
        }
    }
}

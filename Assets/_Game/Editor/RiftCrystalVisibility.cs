using System;
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
    public static class RiftCrystalVisibility
    {
        private const string Folder = "Assets/_Game/Art/RiftVisibility";
        [MenuItem("Tools/深渊猎手/环境/修正第二关晶体遮挡 %&#o")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AbyssDepths.unity");
            var root = Object.FindFirstObjectByType<AbyssExpedition>().transform;
            string before = Physics();
            ApplyToScene(root); ApplyToScene(root);
            if (before != Physics()) throw new InvalidOperationException("Crystal pass changed collision");
            FloatingRiftMapBuilder.Verify();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/crystal-visibility-structure.txt", "PASS | Repeated application; original colliders/transforms preserved; main and side navigation complete\n");
        }

        public static void ApplyToScene(Transform root)
        {
            if (root.gameObject.scene.name != "AbyssDepths") throw new InvalidOperationException("Crystal pass requires AbyssDepths");
            var art = root.Find("裂隙精修美术");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var sources = art.GetComponentsInChildren<MeshFilter>(true).Where(f => !f.name.StartsWith("环境合批_", StringComparison.Ordinal)
                && f.GetComponent<MeshRenderer>() != null && f.GetComponent<MeshRenderer>().sharedMaterial.shader.name != "AbyssHunter/DepthMist").ToArray();
            var targets = sources.Where(IsCluster).ToArray();
            if (targets.Length != 6) throw new InvalidOperationException("Expected six altar cluster source meshes");
            foreach (var group in sources.GroupBy(f => (f.GetComponent<MeshRenderer>().sharedMaterial, Mathf.FloorToInt(f.transform.position.x / 24), Mathf.FloorToInt(f.transform.position.z / 24))))
            {
                if (!group.Any(IsCluster)) continue;
                var full = Combine(group.ToArray(), art);
                var batch = art.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("环境合批_", StringComparison.Ordinal)).SingleOrDefault(f =>
                {
                    var original = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Game/Art/RiftPolish/" + f.name + ".asset");
                    return original != null && original.vertexCount == full.vertexCount && original.vertices.Zip(full.vertices, (a, b) => (a - b).sqrMagnitude < .00000001f).All(v => v);
                });
                Object.DestroyImmediate(full);
                if (batch == null) throw new InvalidOperationException("Cannot identify original crystal batch");
                var remaining = Combine(group.Where(f => !IsCluster(f)).ToArray(), art);
                string path = Folder + "/" + batch.name + ".asset";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved == null) { AssetDatabase.CreateAsset(remaining, path); saved = remaining; }
                else { EditorUtility.CopySerialized(remaining, saved); Object.DestroyImmediate(remaining); EditorUtility.SetDirty(saved); }
                batch.sharedMesh = saved;
            }
            foreach (var target in targets) { target.GetComponent<MeshRenderer>().enabled = true; Fade(target.gameObject); }
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name == "悬浮裂隙核心" || r.name == "祭坛环绕晶体")) Fade(renderer.gameObject);
            AssetDatabase.SaveAssets();
        }
        private static bool IsCluster(MeshFilter f) => f.name == "边缘晶簇" && f.transform.position.z > 47;
        private static void Fade(GameObject go)
        {
            var component = go.GetComponent<AbyssOccluder>() ?? go.AddComponent<AbyssOccluder>();
            var data = new SerializedObject(component); data.FindProperty("targetHeight").floatValue = 1.1f; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Mesh Combine(MeshFilter[] filters, Transform art)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32, name = "晶体分离后的环境" };
            mesh.CombineMeshes(filters.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = art.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray());
            mesh.RecalculateBounds(); return mesh;
        }
        private static string Physics() => string.Join("\n", Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
            .Select(c => c.GetInstanceID() + EditorJsonUtility.ToJson(c) + EditorJsonUtility.ToJson(c.transform)));
    }
}

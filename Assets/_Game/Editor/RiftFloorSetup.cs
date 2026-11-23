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
    // One floor material and one world-space texel scale across decks and modular paving.
    // Only render meshes receive new UVs/submeshes; collider meshes remain untouched.
    public static class RiftFloorSetup
    {
        private const string Folder = "Assets/_Game/Art/RiftFloor";
        private const float RepeatMetres = 3.56f;
        private static readonly string[] DeckNames = {
            "起航浮岛", "双层守望台", "补给悬台", "中央祭坛浮岛", "起航引桥", "起航桥平转台",
            "西行升空桥", "守望桥平转台", "守望台接桥", "祭坛下层接桥", "长桥下层平转台",
            "中央升空长桥", "长桥上层平转台", "祭坛上层接桥", "补给侧桥南", "补给侧桥北" };

        [MenuItem("Tools/深渊猎手/环境/统一第二关地板材质 %&#u")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Logs");
            File.Copy("Assets/Scenes/AbyssDepths.unity", "Logs/AbyssDepths-before-floor-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AbyssDepths.unity");
            var root = Object.FindFirstObjectByType<AbyssExpedition>().transform;
            string before = PhysicsAndGameplay();
            ApplyToScene(root); ApplyToScene(root);
            if (before != PhysicsAndGameplay()) throw new InvalidOperationException("Floor material pass changed physics or gameplay");
            FloatingRiftMapBuilder.Verify();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllLines("Logs/floor-material-verification.txt", new[] {
                "PASS | Repeated floor application preserves colliders, transforms and all gameplay components",
                "PASS | Shared floor material and world-space UV scale across decks and modular paving",
                "PASS | Main and supply navigation complete",
                "INFO | Floor renderers: " + Floors(root).Length });
            Debug.Log("Second-level floor material unified; collision, navigation and gameplay preserved.");
        }

        public static void ApplyToScene(Transform root)
        {
            if (root.gameObject.scene.name != "AbyssDepths") throw new InvalidOperationException("Floor pass requires AbyssDepths");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var material = FloorMaterial();
            var side = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/RiftPolish/深层玄武岩.mat");
            if (side == null) throw new InvalidOperationException("Missing existing platform-side material");
            var floors = Floors(root);
            if (floors.Select(r => r.name).Distinct().Count() != floors.Length)
                throw new InvalidOperationException("Duplicate floor names would overwrite a shared render mesh");
            foreach (var renderer in floors)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                var source = filter.sharedMesh;
                if (source == null) throw new InvalidOperationException("Missing floor mesh: " + renderer.name);
                var mesh = Object.Instantiate(source); mesh.name = renderer.name;
                var vertices = mesh.vertices;
                mesh.uv = vertices.Select(p => {
                    var world = filter.transform.TransformPoint(p);
                    return new Vector2(world.x / RepeatMetres, world.z / RepeatMetres);
                }).ToArray();
                if (renderer.GetComponent<MeshCollider>() != null)
                {
                    // Existing deck prisms have duplicated face vertices. Keep their vertical fascia dark.
                    var triangles = mesh.triangles;
                    var top = new System.Collections.Generic.List<int>();
                    var sides = new System.Collections.Generic.List<int>();
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        var normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]).normalized;
                        var target = filter.transform.TransformDirection(normal).y > .5f ? top : sides;
                        target.Add(triangles[i]); target.Add(triangles[i + 1]); target.Add(triangles[i + 2]);
                    }
                    mesh.subMeshCount = 2; mesh.SetTriangles(top, 0); mesh.SetTriangles(sides, 1);
                    renderer.sharedMaterials = new[] { material, side };
                }
                else renderer.sharedMaterial = material;
                string path = Folder + "/地面显示_" + renderer.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
                else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); }
                filter.sharedMesh = existing;
                if (renderer.sharedMaterials[0] != material) throw new InvalidOperationException("Inconsistent floor material");
                var actual = existing.uv;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var point = filter.transform.TransformPoint(vertices[i]);
                    if (Vector2.Distance(actual[i], new Vector2(point.x / RepeatMetres, point.z / RepeatMetres)) > .0001f)
                        throw new InvalidOperationException("Inconsistent floor texture scale");
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static MeshRenderer[] Floors(Transform root) => root.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => r.enabled && (
                (r.transform.parent == root && DeckNames.Contains(r.name) && r.GetComponent<MeshCollider>() != null)
                || (r.transform.parent?.name == "KayKit入口示范" && r.name.StartsWith("入口错缝石铺_", StringComparison.Ordinal))
                || (r.transform.parent?.name == "KayKit守望遗迹" && r.name.StartsWith("守望错缝铺石_", StringComparison.Ordinal)))).ToArray();

        private static Material FloorMaterial()
        {
            string path = Folder + "/统一冷灰石铺.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", new Color(.27f, .33f, .40f));
            material.SetFloat("_Smoothness", .12f); material.SetFloat("_Metallic", .03f);
            material.SetTexture("_BaseMap", FloorTexture()); material.SetTextureScale("_BaseMap", Vector2.one);
            EditorUtility.SetDirty(material); return material;
        }

        private static Texture2D FloorTexture()
        {
            const int size = 512;
            string path = Folder + "/统一错缝石纹.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "统一错缝石纹", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
                AssetDatabase.CreateAsset(texture, path);
            }
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = x / (float)size * 2, v = y / (float)size * 2;
                int row = Mathf.FloorToInt(v); u += row % 2 == 0 ? .125f : -.125f;
                int column = Mathf.FloorToInt(u); float localX = Mathf.Repeat(u, 1), localY = Mathf.Repeat(v, 1);
                int hash = ((column % 2 + 2) % 2) * 17 + row * 31;
                float distance = Mathf.Min(Mathf.Min(localX, 1 - localX), Mathf.Min(localY, 1 - localY));
                float grain = .88f + Mathf.PerlinNoise(x * .15f, y * .15f) * .06f + (hash % 3) * .012f;
                float bevel = Mathf.SmoothStep(.48f, 1, Mathf.Clamp01(distance / .026f));
                float crack = hash % 3 == 0 && Mathf.Abs(localX - .7f + localY * .24f) < .003f && localY < .33f ? .63f : 1;
                float value = grain * bevel * crack;
                pixels[y * size + x] = new Color(value, value, value, 1);
            }
            texture.SetPixels(pixels); texture.Apply(); EditorUtility.SetDirty(texture); return texture;
        }

        private static string PhysicsAndGameplay() => string.Join("\n", Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
            .Where(c => c is Collider || c is Transform || c is MonoBehaviour).Select(c => c.GetInstanceID() + EditorJsonUtility.ToJson(c)));
    }
}

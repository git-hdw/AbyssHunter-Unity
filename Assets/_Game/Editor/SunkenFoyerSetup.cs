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
    public static class SunkenFoyerSetup
    {
        private const string Folder = "Assets/_Game/Art/SunkenFoyer";
        private const string RootName = "沉没前厅模块建筑";
        [MenuItem("Tools/深渊猎手/环境/试用第一关模块前厅 %&#f")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var root = GameObject.Find("Sunken Ruins").transform;
            string before = Physics();
            ApplyToScene(root); ApplyToScene(root);
            if (before != Physics()) throw new InvalidOperationException("Foyer changed original physics");
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(new Vector3(0, 0, -6), new Vector3(0, 0, 12), NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Foyer exit navigation incomplete");
            if (root.Cast<Transform>().Count(t => t.name == RootName) != 1) throw new InvalidOperationException("Duplicate foyer");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/foyer-structure.txt", "PASS | Repeated generation; original colliders/transforms unchanged; entrance-to-bridge navigation complete\n");
        }
        public static void ApplyToScene(Transform root)
        {
            if (root.gameObject.scene.name != "SampleScene") throw new InvalidOperationException("Foyer requires SampleScene");
            var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/KayKitEntrance/DungeonURP.mat");
            if (template == null) throw new InvalidOperationException("Missing licensed KayKit subset");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var old = root.Find(RootName); if (old != null) Object.DestroyImmediate(old.gameObject);
            // Retain authoritative colliders and all old transforms, hiding only superseded visuals.
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool column = r.name is "Fractured pillar" or "Pillar crown";
                bool oldGate = r.name is "Arch left" or "Arch right" or "Broken arch lintel" or "Broken arch tip" or "前厅破损拱券";
                if ((column && Mathf.Abs(r.transform.position.z) <= 6.1f) || (oldGate && r.transform.position.z < 0)) r.enabled = false;
            }
            var art = new GameObject(RootName).transform; art.SetParent(root, false);
            var stone = Material("潮蚀前厅石", template, new Color(.53f, .61f, .62f), .25f);
            var deep = Material("下沉暗石", template, new Color(.26f, .34f, .38f), .16f);
            var submerged = Material("浅层潮蚀石", template, new Color(.40f, .48f, .50f), .18f);
            // A low material fill preserves the silhouette under the deck's shadow; no extra real-time lights.
            submerged.SetColor("_EmissionColor", new Color(.025f, .04f, .045f));
            submerged.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            MaterialEditor.FixupEmissiveFlag(submerged); submerged.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(submerged);
            var bronze = Material("前厅旧铜纹", template, new Color(.48f, .37f, .21f), .38f);
            var gate = Module(art, "wall_doorway", new Vector3(0, .05f, -5), new Vector3(12.6f, 5.8f, 1.2f), stone);
            WidenGate(gate);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int j = -1; j <= 1; j++)
                    Module(art, "pillar_decorated", new Vector3(side * 8, .45f, j * 6), new Vector3(1.2f, j == 0 ? 4.45f : (side == j ? 2.25f : 2.75f), 1.2f), stone);
                // All side masonry sits beyond the existing rim; the combat floor stays open.
                var wall = Module(art, "wall_doorway", new Vector3(side * 10.1f, -3.7f, .5f), new Vector3(12, 4.2f, 1.1f), submerged);
                wall.rotation = Quaternion.Euler(0, 90, 0);
                var arch = Module(art, "wall_doorway", new Vector3(side * 10.15f, -7.5f, .5f), new Vector3(10.8f, 5, 1.1f), deep);
                arch.rotation = Quaternion.Euler(0, 90, 0);
            }
            // Recessed masonry below the front rim is visible from the game camera, without a false floor.
            Module(art, "wall_doorway", new Vector3(0, -5.4f, -8.65f), new Vector3(16.4f, 4.6f, 1.1f), submerged);
            Module(art, "wall_doorway", new Vector3(-1.1f, -9.3f, -7.9f), new Vector3(13.2f, 4.1f, .9f), deep);
            var crest = Module(art, "floor_tile_small_decorated", new Vector3(-3.8f, 4.8f, -5.67f), new Vector3(1.35f, .12f, 1.25f), bronze);
            crest.rotation = Quaternion.Euler(-90, 0, 0);
            if (art.GetComponentsInChildren<Collider>(true).Length != 0 || art.GetComponentsInChildren<Light>(true).Length != 0)
                throw new InvalidOperationException("Foyer display must not add physics or lights");
            AssetDatabase.SaveAssets();
        }
        private static void WidenGate(Transform gate)
        {
            // Widen the imported doorway to match the existing open passage, without changing physics.
            int index = 0;
            foreach (var filter in gate.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = Object.Instantiate(filter.sharedMesh);
                var bounds = mesh.bounds; var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    float x = (vertices[i].x - bounds.center.x) / bounds.extents.x;
                    vertices[i].x = bounds.center.x + Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), .2f) * bounds.extents.x;
                }
                mesh.vertices = vertices;
                var fractured = FractureLintel(mesh, filter.transform);
                Object.DestroyImmediate(mesh); mesh = fractured;
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string path = Folder + "/前厅宽门洞" + index++ + ".asset";
                mesh.name = Path.GetFileNameWithoutExtension(path);
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
                else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
                filter.sharedMesh = saved;
            }
        }
        private static Mesh FractureLintel(Mesh source, Transform transform)
        {
            // Two oblique cuts through the widened imported lintel. Retain the piers, UVs and closed ends.
            var points = source.vertices; var uv = source.uv; var triangles = source.triangles;
            var vertices = new List<Vector3>(); var texture = new List<Vector2>(); var indices = new List<int>();
            void Triangle(Vector3 a, Vector2 ua, Vector3 b, Vector2 ub, Vector3 c, Vector2 uc)
            {
                if (Vector3.Cross(b - a, c - a).sqrMagnitude < .000000001f) return;
                int n = vertices.Count;
                vertices.Add(transform.InverseTransformPoint(a)); vertices.Add(transform.InverseTransformPoint(b)); vertices.Add(transform.InverseTransformPoint(c));
                texture.Add(ua); texture.Add(ub); texture.Add(uc); indices.Add(n); indices.Add(n + 1); indices.Add(n + 2);
            }
            void Half(Vector3 normal, Vector3 origin)
            {
                normal.Normalize();
                var cap = new List<Vector3>(); Vector2 stoneUv = Vector2.zero;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    var polygon = new List<(Vector3 p, Vector2 uv)>();
                    for (int e = 0; e < 3; e++)
                    {
                        int a = triangles[t + e], b = triangles[t + (e + 1) % 3];
                        var pa = transform.TransformPoint(points[a]); var pb = transform.TransformPoint(points[b]);
                        float da = Vector3.Dot(normal, pa - origin), db = Vector3.Dot(normal, pb - origin);
                        if (da <= 0) polygon.Add((pa, uv[a]));
                        if ((da < 0 && db > 0) || (da > 0 && db < 0))
                        {
                            float f = da / (da - db); var p = Vector3.Lerp(pa, pb, f);
                            var edgeUv = Vector2.Lerp(uv[a], uv[b], f); polygon.Add((p, edgeUv));
                            if (!cap.Any(v => (v - p).sqrMagnitude < .0000001f)) cap.Add(p);
                            stoneUv = edgeUv;
                        }
                    }
                    for (int i = 1; i + 1 < polygon.Count; i++)
                        Triangle(polygon[0].p, polygon[0].uv, polygon[i].p, polygon[i].uv, polygon[i + 1].p, polygon[i + 1].uv);
                }
                if (cap.Count < 3) throw new InvalidOperationException("Foyer lintel cut missed the imported stone");
                var center = cap.Aggregate(Vector3.zero, (a, b) => a + b) / cap.Count;
                var axis = Vector3.Cross(normal, Vector3.forward).normalized; var other = Vector3.Cross(normal, axis);
                var ordered = cap.OrderBy(p => Mathf.Atan2(Vector3.Dot(p - center, other), Vector3.Dot(p - center, axis))).ToArray();
                for (int i = 0; i < ordered.Length; i++) Triangle(center, stoneUv, ordered[i], stoneUv, ordered[(i + 1) % ordered.Length], stoneUv);
            }
            Half(new Vector3(1, .35f, .14f), new Vector3(-.8f, 4.2f, -5));
            Half(new Vector3(-1, -.22f, .1f), new Vector3(1.05f, 4.2f, -5));
            var result = new Mesh { name = "前厅斜断门楣" };
            result.SetVertices(vertices); result.SetUVs(0, texture); result.SetTriangles(indices, 0);
            return result;
        }
        private static Transform Module(Transform parent, string model, Vector3 position, Vector3 size, Material material)
        {
            var go = KayKitEntranceSetup.Place(parent, model, position, size);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                r.sharedMaterial = material;
                if (r.GetComponent<AbyssOccluder>() == null) r.gameObject.AddComponent<AbyssOccluder>();
            }
            return go;
        }
        private static Material Material(string name, Material template, Color color, float smooth)
        {
            string path = Folder + "/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null) { result = new Material(template); AssetDatabase.CreateAsset(result, path); }
            result.SetColor("_BaseColor", color); result.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(result); return result;
        }
        private static string Physics() => string.Join("\n", Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
            .Select(c => c.GetInstanceID() + EditorJsonUtility.ToJson(c) + EditorJsonUtility.ToJson(c.transform)));
    }
}

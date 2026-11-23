using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    public static partial class KayKitEntranceSetup
    {
        private static void Paving(Transform parent, Transform root)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Output + "/DungeonURP.mat");
            var paving = StoneMaterial("旧石铺面", new Color(.46f, .52f, .59f), material);
            var worn = StoneMaterial("旧石暗面", new Color(.44f, .50f, .57f), material);
            var bedding = StoneMaterial("入口石缝底层", new Color(.19f, .235f, .28f), material);
            bedding.SetTexture("_BaseMap", BeddingTexture()); bedding.SetTextureScale("_BaseMap", Vector2.one * 3); EditorUtility.SetDirty(bedding);
            // Keep the exact original deck/collision mesh. Only its exposed material becomes mortar.
            root.GetComponentsInChildren<MeshRenderer>().First(r => r.name == "起航浮岛").sharedMaterial = bedding;
            foreach (var mark in root.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "罗盘刻线" && r.transform.position.z < 10)) mark.enabled = false;
            var tiles = new List<MeshFilter>();
            for (int z = -4; z <= 4; z++) for (int x = -5; x <= 5; x++)
            {
                int hash = (x + 9) * 13 + (z + 9) * 29;
                float boundary = x * x / 29f + z * z / 18f;
                if (boundary > 1.12f || (boundary > .68f && hash % 4 == 0)) continue;
                bool broken = boundary > .82f && hash % 5 == 0;
                string name = broken ? "floor_tile_small_broken_A" : (x == 0 && z == 1 ? "floor_tile_small_decorated" : "floor_tile_small");
                var tile = Place(parent, name,
                    new Vector3(x * 1.78f + (z % 2 == 0 ? -.22f : .22f), .008f, z * 1.78f - .3f),
                    new Vector3(1.72f, .038f, 1.72f));
                tile.rotation = Quaternion.Euler(0, hash % 4 * 90 + (boundary > .62f ? (hash % 5 - 2) * .7f : 0), 0);
                foreach (var renderer in tile.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.sharedMaterial = hash % 5 == 0 ? worn : paving;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    tiles.Add(renderer.GetComponent<MeshFilter>());
                }
            }
            // Many modules, two renderers. Keep tall architecture separate so it can fade independently.
            int index = 0;
            foreach (var group in tiles.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = parent.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray());
                mesh.RecalculateBounds();
                var go = new GameObject("入口错缝石铺_" + index, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = SaveDisplayMesh("入口错缝石铺_" + index++, mesh);
                go.GetComponent<MeshRenderer>().sharedMaterial = group.Key;
                go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                foreach (var filter in group) filter.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        private static Texture2D BeddingTexture()
        {
            string path = Output + "/石缝细粒.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(128, 128, TextureFormat.RGBA32, true) { name = "石缝细粒", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
                AssetDatabase.CreateAsset(texture, path);
            }
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float value = .78f + .12f * Mathf.PerlinNoise(x * .27f, y * .27f) + .1f * Mathf.PerlinNoise(x * 1.7f, y * 1.7f);
                pixels[y * 128 + x] = new Color(value, value, value);
            }
            texture.SetPixels(pixels); texture.Apply(); EditorUtility.SetDirty(texture); return texture;
        }

        private static Material StoneMaterial(string name, Color color, Material template)
        {
            string path = Output + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(template); AssetDatabase.CreateAsset(material, path); }
            else EditorUtility.CopySerialized(template, material);
            material.name = name; material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .12f); EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh SaveDisplayMesh(string name, Mesh mesh)
        {
            mesh.name = name; string path = Output + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing;
        }

        // Cut the imported stone along a sloping fracture, retaining its UVs and adding a closed cap.
        // Source FBX/import settings are never changed; the result is a separate project-owned mesh.
        private static void WeatheredModel(GameObject prefab, string model)
        {
            var renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var normal = model == "wall_doorway" ? new Vector3(.45f, 1, -.10f) : new Vector3(-.25f, 1, .17f);
            float limit = model == "wall_doorway" ? .98f : .92f;
            int index = 0;
            foreach (var renderer in renderers)
            {
                var filter = renderer.GetComponent<MeshFilter>(); var source = filter.sharedMesh;
                Vector3 Normalize(Vector3 p) => new Vector3((p.x - bounds.center.x) / bounds.size.x, (p.y - bounds.min.y) / bounds.size.y, (p.z - bounds.center.z) / bounds.size.z);
                Vector3 Restore(Vector3 p) => filter.transform.InverseTransformPoint(new Vector3(p.x * bounds.size.x + bounds.center.x, p.y * bounds.size.y + bounds.min.y, p.z * bounds.size.z + bounds.center.z));
                var positions = source.vertices.Select(p => Normalize(filter.transform.TransformPoint(p))).ToArray();
                var uvs = source.uv; var triangles = source.triangles;
                var vertices = new List<Vector3>(); var texture = new List<Vector2>(); var indices = new List<int>(); var cap = new List<StoneVertex>();
                void Triangle(StoneVertex a, StoneVertex b, StoneVertex c)
                {
                    int n = vertices.Count; vertices.Add(Restore(a.Position)); vertices.Add(Restore(b.Position)); vertices.Add(Restore(c.Position));
                    texture.Add(a.Uv); texture.Add(b.Uv); texture.Add(c.Uv); indices.Add(n); indices.Add(n + 1); indices.Add(n + 2);
                }
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    var polygon = new List<StoneVertex>();
                    for (int i = 0; i < 3; i++)
                    {
                        int a = triangles[t + i], b = triangles[t + (i + 1) % 3];
                        var va = new StoneVertex(positions[a], uvs[a]); var vb = new StoneVertex(positions[b], uvs[b]);
                        float da = Vector3.Dot(normal, va.Position) - limit, db = Vector3.Dot(normal, vb.Position) - limit;
                        if (da <= 0) polygon.Add(va);
                        if ((da < 0 && db > 0) || (da > 0 && db < 0))
                        {
                            var edge = new StoneVertex(Vector3.Lerp(va.Position, vb.Position, da / (da - db)), Vector2.Lerp(va.Uv, vb.Uv, da / (da - db)));
                            polygon.Add(edge);
                            if (!cap.Any(p => (p.Position - edge.Position).sqrMagnitude < .00000001f)) cap.Add(edge);
                        }
                    }
                    for (int i = 1; i + 1 < polygon.Count; i++) Triangle(polygon[0], polygon[i], polygon[i + 1]);
                }
                if (cap.Count >= 3)
                {
                    var center = cap.Aggregate(Vector3.zero, (sum, v) => sum + v.Position) / cap.Count;
                    var axis = Vector3.Cross(normal, Vector3.forward).normalized; var other = Vector3.Cross(normal.normalized, axis);
                    var ordered = cap.OrderBy(v => Mathf.Atan2(Vector3.Dot(v.Position - center, other), Vector3.Dot(v.Position - center, axis))).ToArray();
                    // Sample the stone part of the palette rather than interpolating into unrelated atlas colours.
                    var stoneUv = ordered[0].Uv;
                    for (int i = 0; i < ordered.Length; i++) Triangle(new StoneVertex(center, stoneUv), new StoneVertex(ordered[i].Position, stoneUv), new StoneVertex(ordered[(i + 1) % ordered.Length].Position, stoneUv));
                }
                var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, texture); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                filter.sharedMesh = SaveDisplayMesh(model + "_断面_" + index++, mesh);
            }
        }

        private readonly struct StoneVertex
        {
            public readonly Vector3 Position;
            public readonly Vector2 Uv;
            public StoneVertex(Vector3 position, Vector2 uv) { Position = position; Uv = uv; }
        }
    }
}

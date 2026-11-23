using System;
using System.Collections.Generic;
using AbyssHunter.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssHunter.EditorTools
{
    public static partial class KayKitWatchSetup
    {
        private static void Well(Transform parent, Material template)
        {
            var well = new GameObject("裂隙封印井").transform; well.SetParent(parent, false);
            var stone = Material("封印井风化石", template, new Color(.65f, .68f, .74f));
            var deep = Material("封印井深层石", template, new Color(.28f, .31f, .39f));
            // All masonry stays inside the original guarded gap, between the two supply bridges.
            // The tall rear arch provides silhouette; the front remains open to reveal the shaft.
            Module(well, "wall_doorway", new Vector3(-30.5f, 2.35f, 32.05f), new Vector3(4.85f, 5.5f, .48f), 0, stone);
            Module(well, "wall_doorway", new Vector3(-32.55f, 2.25f, 30), new Vector3(3.9f, 3.6f, .45f), 90, stone);
            float[] heights = { 8.0f, 11.8f, 7.2f, 10.1f };
            int index = 0;
            foreach (float x in new[] { -32.35f, -28.65f }) foreach (float z in new[] { 28.15f, 31.85f })
            {
                Module(well, "pillar_decorated", new Vector3(x, -3.6f, z), new Vector3(1.0f, heights[index++], 1.0f), x < -30 ? 7 : -7, stone);
                Module(well, "column", new Vector3(x, 2.75f, z), new Vector3(1.12f, .38f, 1.12f), 0, stone);
            }
            Module(well, "wall_half", new Vector3(-30.5f, -5.8f, 32.12f), new Vector3(3.45f, 8.6f, .26f), 0, deep);
            foreach (float x in new[] { -32.65f, -28.35f })
                Module(well, "wall_half", new Vector3(x, -5.8f, 30), new Vector3(3.8f, 8.6f, .26f), 90, deep);
            // Nested recessed rear arches suggest a buried structure, without capping the opening.
            Module(well, "wall_doorway", new Vector3(-30.5f, -3.1f, 31.79f), new Vector3(3.6f, 4.3f, .32f), 0, stone);
            Module(well, "wall_doorway", new Vector3(-30.5f, -6.6f, 31.92f), new Vector3(2.75f, 3.3f, .25f), 0, deep);

            var energy = Material("封印井紫色铭文", template, new Color(.18f, .10f, .30f));
            energy.SetTexture("_BaseMap", null);
            energy.SetColor("_EmissionColor", new Color(.36f, .11f, .8f) * 1.65f);
            // URP validates this flag on import/build and otherwise removes the emission keyword.
            energy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            MaterialEditor.FixupEmissiveFlag(energy); energy.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(energy);
            var marks = new GameObject("下沉封印铭文", typeof(MeshFilter), typeof(MeshRenderer), typeof(AbyssOccluder));
            marks.transform.SetParent(well, false);
            marks.GetComponent<MeshFilter>().sharedMesh = SaveMesh("封印井下沉铭文", WellGlyphs());
            var renderer = marks.GetComponent<MeshRenderer>(); renderer.sharedMaterial = energy;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            // Bounds guard is visual as well as physical: no masonry may overhang either side bridge.
            foreach (var surface in well.GetComponentsInChildren<Renderer>())
                if (surface.bounds.min.z < 27.5f || surface.bounds.max.z > 32.5f || surface.bounds.min.x < -33 || surface.bounds.max.x > -28)
                    throw new InvalidOperationException("Seal-well display extends beyond its original guarded footprint: " + surface.name);
        }

        private static Mesh WellGlyphs()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            void Strip(Vector3 a, Vector3 b, Vector3 width)
            {
                int i = vertices.Count;
                vertices.Add(a - width); vertices.Add(a + width); vertices.Add(b + width); vertices.Add(b - width);
                // Separate back-face vertices so recalculated normals cannot cancel to zero.
                vertices.Add(a - width); vertices.Add(a + width); vertices.Add(b + width); vertices.Add(b - width);
                triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i + 6, i + 5, i + 4, i + 7, i + 6, i + 4 });
            }
            // Broken octagonal seals descend into darkness. No solid disk or false walkable floor.
            for (int layer = 0; layer < 3; layer++)
            {
                float radius = 1.55f - layer * .26f, y = 1.9f - layer * 2.6f;
                for (int segment = 0; segment < 8; segment++)
                {
                    float a = segment * Mathf.PI / 4, b = a + Mathf.PI / 4 * .72f;
                    var center = new Vector3(-30.5f, y, 30);
                    var start = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    var end = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                    Strip(center + start * radius, center + end * radius, (start + end).normalized * .025f);
                }
            }
            // A small rear-wall sigil gives the tall arch a readable carved focal point.
            var origin = new Vector3(-30.5f, 5.1f, 31.78f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3, b = a + Mathf.PI / 3 * .75f;
                var start = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                var end = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0);
                Strip(origin + start * .64f, origin + end * .64f, (start + end).normalized * .025f);
                Strip(origin + start * .83f, origin + start * 1.0f, new Vector3(-start.y, start.x, 0) * .025f);
            }
            var mesh = new Mesh { name = "封印井下沉铭文" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            foreach (var normal in mesh.normals)
                if (float.IsNaN(normal.sqrMagnitude) || normal.sqrMagnitude < .9f) throw new InvalidOperationException("Invalid seal-well glyph normal");
            return mesh;
        }
    }
}

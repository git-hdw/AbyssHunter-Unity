using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    public static class SunkenAltarSetup
    {
        internal const string RootName = "沉没祭坛祈火壁";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/深渊猎手/环境/精修第一关终点祭坛 %&#k")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var ruins = GameObject.Find("Sunken Ruins").transform;
            ApplyToScene(ruins);
            ApplyToScene(ruins);
            var art = ruins.Find(RootName);
            if (ruins.Cast<Transform>().Count(t => t.name == RootName) != 1 ||
                art.GetComponentsInChildren<MeshRenderer>(true).Length != 11 ||
                art.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Sunken altar regeneration failed");
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save altar");
            Debug.Log("Sunken altar saved after two generation passes.");
        }

        internal static void ApplyToScene(Transform ruins)
        {
            if (ruins.gameObject.scene.path != ScenePath) throw new InvalidOperationException("Expected SampleScene");
            var stone = LoadMaterial("桥廊潮蚀石");
            var dark = LoadMaterial("桥下冷石");
            var bronze = LoadMaterial("桥头旧铜");
            var ember = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/SunkenPolish/祭坛琥珀铜.mat");
            if (!ember) throw new InvalidOperationException("Missing ember material");
            var old = ruins.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var art = new GameObject(RootName).transform;
            art.SetParent(ruins, false);
            Module("floor_tile_small_decorated", "火种承印", new Vector3(0, .22f, 52),
                new Vector3(1.72f, .1f, 1.72f), 45, ember);

            // A drowned arcade rises behind the end rail. Its open centre frames the existing flame.
            Module("wall_doorway", "祈火主龛", new Vector3(0, -1.3f, 58.5f), new Vector3(7.4f, 6.4f, 1.3f), 0, stone);
            foreach (int side in new[] { -1, 1 })
            {
                float height = side < 0 ? 4.7f : 3.1f;
                Module("wall_doorway", "侧翼沉拱", new Vector3(side * 5.5f, -1.9f, 58.2f),
                    new Vector3(4.3f, height, 1.1f), side * 14, dark);
                Module("pillar_decorated", "祈火壁端柱", new Vector3(side * 7.65f, -.9f, 57.7f),
                    new Vector3(1.35f, height + .6f, 1.4f), side * 12, stone);
                // Flush reliefs at the two flanks, outside the orbiting stones.
                Module("floor_tile_small_decorated", "奉火铜纹", new Vector3(side * 4.4f, .06f, 52),
                    new Vector3(1.06f, .07f, 1.06f), 45, bronze);
                Module("floor_tile_small_broken_A", "残缺供石", new Vector3(side * 4.05f, .06f, 50.1f),
                    new Vector3(.83f, .07f, 1.15f), side * 26, dark);
            }
            var crest = Module("floor_tile_small_decorated", "龛顶铜印", new Vector3(0, 4.4f, 57.77f),
                new Vector3(1.05f, .13f, 1.05f), 0, bronze);
            crest.rotation = Quaternion.Euler(-90, 0, 45);
            PrefabUtility.RecordPrefabInstancePropertyModifications(crest);

            Transform Module(string model, string label, Vector3 position, Vector3 size, float yaw, Material material)
            {
                var instance = KayKitEntranceSetup.Place(art, model, position, size);
                instance.name = label;
                instance.rotation = Quaternion.Euler(0, yaw, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.Find("VisualRoot"));
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.sharedMaterial = material;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                return instance;
            }
        }

        private static Material LoadMaterial(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/SunkenBridge/" + name + ".mat") ??
            throw new InvalidOperationException("Missing altar material " + name);
    }
}

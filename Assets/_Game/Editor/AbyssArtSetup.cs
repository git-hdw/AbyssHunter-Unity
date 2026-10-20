using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.Character;
using AbyssHunter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    public static class AbyssArtSetup
    {
        private const string Folder = "Assets/_Game/Art/Abyss/";
        private static Transform art;
        private static Mesh bevel, flameMesh;
        private static Material stone, dark, metal, cloth, flame, purple, mist, wet, soot;
        private static Material[] stoneVariants;

        [MenuItem("Tools/深渊猎手/环境/验证微光与环境表现")]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            SessionState.SetBool("AbyssHunter.ArtReview", true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/深渊猎手/环境/应用深渊与微光美术")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            PrepareAssets();
            var ruins = GameObject.Find("Sunken Ruins");
            var old = ruins.transform.Find("深渊环境美术");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            art = new GameObject("深渊环境美术").transform; art.SetParent(ruins.transform);
            SurfacePass(ruins);
            Backdrop();
            FallingDebris();
            Entrance();
            Courtyard();
            Sanctum();
            Details();
            Lighting();
            var hunter = Object.FindFirstObjectByType<PlayerMovement>();
            var lantern = BuildLantern(hunter);
            WireAtmosphere(ruins, lantern);
            CombineScenery(ruins);
            var camera = Object.FindFirstObjectByType<TopDownCamera>();
            var cameraData = new SerializedObject(camera); cameraData.FindProperty("environmentalFraming").boolValue = true; cameraData.ApplyModifiedPropertiesWithoutUndo();
            if (ruins.GetComponent<AbyssSoundscape>() == null) ruins.AddComponent<AbyssSoundscape>();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/abyss-art-setup.txt", "DONE | 护火灯、三处遗迹、石材与金属、深渊层雾、镜头遮挡、空间音效已配置。原有行走碰撞与导航保留。\n");
        }

        private static void PrepareAssets()
        {
            bevel = MeshAsset("倒角石块", BevelCube);
            flameMesh = MeshAsset("微光火苗", FlameMesh);
            var grain = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "石材细纹.asset");
            if (grain == null)
            {
                grain = new Texture2D(128, 128, TextureFormat.RGBA32, true) { name = "石材细纹", wrapMode = TextureWrapMode.Repeat };
                var pixels = new Color[128 * 128];
                for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float n = Mathf.PerlinNoise(x * .08f, y * .08f) * .18f + Mathf.PerlinNoise(x * .7f, y * .7f) * .09f;
                    float seam = Mathf.Abs(Mathf.Sin(x * .14f + Mathf.PerlinNoise(x * .03f, y * .03f) * 8)) < .035f ? .06f : 0;
                    pixels[y * 128 + x] = new Color(.72f + n - seam, .75f + n - seam, .77f + n - seam);
                }
                grain.SetPixels(pixels); grain.Apply(); AssetDatabase.CreateAsset(grain, Folder + "石材细纹.asset");
            }
            stone = Mat("潮蚀玄武岩", new Color(.25f, .31f, .34f), .15f, 0, grain);
            stoneVariants = new Material[5];
            for (int i = 0; i < 5; i++) stoneVariants[i] = Mat("旧石板" + i, new Color(.23f + i * .018f, .29f + i * .016f, .31f + i * .014f), .12f + i * .035f, 0, grain);
            dark = Mat("深处岩壁", new Color(.055f, .095f, .13f), .08f, 0, grain);
            metal = Mat("磨损青铜", new Color(.31f, .23f, .12f), .48f, .65f);
            cloth = Mat("褪色披布", new Color(.19f, .23f, .21f), .06f);
            soot = Mat("裂纹积灰", new Color(.045f, .065f, .07f), .05f);
            wet = Mat("浅湿石面", new Color(.12f, .18f, .20f), .72f, .1f, grain);
            flame = Mat("暖金微光", new Color(1, .58f, .16f), .1f, 0, null, new Color(2.4f, .95f, .12f));
            purple = Mat("侵蚀紫晶", new Color(.26f, .12f, .35f), .4f, .15f, null, new Color(.6f, .12f, .95f));
            mist = AssetDatabase.LoadAssetAtPath<Material>(Folder + "裂隙薄雾.mat");
            if (mist == null) { mist = new Material(Shader.Find("AbyssHunter/DepthMist")); AssetDatabase.CreateAsset(mist, Folder + "裂隙薄雾.mat"); }
            mist.SetColor("_BaseColor", new Color(.12f, .2f, .27f, .36f)); EditorUtility.SetDirty(mist);
        }
        private static Material Mat(string name, Color color, float smooth, float metallic = 0, Texture texture = null, Color emission = default)
        {
            string path = Folder + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", smooth); mat.SetFloat("_Metallic", metallic);
            mat.SetTexture("_BaseMap", texture);
            if (emission.maxColorComponent > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission); }
            EditorUtility.SetDirty(mat); return mat;
        }
        private static Mesh MeshAsset(string name, Func<Mesh> create)
        {
            string path = Folder + name + ".asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = create(); mesh.name = name; AssetDatabase.CreateAsset(mesh, path); }
            return mesh;
        }
        private static void SurfacePass(GameObject ruins)
        {
            int tile = 0;
            foreach (var renderer in ruins.GetComponentsInChildren<MeshRenderer>().Where(r => !r.transform.IsChildOf(art)))
            {
                renderer.enabled = true;
                string name = renderer.name;
                if (name is "Worn flagstone" or "Repaired bridge slab") renderer.sharedMaterial = stoneVariants[tile++ % 5];
                else if (name.Contains("pillar", StringComparison.OrdinalIgnoreCase) || name.Contains("Arch") || name.Contains("arch") || name.Contains("rim") || name.Contains("balustrade")) renderer.sharedMaterial = stone;
                else if (name.Contains("foundation") || name.Contains("Distant")) renderer.sharedMaterial = dark;
                else if (name.Contains("fissure") || name.Contains("abyss core")) renderer.sharedMaterial = purple;
                if (renderer.GetComponent<MeshFilter>() is { } filter && filter.sharedMesh != null && filter.sharedMesh.name == "Cube") filter.sharedMesh = bevel;
                if (name.Contains("arch") || name.Contains("Arch") || name is "Fractured pillar" or "Pillar crown")
                    if (renderer.GetComponent<AbyssOccluder>() == null) renderer.gameObject.AddComponent<AbyssOccluder>();
            }
            // Existing colliders and navigation remain the authoritative walking surface.
        }
        private static void Backdrop()
        {
            var random = new System.Random(613);
            for (int layer = 0; layer < 3; layer++) for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 8; i++)
            {
                float x = side * (17 + layer * 10 + (float)random.NextDouble() * 5);
                float y = -11 - layer * 10;
                var rock = Shape(PrimitiveType.Cube, "深渊岩脊", new Vector3(x, y, -15 + i * 12), new Vector3(4 + layer, 8 + i % 3 * 2, 5), dark);
                rock.transform.rotation = Quaternion.Euler(side * 8, i * 27, side * (12 + i % 3 * 8));
                if (i % 3 == 0)
                {
                    Shape(PrimitiveType.Cube, "沉没楼层", new Vector3(x, y + 2, -15 + i * 12), new Vector3(8, .45f, 7), stone);
                    Shape(PrimitiveType.Cube, "深处残柱", new Vector3(x + side * 2, y + 4, -15 + i * 12), new Vector3(.8f, 5, .8f), stone);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                var fog = Shape(PrimitiveType.Quad, "深渊层雾", new Vector3((i % 2 == 0 ? -1 : 1) * 9, -3.6f - i * 4.5f, 24), new Vector3(75 + i * 7, 115, 1), mist);
                fog.transform.rotation = Quaternion.Euler(90, 0, i * 11);
                fog.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (float z in new[] {12f, 36f})
            {
                Chain(new Vector3(-6, -.6f, z - 4), new Vector3(-12, -13, z + 4), 14);
                Chain(new Vector3(6, -.6f, z - 4), new Vector3(14, -18, z + 5), 17);
                Shape(PrimitiveType.Cube, "桥下断裂拱座", new Vector3(-6.5f, -5, z), new Vector3(2.5f, 9, 3), stone).transform.rotation = Quaternion.Euler(0, 0, -22);
            }
        }
        private static void FallingDebris()
        {
            foreach (int side in new[] {-1, 1})
            {
                var go = new GameObject("深渊缓落碎屑"); go.transform.SetParent(art, false);
                go.transform.position = new Vector3(side * 12, -2, 24);
                var particles = go.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main; main.startLifetime = 8; main.startSpeed = 0;
                main.startSize = new ParticleSystem.MinMaxCurve(.09f, .22f); main.maxParticles = 12;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = particles.emission; emission.rateOverTime = .8f;
                var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(3, 1, 70);
                var velocity = particles.velocityOverLifetime; velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World; velocity.y = -.65f; velocity.x = side * .12f;
                var rotation = particles.rotationOverLifetime; rotation.enabled = true; rotation.z = .3f;
                var renderer = particles.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = bevel; renderer.sharedMaterial = stone; renderer.shadowCastingMode = ShadowCastingMode.Off;
                particles.Play();
            }
        }
        private static void Entrance()
        {
            Shape(PrimitiveType.Cube, "遗弃铺毯", new Vector3(-6, .03f, -3), new Vector3(2.2f, .04f, 2.7f), cloth).transform.Rotate(0, -16, 0);
            Shape(PrimitiveType.Cube, "折断帐篷杆", new Vector3(-7, .85f, -2.7f), new Vector3(.13f, 2, .13f), metal).transform.Rotate(0, 0, -26);
            Shape(PrimitiveType.Cube, "营地石凳", new Vector3(-6, .35f, -.8f), new Vector3(2, .65f, .7f), stone);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3;
                Shape(PrimitiveType.Cube, "熄灭篝火石", new Vector3(-5.8f + Mathf.Cos(angle) * .65f, .15f, -4.8f + Mathf.Sin(angle) * .65f), new Vector3(.4f, .3f, .3f), stone);
            }
            Shape(PrimitiveType.Cylinder, "冷灰", new Vector3(-5.8f, .035f, -4.8f), new Vector3(1.1f, .025f, 1.1f), soot);
            Shape(PrimitiveType.Cube, "倒伏铭碑", new Vector3(6.4f, .35f, -3), new Vector3(1.1f, .6f, 2.7f), stone).transform.Rotate(0, 24, 8);
            for (int i = 0; i < 4; i++) Shape(PrimitiveType.Cube, "铭碑刻痕", new Vector3(6.3f, .68f, -3.7f + i * .37f), new Vector3(.65f, .02f, .035f), metal).transform.Rotate(0, 24, 0);
            DeadLamp(new Vector3(-6.7f, .2f, -2));
        }
        private static void Courtyard()
        {
            Shape(PrimitiveType.Cube, "守望者断座", new Vector3(-6, .4f, 26), new Vector3(3.1f, .8f, 3.2f), stone);
            Shape(PrimitiveType.Cube, "倒伏石像躯干", new Vector3(-6, 1.1f, 24.6f), new Vector3(1.7f, 1.7f, 3.7f), stone).transform.rotation = Quaternion.Euler(0, -12, 8);
            Shape(PrimitiveType.Sphere, "石像残首", new Vector3(-6.2f, 1.2f, 21.8f), new Vector3(1.8f, 2, 1.5f), stone);
            Shape(PrimitiveType.Cube, "石像面罩", new Vector3(-6.2f, 1.6f, 21.15f), new Vector3(1.3f, .75f, .15f), metal).transform.Rotate(12, 0, 0);
            Chain(new Vector3(7, 4.7f, 20), new Vector3(6, .2f, 28), 20);
            Chain(new Vector3(-7.5f, 4.2f, 26), new Vector3(-5.8f, 1.4f, 24), 10);
            Shape(PrimitiveType.Cube, "破损垂布", new Vector3(7.6f, 2.5f, 26), new Vector3(.07f, 2.8f, 1.25f), cloth).transform.Rotate(0, 0, 12);
            for (int i = 0; i < 5; i++) Shape(PrimitiveType.Cube, "庭院侵蚀纹", new Vector3(6.9f - i * .18f, .035f, 23 + i * .6f), new Vector3(.06f, .035f, .75f), purple).transform.Rotate(0, 25 + i * 16, 0);
        }
        private static void Sanctum()
        {
            foreach (int side in new[] {-1, 1})
            {
                Shape(PrimitiveType.Cube, "祭坛倾斜巨柱", new Vector3(side * 10.4f, 3.5f, 53), new Vector3(1.4f, 10, 1.8f), stone, true).transform.Rotate(0, side * 15, side * 20);
                Chain(new Vector3(side * 5, .2f, 54), new Vector3(side * 14, -12, 58), 18);
                for (int i = 0; i < 5; i++)
                {
                    var shard = Shape(PrimitiveType.Cube, "裂隙晶簇", new Vector3(side * (7 + i * .4f), -.2f + i * .1f, 45 + i * 2), new Vector3(.3f, .8f + i * .23f, .35f), purple);
                    shard.transform.Rotate(12 + i * 9, i * 25, side * 25);
                }
            }
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2 / 9;
                var fragment = Shape(PrimitiveType.Cube, "祭坛悬石", new Vector3(Mathf.Cos(a) * 3.2f, 1.7f + i % 3 * .55f, 52 + Mathf.Sin(a) * 2.1f), new Vector3(.4f, .65f, .4f), stone);
                fragment.transform.rotation = Quaternion.Euler(i * 23, i * 41, i * 17);
            }
            Shape(PrimitiveType.Cylinder, "祭坛外环", new Vector3(0, .08f, 52), new Vector3(4.8f, .08f, 4.8f), stone);
            Shape(PrimitiveType.Cylinder, "祭坛铜盘", new Vector3(0, .18f, 52), new Vector3(3.2f, .03f, 3.2f), metal);
        }
        private static void Details()
        {
            var random = new System.Random(144);
            for (int area = 0; area < 3; area++) for (int i = 0; i < 30; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (6.5f + (float)random.NextDouble() * 2);
                float z = area * 24 - 7 + (float)random.NextDouble() * 14;
                var rock = Shape(PrimitiveType.Cube, "边缘碎石", new Vector3(x, .08f, z), new Vector3(.2f + (float)random.NextDouble() * .45f, .15f + (float)random.NextDouble() * .25f, .25f + (float)random.NextDouble() * .4f), stoneVariants[i % 5]);
                rock.transform.Rotate(i * 11, i * 37, i * 8);
                if (i % 6 == 0) Shape(PrimitiveType.Cube, "积水湿痕", new Vector3(x - side, .016f, z), new Vector3(1.2f, .018f, .7f), wet).transform.Rotate(0, i * 17, 0);
            }
            for (int area = 0; area < 3; area++) foreach (int side in new[] {-1, 1})
                for (int i = 0; i < 4; i++) Shape(PrimitiveType.Cube, "石板裂纹", new Vector3(side * (4 + i * .45f), .017f, area * 24 + 4 + i * .35f), new Vector3(.025f, .02f, .7f), soot).transform.Rotate(0, side * (30 + i * 15), 0);
        }
        private static void Lighting()
        {
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional);
            sun.color = new Color(.56f, .7f, .9f); sun.intensity = .85f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38, -38, 0);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.19f, .25f, .33f);
            RenderSettings.ambientEquatorColor = new Color(.095f, .13f, .17f);
            RenderSettings.ambientGroundColor = new Color(.035f, .055f, .075f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.035f, .065f, .095f); RenderSettings.fogDensity = .009f;
            Camera.main.backgroundColor = new Color(.012f, .025f, .044f);
            foreach (var lamp in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.name == "Guiding lantern")) { lamp.intensity = .28f; lamp.range = 4; lamp.color = new Color(.25f, .48f, .61f); lamp.shadows = LightShadows.None; }
            for (int area = 0; area < 3; area++)
            {
                var light = new GameObject("遗迹侧缘冷光").AddComponent<Light>(); light.transform.SetParent(art); light.transform.position = new Vector3(-7, 5, area * 24);
                light.type = LightType.Point; light.color = new Color(.25f, .43f, .62f); light.intensity = 1.3f; light.range = 12; light.shadows = LightShadows.None;
            }
            var coreLight = GameObject.Find("Core glow").GetComponent<Light>(); coreLight.color = new Color(.47f, .2f, .75f); coreLight.intensity = 2.5f; coreLight.range = 6;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (profile.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(.32f); bloom.threshold.Override(1.1f); EditorUtility.SetDirty(bloom); }
            if (profile.TryGet<Vignette>(out var vignette)) { vignette.intensity.Override(.15f); EditorUtility.SetDirty(vignette); }
        }
        private static LastLight BuildLantern(PlayerMovement hunter)
        {
            var old = hunter.GetComponentInChildren<LastLight>(); if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject("最后的微光 · 护火灯");
            go.transform.position = hunter.transform.position + new Vector3(-.48f, -.1f, -.32f);
            var chest = hunter.GetComponentsInChildren<Transform>().First(t => t.name == "chest");
            go.transform.SetParent(chest, true);
            Transform saved = art; art = go.transform;
            ShapeLocal(PrimitiveType.Cylinder, "灯座", new Vector3(0, -.23f, 0), new Vector3(.35f, .045f, .35f), metal);
            ShapeLocal(PrimitiveType.Cylinder, "灯罩", new Vector3(0, .22f, 0), new Vector3(.37f, .045f, .37f), metal);
            foreach (int x in new[] {-1, 1}) foreach (int z in new[] {-1, 1}) ShapeLocal(PrimitiveType.Cube, "护火金属框", new Vector3(x * .12f, 0, z * .12f), new Vector3(.025f, .44f, .025f), metal);
            ShapeLocal(PrimitiveType.Cube, "灯提柄", new Vector3(0, .34f, 0), new Vector3(.035f, .18f, .035f), metal);
            var fire = ShapeLocal(PrimitiveType.Sphere, "微光火苗", Vector3.zero, new Vector3(.13f, .35f, .13f), flame);
            fire.GetComponent<MeshFilter>().sharedMesh = flameMesh;
            var glow = new GameObject("微光照明").AddComponent<Light>(); glow.transform.SetParent(go.transform, false); glow.transform.localPosition = new Vector3(0, .05f, -.08f);
            glow.type = LightType.Point; glow.color = new Color(1, .54f, .18f); glow.intensity = 1.4f; glow.range = 3.6f; glow.shadows = LightShadows.None;
            var sparks = Sparks(go.transform);
            var script = go.AddComponent<LastLight>(); Set(script, "flame", fire.transform); Set(script, "glow", glow); Set(script, "sparks", sparks);
            art = saved; return script;
        }
        private static ParticleSystem Sparks(Transform parent)
        {
            var go = new GameObject("漂散余烬"); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 1.2f; main.startSpeed = .13f; main.startSize = .022f; main.startColor = new Color(1, .65f, .2f, .7f); main.maxParticles = 24; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.rateOverTime = 2;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .05f;
            var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.y = .15f;
            var color = ps.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(new Color(1, .66f, .2f), 0), new GradientColorKey(new Color(.9f, .25f, .07f), 1)}, new[] {new GradientAlphaKey(.8f, 0), new GradientAlphaKey(0, 1)}); color.color = gradient;
            var sparkMat = AssetDatabase.LoadAssetAtPath<Material>(Folder + "余烬.mat");
            if (sparkMat == null) { sparkMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); sparkMat.SetColor("_BaseColor", new Color(1, .6f, .12f)); AssetDatabase.CreateAsset(sparkMat, Folder + "余烬.mat"); }
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkMat;
            ps.Play(); return ps;
        }
        private static void WireAtmosphere(GameObject ruins, LastLight lantern)
        {
            var mission = ruins.GetComponent<AbyssExpedition>(); var data = new SerializedObject(mission);
            var atmosphere = ruins.GetComponent<AbyssAtmosphere>(); if (atmosphere == null) atmosphere = ruins.AddComponent<AbyssAtmosphere>();
            var target = new SerializedObject(atmosphere);
            var fragments = art.GetComponentsInChildren<Transform>().Where(t => t.name == "祭坛悬石").ToArray();
            var fragmentProperty = target.FindProperty("floatingFragments"); fragmentProperty.arraySize = fragments.Length;
            for (int i = 0; i < fragments.Length; i++) fragmentProperty.GetArrayElementAtIndex(i).objectReferenceValue = fragments[i];
            foreach (var pair in new[] {("returnLights", "routeLights"), ("returnRunes", "routeRunes")})
            {
                var from = data.FindProperty(pair.Item1); var to = target.FindProperty(pair.Item2); to.arraySize = from.arraySize;
                for (int i = 0; i < from.arraySize; i++) to.GetArrayElementAtIndex(i).objectReferenceValue = from.GetArrayElementAtIndex(i).objectReferenceValue;
            }
            foreach (string field in new[] {"altarLight", "beaconLight", "awakenedRune"}) target.FindProperty(field).objectReferenceValue = data.FindProperty(field).objectReferenceValue;
            var core = ((GameObject)data.FindProperty("coreVisual").objectReferenceValue).transform;
            core.GetComponent<MeshFilter>().sharedMesh = flameMesh; core.localScale = new Vector3(.65f, 1.2f, .65f);
            target.FindProperty("core").objectReferenceValue = core; target.FindProperty("lantern").objectReferenceValue = lantern;
            var ember = Shape(PrimitiveType.Sphere, "回收核心的火种", core.position, Vector3.one * .2f, flame);
            target.FindProperty("travellingEmber").objectReferenceValue = ember.transform; ember.SetActive(false);
            var beacon = (Transform)data.FindProperty("beacon").objectReferenceValue;
            foreach (var oldLamp in ruins.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name is "Rune lantern" or "Rune lantern base"))
            {
                Vector3 delta = oldLamp.transform.position - beacon.position; delta.y = 0;
                if (delta.sqrMagnitude < .1f) oldLamp.gameObject.SetActive(false);
            }
            Shape(PrimitiveType.Cylinder, "归途火盆", beacon.position + Vector3.up * .65f, new Vector3(.9f, .12f, .9f), metal);
            var beaconFire = Shape(PrimitiveType.Sphere, "归途信标火焰", beacon.position + Vector3.up * 1.12f, new Vector3(.42f, .85f, .42f), flame);
            beaconFire.GetComponent<MeshFilter>().sharedMesh = flameMesh;
            target.FindProperty("beaconFlame").objectReferenceValue = beaconFire.transform; target.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void DeadLamp(Vector3 p)
        {
            Shape(PrimitiveType.Cylinder, "遗弃灯座", p, new Vector3(.4f, .1f, .4f), metal);
            Shape(PrimitiveType.Cube, "熄灭灯芯", p + Vector3.up * .2f, new Vector3(.09f, .3f, .09f), soot);
        }
        private static void CombineScenery(GameObject ruins)
        {
            // Combine only stationary opaque geometry, in spatial chunks. Interactable surfaces,
            // flames, fog, runes and camera occluders remain independent renderers.
            string[] stationary = {"Worn flagstone", "Repaired bridge slab", "Suspended foundation", "Distant sunken tower", "Platform rim", "Broken balustrade", "Bridge guardrail", "Entrance end", "Sanctum end", "Rune lantern base", "Rune lantern", "Pillar footing", "Core plinth", "Beacon plinth", "Corrupted fissure"};
            var filters = ruins.GetComponentsInChildren<MeshFilter>().Where(f =>
            {
                var r = f.GetComponent<MeshRenderer>();
                if (r == null || !r.enabled || r.sharedMaterial == null || f.GetComponent<AbyssOccluder>() != null) return false;
                if (f.name is "回收核心的火种" or "归途信标火焰" or "祭坛悬石" || r.sharedMaterial == mist) return false;
                return f.transform.IsChildOf(art) || stationary.Contains(f.name);
            }).ToArray();
            var groups = filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial.name + "_" + Mathf.FloorToInt(f.transform.position.x / 20) + "_" + Mathf.FloorToInt((f.transform.position.z + 10) / 24));
            foreach (var group in groups)
            {
                var items = group.ToArray(); if (items.Length < 2) continue;
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(items.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = art.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray());
                mesh.RecalculateBounds(); string path = Folder + "环境合批_" + group.Key + ".asset";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved == null) { mesh.name = "环境合批_" + group.Key; AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
                else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
                var go = new GameObject("静态环境 · " + group.Key); go.transform.SetParent(art, false);
                go.AddComponent<MeshFilter>().sharedMesh = saved;
                go.AddComponent<MeshRenderer>().sharedMaterial = items[0].GetComponent<MeshRenderer>().sharedMaterial;
                foreach (var item in items) item.GetComponent<MeshRenderer>().enabled = false;
            }
        }
        private static void Chain(Vector3 start, Vector3 end, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1); Vector3 p = Vector3.Lerp(start, end, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * .65f;
                var link = Shape(PrimitiveType.Cube, "残破锁链", p, new Vector3(.11f, .32f, .1f), metal);
                link.transform.rotation = Quaternion.FromToRotation(Vector3.up, end - start) * Quaternion.Euler(0, 0, i % 2 == 0 ? 25 : -25);
            }
        }
        private static GameObject ShapeLocal(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = Shape(type, name, Vector3.zero, scale, material); go.transform.localPosition = position; go.transform.localRotation = Quaternion.identity; return go;
        }
        private static GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, bool occluder = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(art, false); go.transform.position = position; go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = material;
            if (type == PrimitiveType.Cube) go.GetComponent<MeshFilter>().sharedMesh = bevel;
            if (occluder) go.AddComponent<AbyssOccluder>(); return go;
        }
        private static void Set(Object obj, string field, Object value) { var s = new SerializedObject(obj); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }

        private static Mesh FlameMesh()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            float[] heights = {-.5f, -.3f, 0, .25f, .5f}; float[] radii = {0, .43f, .5f, .28f, 0};
            for (int ring = 0; ring < heights.Length; ring++) for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4; vertices.Add(new Vector3(Mathf.Cos(a) * radii[ring] + heights[ring] * heights[ring] * .18f, heights[ring], Mathf.Sin(a) * radii[ring]));
                if (ring < 4) { int b = ring * 8 + i, n = ring * 8 + (i + 1) % 8; triangles.AddRange(new[] {b, b + 8, n, n, b + 8, n + 8}); }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh BevelCube()
        {
            const float inset = .455f;
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            void Face(params Vector3[] points)
            {
                Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
                if (Vector3.Dot(normal, points.Aggregate(Vector3.zero, (a, b) => a + b)) < 0) Array.Reverse(points);
                int start = vertices.Count; vertices.AddRange(points);
                foreach (var p in points) uv.Add(Mathf.Abs(normal.y) > Mathf.Abs(normal.z) ? new Vector2(p.x + .5f, p.z + .5f) : new Vector2(p.x + p.z + .5f, p.y + .5f));
                for (int j = 1; j < points.Length - 1; j++) triangles.AddRange(new[] {start, start + j, start + j + 1});
            }
            for (int axis = 0; axis < 3; axis++) foreach (int sign in new[] {-1, 1})
            {
                var points = new Vector3[4]; int a = (axis + 1) % 3, b = (axis + 2) % 3;
                for (int j = 0; j < 4; j++) { points[j][axis] = sign * .5f; points[j][a] = (j < 2 ? -1 : 1) * inset; points[j][b] = (j == 0 || j == 3 ? -1 : 1) * inset; }
                Face(points);
            }
            for (int c = 0; c < 3; c++) foreach (int sa in new[] {-1, 1}) foreach (int sb in new[] {-1, 1})
            {
                int a = (c + 1) % 3, b = (c + 2) % 3; var p = new Vector3[4];
                for (int j = 0; j < 4; j++) { p[j][c] = (j == 0 || j == 3 ? -1 : 1) * inset; p[j][a] = sa * (j < 2 ? .5f : inset); p[j][b] = sb * (j < 2 ? inset : .5f); }
                Face(p);
            }
            foreach (int x in new[] {-1, 1}) foreach (int y in new[] {-1, 1}) foreach (int z in new[] {-1, 1})
                Face(new Vector3(x * .5f, y * inset, z * inset), new Vector3(x * inset, y * .5f, z * inset), new Vector3(x * inset, y * inset, z * .5f));
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}

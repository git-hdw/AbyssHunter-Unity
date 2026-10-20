using System;
using System.IO;
using System.Linq;
using System.Text;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    // Only replaces visual children; gameplay roots, colliders and Addressables GUIDs stay stable.
    [InitializeOnLoad]
    public static class CharacterModelSetup
    {
        private const string Adventurers = "Assets/KayKit/Characters/Adventurers/";
        private const string Skeletons = "Assets/KayKit/Characters/Skeletons/";
        private const string Visuals = "Assets/_Game/Prefabs/Characters/";
        static CharacterModelSetup()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode) return;
                string previous = SessionState.GetString("AbyssHunter.ModelReviewScene", "");
                SessionState.EraseString("AbyssHunter.ModelReviewScene");
                if (!string.IsNullOrEmpty(previous)) EditorApplication.delayCall += () => EditorSceneManager.OpenScene(previous);
            };
        }

        [MenuItem("Tools/深渊猎手/角色/验证动画并导出对比图")]
        public static void Review()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetString("AbyssHunter.ModelReviewScene", EditorSceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            SessionState.SetBool("AbyssHunter.ModelTest", true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/深渊猎手/角色/配置猎手与骷髅模型")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Visuals);
            AssetDatabase.Refresh();
            foreach (string path in new[] { Adventurers + "Rogue_Hooded.fbx", Skeletons + "Skeleton_Minion.fbx", Skeletons + "Skeleton_Warrior.fbx" })
                ImportCharacter(path);
            var hunter = Material("猎手", Adventurers + "rogue_texture.png");
            var bone = Material("骷髅", Skeletons + "skeleton_texture.png");
            var sword = Material("猎手长剑", Adventurers + "knight_texture.png");
            CreateVisual("HunterVisual", Adventurers + "Rogue_Hooded.fbx", "PlayerAnimator", hunter, 1,
                Adventurers + "sword_1handed.fbx", sword, false);
            CreateVisual("SkeletonVisual", Skeletons + "Skeleton_Minion.fbx", "EnemyAnimator", bone, 1,
                Skeletons + "Skeleton_Blade.fbx", bone, false);
            CreateVisual("GuardianVisual", Skeletons + "Skeleton_Warrior.fbx", "HeavyAnimator", bone, 1.25f,
                null, null, true);
            ReplaceEnemy("Enemy", "SkeletonVisual");
            ReplaceEnemy("HeavyEnemy", "GuardianVisual");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var player = Object.FindFirstObjectByType<PlayerAnimationController>();
            ReplaceVisual(player.gameObject, "HunterVisual", player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate();
        }

        private static void ImportCharacter(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/KayKit/Characters/Animations/Avatars/Rig_Medium_Avatar.asset");
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            // These FBX files contain skin/rig only, so Unity does not add an Animator automatically.
            if (importer.sourceAvatar == null || !importer.sourceAvatar.isValid || !importer.sourceAvatar.isHuman
                || !model.GetComponentsInChildren<Transform>().Any(t => t.name == "Rig_Medium"))
                throw new InvalidOperationException("角色 Humanoid Avatar 无效：" + path);
        }

        private static Material Material(string name, string texturePath)
        {
            string path = Visuals + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", .2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreateVisual(string name, string modelPath, string controller, Material material,
            float scale, string weaponPath, Material weaponMaterial, bool hammer)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            try
            {
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = name;
                model.transform.localScale = Vector3.one * scale;
                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.avatar = ((ModelImporter)AssetImporter.GetAtPath(modelPath)).sourceAvatar;
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/_Game/Animations/{controller}.controller");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                model.AddComponent<AttackAnimationEventRelay>();
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                var hand = model.GetComponentsInChildren<Transform>().First(t => t.name == "handslot.r");
                if (weaponPath != null)
                {
                    var weapon = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath), hand);
                    weapon.name = "右手武器";
                    weapon.transform.localPosition = Vector3.zero;
                    weapon.transform.localRotation = Quaternion.identity;
                    foreach (var renderer in weapon.GetComponentsInChildren<Renderer>())
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => weaponMaterial).ToArray();
                }
                if (hammer)
                {
                    // Same contact silhouette as the existing heavy attack, now held by the skeletal guardian.
                    Box(hand, "Hammer shaft", new Vector3(0, .4f, 0), new Vector3(.1f, 1.2f, .1f), "MAT_HeavyGrip");
                    Box(hand, "Hammer head", new Vector3(0, 1, 0), new Vector3(.55f, .4f, .85f), "MAT_HeavySteel");
                    Box(hand, "重锤端箍左", new Vector3(0, 1, -.43f), new Vector3(.6f, .46f, .08f), "MAT_HeavyArmor");
                    Box(hand, "重锤端箍右", new Vector3(0, 1, .43f), new Vector3(.6f, .46f, .08f), "MAT_HeavyArmor");
                }
                PrefabUtility.SaveAsPrefabAsset(model, Visuals + name + ".prefab");
            }
            finally { Object.DestroyImmediate(model); }
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, string material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Game/Materials/{material}.mat");
        }

        private static void ReplaceEnemy(string name, string visual)
        {
            string path = $"Assets/_Game/Prefabs/{name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ReplaceVisual(root, visual, root.GetComponent<EnemyAnimationController>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ReplaceVisual(GameObject root, string name, Component animationController)
        {
            var old = root.GetComponentInChildren<Animator>();
            var position = old.transform.localPosition;
            var rotation = old.transform.localRotation;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Visuals + name + ".prefab"), root.transform);
            model.transform.localPosition = position;
            model.transform.localRotation = rotation;
            var data = new SerializedObject(animationController);
            data.FindProperty("animator").objectReferenceValue = model.GetComponent<Animator>();
            data.ApplyModifiedPropertiesWithoutUndo();
            var lantern = old.GetComponentInChildren<AbyssHunter.World.LastLight>();
            if (lantern != null)
            {
                var chest = model.GetComponentsInChildren<Transform>().First(t => t.name == "chest");
                lantern.transform.SetParent(chest, false);
            }
            Object.DestroyImmediate(old.gameObject);
        }

        [MenuItem("Tools/深渊猎手/角色/检查模型引用")]
        public static void Validate()
        {
            var report = new StringBuilder();
            foreach (string name in new[] { "HunterVisual", "SkeletonVisual", "GuardianVisual" })
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>(Visuals + name + ".prefab");
                var animator = visual.GetComponent<Animator>();
                if (!animator.avatar.isHuman || !animator.avatar.isValid) throw new InvalidOperationException(name + " Avatar invalid");
                if (visual.GetComponent<AttackAnimationEventRelay>() == null) throw new InvalidOperationException(name + " event relay missing");
                report.AppendLine($"PASS | {name}: Humanoid Avatar, controller, damage event relay");
                foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (renderer.bones.Any(b => b == null) || renderer.sharedMesh == null) throw new InvalidOperationException(name + " skin binding invalid");
                    report.AppendLine($"  {renderer.name}: {renderer.sharedMesh.vertexCount} vertices, {renderer.bones.Length} bones, bounds={renderer.localBounds}");
                }
                report.AppendLine(string.Join(", ", visual.GetComponentsInChildren<Transform>().Select(t => t.name)));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/character-model-validation.txt", report.ToString());
        }
    }
}

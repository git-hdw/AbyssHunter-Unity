using System.Linq;
using AbyssHunter.Enemy;
using UnityEditor.Animations;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AbyssHunter.EditorTools
{
    public static class RiftCasterSetup
    {
        private const string Prefab = "Assets/_Game/Prefabs/RiftCaster.prefab";

        [MenuItem("Tools/深渊猎手/关卡/配置第二关烬火法师")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AbyssDepths.unity");
            Configure(Object.FindFirstObjectByType<EnemyWaveSpawner>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        internal static void Configure(EnemyWaveSpawner waves)
        {
            if (waves.gameObject.scene.name != "AbyssDepths") throw new System.InvalidOperationException("Expected AbyssDepths");
            EnsureFireMage();
            string guid = AssetDatabase.AssetPathToGUID(Prefab);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings.FindAssetEntry(guid) == null)
            {
                var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.address = "RiftCaster";
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
            }
            var data = new SerializedObject(waves);
            var poolProperty = data.FindProperty("casterPool");
            var pool = poolProperty.objectReferenceValue as EnemyPool;
            if (pool == null)
            {
                pool = new GameObject("烬火法师池").AddComponent<EnemyPool>();
                pool.transform.SetParent(waves.transform, false);
                poolProperty.objectReferenceValue = pool;
            }
            pool.gameObject.name = "烬火法师池";
            var pd = new SerializedObject(pool);
            pd.FindProperty("enemyPrefabReference.m_AssetGUID").stringValue = guid;
            pd.FindProperty("initialSize").intValue = 1;
            pd.ApplyModifiedPropertiesWithoutUndo();
            var encounters = data.FindProperty("encounters");
            for (int i = 1; i <= 2; i++)
            {
                var e = encounters.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("title").stringValue = i == 1 ? "悬浮守望台 · 重锤与烬火法师" : "中央祭坛 · 焰杖守核";
                e.FindPropertyRelative("normalCount").intValue = i - 1;
                e.FindPropertyRelative("casterCount").intValue = 1;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private const string Visual = "CinderMageVisual";
        private const string AnimationFolder = "Assets/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Ranged/";

        private static void EnsureFireMage()
        {
            var previous = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if (previous != null && previous.transform.Find(Visual) != null) return;

            const string models = "Assets/KayKit/Characters/Skeletons/";
            var bones = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Prefabs/Characters/骷髅.mat");
            var controller = SpellController();
            string visualPath = "Assets/_Game/Prefabs/Characters/" + Visual + ".prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(visualPath))
                CharacterModelSetup.CreateVisual(Visual, models + "Skeleton_Mage.fbx", controller.name, bones,
                    1, models + "Skeleton_Staff.fbx", bones, false);

            const string spellMaterial = "Assets/_Game/Materials/MAT_CinderSpell.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(spellMaterial);
            if (!material)
            {
                material = new Material(Shader.Find("AbyssHunter/CinderSpell"));
                AssetDatabase.CreateAsset(material, spellMaterial);
            }

            var root = previous ? PrefabUtility.LoadPrefabContents(Prefab) :
                (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemy.prefab"));
            try
            {
                var original = root.GetComponentInChildren<Animator>();
                var mage = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(visualPath), root.transform);
                mage.transform.SetLocalPositionAndRotation(original.transform.localPosition, original.transform.localRotation);
                original.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(original.gameObject);
                var animation = new SerializedObject(root.GetComponent<EnemyAnimationController>());
                animation.FindProperty("animator").objectReferenceValue = mage.GetComponent<Animator>();
                animation.ApplyModifiedPropertiesWithoutUndo();

                root.name = "烬火法师";
                var caster = root.GetComponent<RiftCaster>();
                if (!caster) caster = root.AddComponent<RiftCaster>();
                var data = new SerializedObject(caster);
                foreach (string field in new[] { "focus", "bolt" })
                {
                    var old = data.FindProperty(field).objectReferenceValue as Transform;
                    if (old) Object.DestroyImmediate(old.gameObject);
                }
                var aim = data.FindProperty("warning").objectReferenceValue as LineRenderer;
                if (!aim)
                {
                    aim = new GameObject("施法锁向").AddComponent<LineRenderer>();
                    aim.transform.SetParent(root.transform, false);
                }
                aim.useWorldSpace = true;
                aim.positionCount = 2;
                aim.textureMode = LineTextureMode.Tile;
                aim.sharedMaterial = material;
                aim.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                aim.enabled = false;
                data.FindProperty("warning").objectReferenceValue = aim;
                data.FindProperty("focus").objectReferenceValue = Quad("杖头火种");
                data.FindProperty("bolt").objectReferenceValue = Quad("飞行火球");
                data.FindProperty("impact").objectReferenceValue = Quad("爆燃余烬");
                data.FindProperty("staff").objectReferenceValue = mage.GetComponentsInChildren<Transform>().Single(t => t.name == "右手武器");
                data.ApplyModifiedPropertiesWithoutUndo();
                var ai = new SerializedObject(root.GetComponent<EnemyController>());
                ai.FindProperty("attackRange").floatValue = 8;
                ai.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);

                Transform Quad(string name)
                {
                    var sheet = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    sheet.name = name;
                    sheet.transform.SetParent(root.transform, false);
                    Object.DestroyImmediate(sheet.GetComponent<Collider>());
                    var renderer = sheet.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    sheet.SetActive(false);
                    return sheet.transform;
                }
            }
            finally
            {
                if (previous) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        private static AnimatorController SpellController()
        {
            const string path = "Assets/_Game/Animations/CinderMageAnimator.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller) return controller;
            AssetDatabase.CopyAsset("Assets/_Game/Animations/EnemyAnimator.controller", path);
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            controller.name = "CinderMageAnimator";
            var machine = controller.layers[0].stateMachine;
            var lift = machine.states.Single(s => s.state.name == "Melee_2H_Attack").state;
            lift.name = "RaiseStaff";
            lift.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationFolder + "Ranged_Magic_Raise.anim");
            lift.speed = 1;
            var release = machine.AddState("ReleaseFire");
            release.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationFolder + "Ranged_Magic_Shoot.anim");
            var transition = release.AddTransition(machine.defaultState);
            transition.hasExitTime = true;
            transition.exitTime = .82f;
            transition.duration = .12f;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}

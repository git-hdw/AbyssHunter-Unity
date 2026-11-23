using System.Linq;
using AbyssHunter.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AbyssHunter.EditorTools
{
    public static class HunterWeaponSetup
    {
        [MenuItem("Tools/深渊猎手/角色/配置猎手武器")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ConfigureAttack("AxeSweep", "Combat Melee/Melee_2H_Attack_Slice", "HunterAxeSweep", .44f, 1.25f);
            ConfigureAttack("CrossbowShot", "Combat Ranged/Ranged_1H_Shoot", "HunterCrossbowShot", .29f, 1.4f);
            const string path = "Assets/_Game/Prefabs/Characters/HunterVisual.prefab";
            var hunter = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var weapons = hunter.GetComponent<HunterWeapons>();
                if (!weapons) weapons = hunter.AddComponent<HunterWeapons>();
                var slots = new SerializedObject(weapons);
                var bolt = slots.FindProperty("bolt").objectReferenceValue as GameObject;

                var sword = hunter.GetComponentsInChildren<Transform>().Single(t => t.name == "右手武器");
                slots.FindProperty("sword").objectReferenceValue = sword.gameObject;
                slots.FindProperty("supportGrip").objectReferenceValue =
                    hunter.GetComponentsInChildren<Transform>(true).Single(t => t.name == "handslot.l");
                var axeSlot = slots.FindProperty("axe");
                var axe = axeSlot.objectReferenceValue as GameObject;
                const string heavyModel = "Assets/KayKit/Characters/Adventurers/axe_2handed.fbx";
                if (!axe || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(axe) != heavyModel)
                {
                    if (axe) Object.DestroyImmediate(axe);
                    axeSlot.objectReferenceValue = Mount("axe_2handed", "重斧", "BattleAxe", sword.parent);
                }
                if (!slots.FindProperty("crossbow").objectReferenceValue)
                    slots.FindProperty("crossbow").objectReferenceValue = Mount("crossbow_1handed", "轻弩", "猎手", sword.parent);
                var bow = (GameObject)slots.FindProperty("crossbow").objectReferenceValue;
                bow.transform.localRotation = Quaternion.Euler(0, -90, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(bow.transform);
                if (!bolt) bolt = Mount("arrow_crossbow", "弩矢", "猎手", hunter.transform);
                var trail = bolt.GetComponent<TrailRenderer>();
                if (!trail) trail = bolt.AddComponent<TrailRenderer>();
                trail.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/RiftPolish/核心橙金.mat");
                trail.time = .063f;
                trail.startWidth = .085f;
                trail.endWidth = 0;
                trail.minVertexDistance = .06f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                slots.FindProperty("bolt").objectReferenceValue = bolt;
                slots.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(hunter, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(hunter); }
        }

        private static GameObject Mount(string model, string name, string material, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/KayKit/Characters/Adventurers/{model}.fbx");
            var item = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            item.name = name;
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            var atlas = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Game/Prefabs/Characters/{material}.mat");
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => atlas).ToArray();
            item.SetActive(false);
            return item;
        }

        private static void ConfigureAttack(string state, string sourceName, string clipName, float contact, float speed)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Game/Animations/PlayerAnimator.controller");
            var machine = controller.layers[0].stateMachine;
            var attack = machine.states.FirstOrDefault(s => s.state.name == state).state;
            string source = $"Assets/KayKit/Characters/Animations/Animations/Rig_Medium/{sourceName}.anim";
            string path = $"Assets/_Game/Animations/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var authored = Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>(source));
            authored.name = clipName;
            AnimationUtility.SetAnimationEvents(authored, new[] { new AnimationEvent { time = contact, functionName = "ApplyAttackDamage" } });
            if (clip)
            {
                EditorUtility.CopySerializedIfDifferent(authored, clip);
                Object.DestroyImmediate(authored);
            }
            else
            {
                clip = authored;
                AssetDatabase.CreateAsset(clip, path);
            }
            if (!attack)
            {
                attack = machine.AddState(state);
                var exit = attack.AddTransition(machine.defaultState);
                exit.hasExitTime = true;
                exit.exitTime = .94f;
                exit.duration = .06f;
            }
            attack.motion = clip;
            attack.speed = speed;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }
    }
}

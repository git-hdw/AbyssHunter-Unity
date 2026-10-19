using System.IO;
using System.Linq;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AI;

namespace AbyssHunter.EditorTools
{
    public static class HeavyEnemySetup
    {
        [MenuItem("Tools/深渊猎手/Verify heavy combat")]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            SessionState.SetBool("AbyssHunter.HeavyTest", true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/深渊猎手/Configure heavy enemy encounters")]
        public static void Configure()
        {
            if (EditorApplication.isPlaying) return;
            Directory.CreateDirectory("Assets/_Game/Combat");
            const string profilePath="Assets/_Game/Combat/BruteAttack.asset";
            var profile=AssetDatabase.LoadAssetAtPath<HeavyAttackProfile>(profilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<HeavyAttackProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
            const string controllerPath="Assets/_Game/Animations/HeavyAnimator.controller";
            if(AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)==null)
                AssetDatabase.CopyAsset("Assets/_Game/Animations/EnemyAnimator.controller",controllerPath);
            var animatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            var machine=animatorController.layers[0].stateMachine;
            if(!machine.states.Any(s=>s.state.name=="HeavySwing"))
            {
                var state=machine.AddState("HeavySwing");
                state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/KayKit/Characters/Animations/Animations/Rig_Large/Combat Melee/Melee_2H_Attack.anim");
                state.speed=0;
            }
            const string warningPath="Assets/_Game/Materials/MAT_HeavyWarning.mat";
            var warning=AssetDatabase.LoadAssetAtPath<Material>(warningPath);
            if(warning==null){warning=new Material(Shader.Find("AbyssHunter/AttackTelegraph"));AssetDatabase.CreateAsset(warning,warningPath);}
            const string armorPath="Assets/_Game/Materials/MAT_HeavyArmor.mat";
            var armor=AssetDatabase.LoadAssetAtPath<Material>(armorPath);
            if(armor==null)
            {
                armor=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/MAT_Enemy_Red.mat"));
                armor.SetColor("_BaseColor",new Color(.65f,.34f,.08f));AssetDatabase.CreateAsset(armor,armorPath);
            }
            const string prefabPath="Assets/_Game/Prefabs/HeavyEnemy.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)==null)
            {
                var root=PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/Enemy.prefab");
                try
                {
                    root.name="HeavyEnemy";
                    Set(root.GetComponent<EnemyController>(),"heavyAttack",profile);
                    var health=new SerializedObject(root.GetComponent<Health>());health.FindProperty("maximumHealth").intValue=150;health.ApplyModifiedPropertiesWithoutUndo();
                    var agent=root.GetComponent<NavMeshAgent>();agent.speed=2.2f;agent.acceleration=6;
                    var animator=root.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=animatorController;
                    animator.transform.localScale*=1.16f;
                    foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                        renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>armor).ToArray();
                    var hand=root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="hand.r");
                    if(hand!=null)
                    {
                        Primitive(hand,"Hammer shaft",new Vector3(0,-.3f,0),new Vector3(.09f,.85f,.09f),armor);
                        Primitive(hand,"Hammer head",new Vector3(0,-.75f,0),new Vector3(.75f,.32f,.4f),armor);
                    }
                    var presentation=root.AddComponent<HeavyAttackPresentation>();Set(presentation,"telegraphMaterial",warning);
                    PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
                    File.WriteAllText("Logs/heavy-model-rig.txt",string.Join("\n",root.GetComponentsInChildren<Transform>().Select(t=>t.name)));
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath),settings.DefaultGroup);
            entry.address="HeavyEnemy";
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved,entry,true);
            AssetDatabase.SaveAssets();

            var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var wave=Object.FindFirstObjectByType<EnemyWaveSpawner>();
            var poolObject=GameObject.Find("HeavyEnemyPool");
            if(poolObject==null)poolObject=new GameObject("HeavyEnemyPool");
            var pool=poolObject.GetComponent<EnemyPool>();if(pool==null)pool=poolObject.AddComponent<EnemyPool>();
            var poolData=new SerializedObject(pool);
            poolData.FindProperty("enemyPrefabReference.m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(prefabPath);
            poolData.FindProperty("initialSize").intValue=1;poolData.ApplyModifiedPropertiesWithoutUndo();
            var waves=new SerializedObject(wave);waves.FindProperty("heavyEnemyPool").objectReferenceValue=pool;
            var array=waves.FindProperty("encounters");array.arraySize=3;
            string[] titles={"步法练习 / 清除两名斥候","重锤试炼 / 诱导落锤，抓住后摇反击","混合敌潮 / 拉开身位，寻找反击时机"};
            int[] normals={2,0,2};int[] heavies={0,1,1};
            for(int i=0;i<3;i++)
            {
                var e=array.GetArrayElementAtIndex(i);e.FindPropertyRelative("title").stringValue=titles[i];
                e.FindPropertyRelative("normalCount").intValue=normals[i];e.FindPropertyRelative("heavyCount").intValue=heavies[i];
            }
            waves.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/heavy-setup.txt","DONE: Heavy prefab, profile, shader, animator, Addressables registration and mixed encounters saved.");
        }

        [MenuItem("Tools/深渊猎手/Refresh heavy appearance")]
        public static void RefreshAppearance()
        {
            if (EditorApplication.isPlaying) return;
            Material Solid(string name, Color color, float metallic)
            {
                string path = $"Assets/_Game/Materials/{name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetTexture("_BaseMap", null);
                material.SetTexture("_MainTex", null);
                material.SetColor("_BaseColor", color);
                material.SetColor("_Color", color);
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", .32f);
                EditorUtility.SetDirty(material);
                return material;
            }
            var armor = Solid("MAT_HeavyArmor", new Color(.72f, .37f, .1f), .3f);
            var steel = Solid("MAT_HeavySteel", new Color(.16f, .21f, .25f), .65f);
            var grip = Solid("MAT_HeavyGrip", new Color(.18f, .09f, .045f), 0);
            const string path = "Assets/_Game/Prefabs/HeavyEnemy.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentInChildren<Animator>().GetComponentsInChildren<Renderer>())
                {
                    var material = renderer.name.Contains("Leg") || renderer.name == "Hammer head" ? steel : armor;
                    if (renderer.name == "Hammer shaft") material = grip;
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                }
                var transforms = root.GetComponentsInChildren<Transform>();
                var shaft = transforms.First(t => t.name == "Hammer shaft");
                shaft.localPosition = new Vector3(0, -.4f, 0);
                shaft.localScale = new Vector3(.1f, 1.2f, .1f);
                var head = transforms.First(t => t.name == "Hammer head");
                head.localPosition = new Vector3(0, -1, 0);
                head.localScale = new Vector3(.9f, .45f, .5f);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/heavy-appearance.txt", "DONE");
        }
        private static void Set(Object obj,string name,Object value)
        {var data=new SerializedObject(obj);data.FindProperty(name).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
        private static void Primitive(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}

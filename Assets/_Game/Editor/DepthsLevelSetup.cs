using System;
using System.IO;
using System.Linq;
using AbyssHunter.Character;
using AbyssHunter.Enemy;
using AbyssHunter.Inventory;
using AbyssHunter.UI;
using AbyssHunter.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    public static class DepthsLevelSetup
    {
        private const string ScenePath = "Assets/Scenes/AbyssDepths.unity";
        private static Vector3 Map(Vector3 p)
        {
            float shift = p.z <= 8 ? 0 : p.z < 16 ? Mathf.Lerp(0,-12,(p.z-8)/8) : p.z <= 32 ? -12 : p.z < 40 ? Mathf.Lerp(-12,8,(p.z-32)/8) : 8;
            return p + Vector3.right * shift;
        }
        [MenuItem("Tools/深渊猎手/关卡/创建裂隙回廊（仅首次）")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Logs");
            if (File.Exists(ScenePath)) { Debug.Log("裂隙回廊已存在，保留现有场景。" ); return; }
            if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath)) throw new IOException("Cannot copy scene");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            // Map world positions from a snapshot in parent-first order, so children are not shifted twice.
            var transforms = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var positions = transforms.Select(t=>Map(t.position)).ToArray();
            for (int i=0;i<transforms.Length;i++) transforms[i].position=positions[i];
            var root = GameObject.Find("Sunken Ruins"); root.name="裂隙回廊";
            foreach(var t in transforms.Where(t=>t!=null && t.name=="Platform rim" && t.position.x < -20 && Mathf.Abs(t.position.z-24)<1).ToArray()) Object.DestroyImmediate(t.gameObject);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Ruins/Carved stone.mat");
            if (material == null) material=root.GetComponentInChildren<Renderer>().sharedMaterial;
            foreach(var t in transforms.Where(t=>t!=null && (t.name=="Bridge guardrail" || t.name=="Repaired bridge slab")).ToArray()) Object.DestroyImmediate(t.gameObject);
            Bridge(root.transform,Map(new Vector3(0,0,7)),Map(new Vector3(0,0,17)),material);
            SecondBridge(root.transform,material);
            // Side passage rejoins the courtyard before the next encounter. It is never required.
            SidePassage(root.transform,material);
            var potion=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Pickup_HealthPotion.prefab"));
            potion.name="侧廊补给 · 生命药水"; potion.transform.position=new Vector3(-24,.65f,24);
            ConfigurePotion(potion);
            var waves=Object.FindFirstObjectByType<EnemyWaveSpawner>(); var data=new SerializedObject(waves);
            var encounters=data.FindProperty("encounters");
            string[] titles={"入口回廊 · 侵蚀巡卫","断桥庭院 · 守卫伏击","深层祭坛 · 双锤封锁"};
            int[] normal={3,1,2},heavy={0,1,2};
            for(int i=0;i<3;i++)
            {
                var e=encounters.GetArrayElementAtIndex(i); e.FindPropertyRelative("title").stringValue=titles[i];
                e.FindPropertyRelative("normalCount").intValue=normal[i]; e.FindPropertyRelative("heavyCount").intValue=heavy[i];
                var spawn=e.FindPropertyRelative("positions"); spawn.arraySize=4;
                for(int j=0;j<4;j++)
                {
                    var point=new GameObject("Depths spawn " + i+"-"+j).transform; point.SetParent(root.transform);
                    point.position=Map(new Vector3((j%2==0?-1:1)*3,1,i*24+(j/2)*3));
                    spawn.GetArrayElementAtIndex(j).objectReferenceValue=point;
                }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            var mission=new SerializedObject(root.GetComponent<AbyssExpedition>());
            mission.FindProperty("requiredShards").intValue=6; mission.FindProperty("levelTitle").stringValue="裂隙回廊";
            Strings(mission,"objectives",new[]{"穿越入口回廊","突破断桥庭院","肃清深层祭坛"});
            Strings(mission,"clearTitles",new[]{"入口回廊已肃清","断桥庭院已肃清","深层核心已解封"});
            Strings(mission,"clearDetails",new[]{"沿折返石桥深入，庭院侧廊留有补给。","前往深层祭坛，小心两名重锤守卫。","按 E 取回核心，带六枚碎片返回入口。"});
            mission.ApplyModifiedPropertiesWithoutUndo();
            var atmosphere=new SerializedObject(root.GetComponent<AbyssAtmosphere>());
            atmosphere.FindProperty("useConfiguredRouteOrder").boolValue=true;
            foreach(string field in new[]{"routeLights","routeRunes"})
            {
                var a=atmosphere.FindProperty(field); var values=Enumerable.Range(0,a.arraySize).Select(i=>(Component)a.GetArrayElementAtIndex(i).objectReferenceValue).OrderByDescending(c=>c.transform.position.z).ToArray();
                for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
            }
            atmosphere.ApplyModifiedPropertiesWithoutUndo();
            var sound=new SerializedObject(root.GetComponent<AbyssSoundscape>());
            foreach(string field in new[]{"chainsPosition","altarPosition","stonesPosition"})sound.FindProperty(field).vector3Value=Map(sound.FindProperty(field).vector3Value);
            var wind=sound.FindProperty("windPositions");for(int i=0;i<wind.arraySize;i++)wind.GetArrayElementAtIndex(i).vector3Value=Map(wind.GetArrayElementAtIndex(i).vector3Value);
            sound.ApplyModifiedPropertiesWithoutUndo();
            RepairVisualGeometry(root);
            MarkNavigationObstacles(root);
            var surface=root.GetComponent<NavMeshSurface>(); surface.RemoveData(); surface.navMeshData=null; surface.BuildNavMesh();
            surface.navMeshData.name="AbyssDepthsNavMesh";
            AssetDatabase.CreateAsset(surface.navMeshData,"Assets/_Game/Combat/AbyssDepthsNavMesh.asset");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes=LevelCatalog.BuildScenes.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
            PlayerSettings.bundleVersion="0.3.0";
            File.WriteAllText("Logs/depths-setup.txt","DONE | Independent scene and navigation created; nine enemies, six shards required.\n");
        }
        private static void Strings(SerializedObject data,string field,string[] values)
        {var p=data.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).stringValue=values[i];}
        [MenuItem("Tools/深渊猎手/关卡/修复第二座桥通行")]
        public static void RepairSecondBridge()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(ScenePath);var root=GameObject.Find("裂隙回廊");
            if(root.transform.Find("浮空裂隙地图")!=null){Debug.Log("浮空裂隙不使用旧桥迁移工具。");return;}
            if(root.transform.Find("第二桥折返平台")==null)
            {
                foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>(t.name=="折返石桥" || t.name=="折返护栏") && t.position.z>25).ToArray())Object.DestroyImmediate(t.gameObject);
                var material=root.GetComponentsInChildren<Renderer>().First(r=>r.name=="折返石桥").sharedMaterial;
                SecondBridge(root.transform,material);MarkNavigationObstacles(root);
                Physics.SyncTransforms();
                var surface=root.GetComponent<NavMeshSurface>();var asset=surface.navMeshData;
                surface.RemoveData();surface.navMeshData=null;surface.BuildNavMesh();surface.navMeshData.name="AbyssDepthsNavMesh";
                EditorUtility.CopySerialized(surface.navMeshData,asset);surface.RemoveData();Object.DestroyImmediate(surface.navMeshData);surface.navMeshData=asset;surface.AddData();EditorUtility.SetDirty(asset);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
        }
        private static void SecondBridge(Transform parent,Material material)
        {
            // Six-metre walkway with two broad right-angle turns; overlap each platform.
            Box(parent,"第二桥庭院接桥",new Vector3(-12,-.3f,33.5f),new Vector3(6,.6f,7),Quaternion.identity,material);
            Box(parent,"第二桥折返平台",new Vector3(-2,-.3f,36),new Vector3(26,.6f,6),Quaternion.identity,material);
            Box(parent,"第二桥祭坛接桥",new Vector3(8,-.3f,38.5f),new Vector3(6,.6f,7),Quaternion.identity,material);
            Box(parent,"折返护栏",new Vector3(1.2f,.7f,32.8f),new Vector3(.35f,1.4f,20),Quaternion.Euler(0,90,0),material);
            Box(parent,"折返护栏",new Vector3(-5.2f,.7f,39.2f),new Vector3(.35f,1.4f,20),Quaternion.Euler(0,90,0),material);
            foreach(float x in new[]{-15.2f,11.2f})Box(parent,"折返护栏",new Vector3(x,.7f,36),new Vector3(.35f,1.4f,6.4f),Quaternion.identity,material);
        }
        [MenuItem("Tools/深渊猎手/关卡/修复裂隙回廊显示几何")]
        public static void RepairVisuals()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var root=GameObject.Find("裂隙回廊");
            if(root.transform.Find("浮空裂隙地图")!=null){Debug.Log("浮空裂隙已有独立地图网格，无需迁移旧地图。");return;}
            RepairVisualGeometry(root);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static void RepairVisualGeometry(GameObject root)
        {
            if(root.transform.Find("裂隙回廊环境合批")!=null)return;
            var oldBatches=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("静态环境 · ",StringComparison.Ordinal)).ToArray();
            if(oldBatches.Length==0)throw new InvalidOperationException("Missing original scenery batches");
            var materials=oldBatches.Select(r=>r.sharedMaterial).ToArray();
            // Original source meshes remain in the scene, with their second-level transforms.
            // Rebuild from those sources; never deform or overwrite the shared first-level meshes.
            var sources=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>
            {
                var renderer=f.GetComponent<MeshRenderer>();
                return renderer!=null && !renderer.enabled && f.sharedMesh!=null && materials.Contains(renderer.sharedMaterial);
            }).ToArray();
            if(sources.Length==0)throw new InvalidOperationException("Missing scenery source meshes");
            const string folder="Assets/_Game/Art/Abyss/Depths";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/_Game/Art/Abyss","Depths");
            var parent=new GameObject("裂隙回廊环境合批").transform;parent.SetParent(root.transform,false);
            int index=0;
            foreach(var group in sources.GroupBy(f=>(f.GetComponent<MeshRenderer>().sharedMaterial,Mathf.FloorToInt(f.transform.position.x/20),Mathf.FloorToInt(f.transform.position.z/24))))
            {
                var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,name="DepthsScenery"+index};
                mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
                mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/Scenery"+index+".asset");
                var batch=new GameObject("回廊环境 · "+index,typeof(MeshFilter),typeof(MeshRenderer));batch.transform.SetParent(parent,false);
                batch.GetComponent<MeshFilter>().sharedMesh=mesh;batch.GetComponent<MeshRenderer>().sharedMaterial=group.Key.Item1;batch.isStatic=true;
                index++;
            }
            foreach(var batch in oldBatches)batch.gameObject.SetActive(false);
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/depths-visual-repair.txt",$"Rebuilt {sources.Length} source meshes into {index} independent batches; retired {oldBatches.Length} first-level batches.\n");
        }
        [MenuItem("Tools/深渊猎手/关卡/验证裂隙回廊导航与补给 %&j")]
        public static void VerifyNavigation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var root=GameObject.Find("裂隙回廊");
            if(root.transform.Find("浮空裂隙地图")!=null){FloatingRiftMapBuilder.Verify();return;}
            bool navigationChanged=MarkNavigationObstacles(root);
            var potion=GameObject.Find("侧廊补给 · 生命药水");
            if(potion!=null && !(potion.GetComponent<PickupItem>().Item is HealingItemData))
            { ConfigurePotion(potion);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene); }
            var rim=root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Platform rim" && t.position.x < -20 && Mathf.Abs(t.position.z-24)<1);
            if(navigationChanged || rim!=null || root.transform.Find("侧廊石台")==null || root.GetComponentsInChildren<Transform>().Any(t=>t.name=="折返护栏" && t.localScale.y<1))
            {
                if(rim!=null)Object.DestroyImmediate(rim.gameObject);
                foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>(t.name=="折返石桥" || t.name=="折返护栏")&&t.position.x < -15).ToArray())Object.DestroyImmediate(t.gameObject);
                if(root.transform.Find("侧廊石台")==null)SidePassage(root.transform,AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Ruins/Carved stone.mat"));
                foreach(var rail in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="折返护栏"))
                { var position=rail.position;position.y=.7f;rail.position=position;var scale=rail.localScale;scale.y=1.4f;rail.localScale=scale; }
                var surface=root.GetComponent<NavMeshSurface>(); var asset=surface.navMeshData;
                surface.RemoveData();surface.navMeshData=null;surface.BuildNavMesh();
                EditorUtility.CopySerialized(surface.navMeshData,asset);surface.RemoveData();Object.DestroyImmediate(surface.navMeshData);surface.navMeshData=asset;surface.AddData();EditorUtility.SetDirty(asset);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            var mission=root.GetComponent<AbyssExpedition>();
            var report=new System.Collections.Generic.List<string>();
            var navigationAsset=root.GetComponent<NavMeshSurface>().navMeshData;
            navigationAsset.name="AbyssDepthsNavMesh";EditorUtility.SetDirty(navigationAsset);AssetDatabase.SaveAssets();
            report.Add(potion!=null && potion.GetComponent<PickupItem>().Item is HealingItemData ? "PASS | Fixed side supply is healing potion" : "FAIL | Missing healing supply");
            foreach(var destination in new[]{mission.Core.position,new Vector3(-24,0,24)})
            {
                bool start=NavMesh.SamplePosition(mission.Beacon.position,out var a,5,NavMesh.AllAreas);
                bool end=NavMesh.SamplePosition(destination,out var b,5,NavMesh.AllAreas);var path=new NavMeshPath();
                bool connected=start&&end&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                report.Add((connected?"PASS | ":"FAIL | ")+"Navigation from beacon to "+destination);
            }
            File.WriteAllLines("Logs/depths-navigation.txt",report);
            if(report.Any(r=>r.StartsWith("FAIL")))throw new InvalidOperationException("Depths navigation disconnected");
        }
        private static void ConfigurePotion(GameObject potion)
        {
            var data=new SerializedObject(potion.GetComponent<PickupItem>());
            data.FindProperty("item").objectReferenceValue=AssetDatabase.LoadAssetAtPath<HealingItemData>("Assets/_Game/Items/HealthPotion.asset");
            data.FindProperty("quantity").intValue=1;data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static bool MarkNavigationObstacles(GameObject root)
        {
            // Default agents can climb 0.75m, while the player steps only 0.3m.
            // Keep low solid rims and footings out of walkable paths in this scene only.
            string[] blockers={"Pillar footing","Platform rim","折返护栏","侧廊外缘","侧廊端缘"};
            bool changed=false;
            foreach(var collider in root.GetComponentsInChildren<Collider>())
            {
                if(!blockers.Contains(collider.name))continue;
                var modifier=collider.GetComponent<NavMeshModifier>();
                if(modifier==null){modifier=collider.gameObject.AddComponent<NavMeshModifier>();changed=true;}
                if(!modifier.overrideArea || modifier.area!=1){modifier.overrideArea=true;modifier.area=1;changed=true;}
                if(collider.name=="折返护栏" && collider.transform.Find("护栏导航边界")==null)
                {
                    var boundary=new GameObject("护栏导航边界");
                    boundary.transform.SetParent(root.transform);
                    boundary.transform.SetPositionAndRotation(collider.transform.position,collider.transform.rotation);
                    var volume=boundary.AddComponent<NavMeshModifierVolume>();
                    volume.area=1;volume.size=new Vector3(1.4f,4,collider.transform.localScale.z+1);
                    // Track ownership without inheriting the rail's non-uniform scale.
                    boundary.transform.SetParent(collider.transform,true);
                    changed=true;
                }
            }
            return changed;
        }
        private static void Bridge(Transform parent, Vector3 from, Vector3 to, Material material,float width=6)
        {
            Vector3 direction=to-from;
            Box(parent,"折返石桥",(from+to)*.5f-Vector3.up*.3f,new Vector3(width,.6f,direction.magnitude+1),Quaternion.LookRotation(direction),material);
            var right=Vector3.Cross(Vector3.up,direction.normalized);
            foreach(int side in new[]{-1,1})
                Box(parent,"折返护栏",(from+to)*.5f+right*side*(width*.5f+.2f)+Vector3.up*.7f,new Vector3(.35f,1.4f,direction.magnitude-2),Quaternion.LookRotation(direction),material);
        }
        private static void SidePassage(Transform parent,Material material)
        {
            Box(parent,"侧廊石台",new Vector3(-23,.0f-.3f,24),new Vector3(10,.6f,16),Quaternion.identity,material);
            Box(parent,"侧廊外缘",new Vector3(-28,.35f,24),new Vector3(.4f,.7f,16),Quaternion.identity,material);
            foreach(float z in new[]{16f,32f})Box(parent,"侧廊端缘",new Vector3(-23,.35f,z),new Vector3(10,.7f,.4f),Quaternion.identity,material);
        }
        private static void Box(Transform parent,string name,Vector3 position,Vector3 size,Quaternion rotation,Material material)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.SetPositionAndRotation(position,rotation);g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;g.isStatic=true;}
    }
}

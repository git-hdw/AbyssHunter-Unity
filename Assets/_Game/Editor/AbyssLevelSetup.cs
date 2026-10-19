using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.Character;
using AbyssHunter.Enemy;
using AbyssHunter.Inventory;
using AbyssHunter.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace AbyssHunter.EditorTools
{
    public static class AbyssLevelSetup
    {
        [MenuItem("Tools/深渊猎手/Verify sunken expedition")]
        public static void Verify()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool("AbyssHunter.ExpeditionTest",true);
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.isPlaying=true;
        }
        private static Transform root;
        private static Material stone, edge, dark, trim, dormant, glow, corruption;
        private static readonly List<Light> lights=new();
        private static readonly List<Renderer> runes=new();

        [MenuItem("Tools/深渊猎手/Configure sunken expedition")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            foreach(string name in new[]{"Sunken Ruins","Ground","ArenaWalls","ArenaObstacles","AbyssAltar","Navigation"})
            { var old=GameObject.Find(name);if(old!=null)Object.DestroyImmediate(old); }
            root=new GameObject("Sunken Ruins").transform;lights.Clear();runes.Clear();
            Directory.CreateDirectory("Assets/_Game/Materials/Ruins");
            stone=Mat("Basalt",new Color(.19f,.25f,.29f)); edge=Mat("Carved stone",new Color(.31f,.38f,.4f));
            dark=Mat("Abyss rock",new Color(.055f,.085f,.12f));trim=Mat("Ancient brass",new Color(.34f,.26f,.15f));
            dormant=Mat("Dormant rune",new Color(.12f,.26f,.29f),new Color(.04f,.16f,.19f));
            glow=Mat("Guiding light",new Color(.2f,.8f,.87f),new Color(.15f,1.1f,1.4f));
            corruption=Mat("Corruption",new Color(.8f,.31f,.09f),new Color(1.5f,.28f,.03f));

            for(int area=0;area<3;area++)
            {
                float z=area*24;
                Box("Suspended foundation",new Vector3(0,-2.3f,z),new Vector3(20,4,18),dark);
                for(int x=-4;x<=4;x++)for(int row=-4;row<=3;row++)
                {
                    var slab=Box("Worn flagstone",new Vector3(x*2,-.3f,z+row*2+1),new Vector3(1.96f,.6f,1.96f),(x+row)%4==0?edge:stone);
                    slab.transform.Rotate(0,0,0);
                }
                for(int side=-1;side<=1;side+=2)
                {
                    Box("Platform rim",new Vector3(side*9.2f,.4f,z),new Vector3(.65f,.8f,16),edge);
                    for(int j=-1;j<=1;j++)
                    {
                        float h=j==0?4.8f:2.8f;
                        Box("Pillar footing",new Vector3(side*8,.25f,z+j*6),new Vector3(1.6f,.5f,1.6f),trim);
                        Box("Fractured pillar",new Vector3(side*8,h*.5f,z+j*6),new Vector3(.9f,h,1),edge).transform.Rotate(0,j*7,side*j*3);
                        Box("Pillar crown",new Vector3(side*8,h,z+j*6),new Vector3(1.4f,.3f,1.4f),stone);
                    }
                }
                foreach(float end in new[]{-8f,8f}) foreach(int side in new[]{-1,1})
                    Box("Broken balustrade",new Vector3(side*6,.4f,z+end),new Vector3(6,.8f,.6f),edge);
                foreach(int side in new[]{-1,1})
                {
                    var rune=Box("Guidance inlay",new Vector3(side*2.8f,.025f,z),new Vector3(.08f,.03f,12),dormant,false);
                    runes.Add(rune.GetComponent<Renderer>());
                    Lamp(new Vector3(side*3.6f,1,z-5),false);
                }
                if(area<2)
                {
                    for(int j=0;j<8;j++)Box("Repaired bridge slab",new Vector3(0,-.3f,z+8.5f+j),new Vector3(6,.6f,.94f),j%3==0?edge:stone);
                    foreach(int side in new[]{-1,1})
                    {
                        Box("Bridge guardrail",new Vector3(side*3.15f,.35f,z+12),new Vector3(.4f,.7f,8.3f),edge);
                        for(int j=0;j<4;j++)runes.Add(Box("Bridge rune",new Vector3(side*2.6f,.03f,z+9+j*2),new Vector3(.18f,.04f,.6f),dormant,false).GetComponent<Renderer>());
                    }
                }
            }
            // End caps prevent accidental falls; the abyss remains visible through the ruined rim.
            Box("Entrance end",new Vector3(0,.4f,-8.7f),new Vector3(18,.8f,.6f),edge);
            Box("Sanctum end",new Vector3(0,.4f,56.7f),new Vector3(18,.8f,.6f),edge);
            for(int i=0;i<24;i++)
            {
                float side=i%2==0?1:-1;float z=-16+i*3.8f;
                var ruin=Box("Distant sunken tower",new Vector3(side*(17+i%4*4),-5-i%3,z),new Vector3(3+i%3,6+i%5,3),dark,false);
                ruin.transform.Rotate(i%3*4,i*17,side*8);
            }
            // Fractured arches frame each hunting ground without obstructing its combat floor.
            foreach(float z in new[]{-5f,20f,45f})
            {
                Box("Arch left",new Vector3(-5.8f,3,z),new Vector3(1.1f,6,1.2f),edge);
                Box("Arch right",new Vector3(5.8f,3,z),new Vector3(1.1f,6,1.2f),edge);
                Box("Broken arch lintel",new Vector3(-3.8f,6,z),new Vector3(5,.7f,1.2f),stone).transform.Rotate(0,0,-8);
                Box("Broken arch tip",new Vector3(4.6f,6,z),new Vector3(2.5f,.7f,1.2f),stone).transform.Rotate(0,0,12);
            }
            var beacon=Marker("Return Beacon",new Vector3(0,.1f,-6));
            Cylinder("Beacon plinth",beacon.position-Vector3.up*.05f,new Vector3(2,.1f,2),trim,false);
            var beaconLight=Lamp(beacon.position+Vector3.up*1.2f,true);
            var core=Marker("Abyss Core",new Vector3(0,.1f,52));
            Cylinder("Core plinth",core.position-Vector3.up*.05f,new Vector3(2.4f,.1f,2.4f),trim,false);
            var coreVisual=Box("Sealed abyss core",core.position+Vector3.up*1.4f,new Vector3(.85f,1.3f,.85f),corruption,false);
            coreVisual.transform.rotation=Quaternion.Euler(20,45,20);
            var altarLight=LightAt("Core glow",core.position+Vector3.up*3, new Color(1,.35f,.1f),5,10);
            foreach(int side in new[]{-1,1})
                for(int i=0;i<4;i++)Box("Corrupted fissure",new Vector3(side*(.8f+i*.4f),.035f,51+i*.65f),new Vector3(.08f,.04f,.9f),corruption,false).transform.Rotate(0,side*35,0);

            var player=Object.FindFirstObjectByType<PlayerMovement>();player.transform.position=new Vector3(0,1,-5);
            player.transform.rotation=Quaternion.identity;
            var camera=Object.FindFirstObjectByType<TopDownCamera>();SetVector(camera,"offset",new Vector3(0,13,-10));
            camera.transform.position=player.transform.position+new Vector3(0,13,-10);camera.transform.LookAt(player.transform.position+Vector3.up);
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(canvas.renderMode!=RenderMode.WorldSpace)canvas.enabled=false;
            Camera.main.backgroundColor=new Color(.025f,.045f,.075f);Camera.main.clearFlags=CameraClearFlags.SolidColor;
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            sun.color=new Color(.62f,.77f,1);sun.intensity=1.35f;sun.transform.rotation=Quaternion.Euler(48,-32,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.25f,.34f,.43f);RenderSettings.ambientEquatorColor=new Color(.13f,.19f,.24f);RenderSettings.ambientGroundColor=new Color(.07f,.1f,.13f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=Camera.main.backgroundColor;RenderSettings.fogDensity=.012f;
            Dust();root.gameObject.AddComponent<RuinAmbience>();

            var waves=Object.FindFirstObjectByType<EnemyWaveSpawner>();var data=new SerializedObject(waves);
            data.FindProperty("timeBetweenWaves").floatValue=.5f;
            var array=data.FindProperty("encounters");array.arraySize=3;
            string[] titles={"遗迹入口 · 侵蚀斥候","守卫庭院 · 重锤猎物","核心祭坛 · 最后的阻拦"};
            for(int i=0;i<3;i++)
            {
                var encounter=array.GetArrayElementAtIndex(i);encounter.FindPropertyRelative("title").stringValue=titles[i];
                encounter.FindPropertyRelative("normalCount").intValue=i==1?0:2;encounter.FindPropertyRelative("heavyCount").intValue=i==0?0:1;
                encounter.FindPropertyRelative("approach").objectReferenceValue=Marker("Encounter approach "+i,new Vector3(0,1,i*24));
                encounter.FindPropertyRelative("activationRadius").floatValue=5;
                var positions=encounter.FindPropertyRelative("positions");positions.arraySize=3;
                for(int j=0;j<3;j++)positions.GetArrayElementAtIndex(j).objectReferenceValue=Marker("Hunter ground spawn",new Vector3((j-1)*3,1,i*24+2));
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            var mission=root.gameObject.AddComponent<AbyssExpedition>();
            Set(mission,"encounters",waves);Set(mission,"hunter",player);Set(mission,"shard",AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Game/Items/AbyssShard.asset"));
            Set(mission,"core",core);Set(mission,"beacon",beacon);Set(mission,"coreVisual",coreVisual);Set(mission,"altarLight",altarLight);Set(mission,"beaconLight",beaconLight);Set(mission,"awakenedRune",glow);
            var m=new SerializedObject(mission);SetArray(m,"returnLights",lights.Cast<Object>().ToArray());SetArray(m,"returnRunes",runes.Cast<Object>().ToArray());m.ApplyModifiedPropertiesWithoutUndo();
            var surface=root.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();
            const string navPath="Assets/_Game/Combat/SunkenRuinsNavMesh.asset";
            var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(existing==null)AssetDatabase.CreateAsset(surface.navMeshData,navPath);
            else{EditorUtility.CopySerialized(surface.navMeshData,existing);Object.DestroyImmediate(surface.navMeshData);surface.navMeshData=existing;EditorUtility.SetDirty(existing);}
            ConfigureEnemies();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/abyss-level-setup.txt","DONE: three spatial encounters, ruins, baked navigation, core, beacon, guaranteed fragments and corruption appearance.");
        }
        private static void ConfigureEnemies()
        {
            foreach(string name in new[]{"Enemy","HeavyEnemy"})
            {
                string path=$"Assets/_Game/Prefabs/{name}.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach(var drop in prefab.GetComponents<EnemyItemDrop>())
                    {
                        var d=new SerializedObject(drop);var item=d.FindProperty("item").objectReferenceValue as ItemData;
                        if(item!=null&&item.ItemId=="abyss_shard"){d.FindProperty("minimumQuantity").intValue=1;d.FindProperty("maximumQuantity").intValue=1;d.FindProperty("dropChance").floatValue=1;d.ApplyModifiedPropertiesWithoutUndo();}
                    }
                    var old=prefab.transform.Find("Corruption scars");if(old!=null)Object.DestroyImmediate(old.gameObject);
                    var scars=new GameObject("Corruption scars").transform;scars.SetParent(prefab.transform,false);
                    // Attached to chest bone so the glow follows the attack animation.
                    var chest=prefab.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="chest");
                    if(chest!=null){scars.SetParent(chest,false);scars.localPosition=Vector3.zero;}
                    for(int i=0;i<3;i++)
                    {
                        var scar=GameObject.CreatePrimitive(PrimitiveType.Cube);scar.name="侵蚀裂隙";scar.transform.SetParent(scars,false);
                        scar.transform.localPosition=new Vector3((i-1)*.13f,chest!=null?0:1.5f,.3f);scar.transform.localScale=new Vector3(.045f,.38f,.045f);scar.transform.localRotation=Quaternion.Euler(0,0,i*22-22);
                        Object.DestroyImmediate(scar.GetComponent<Collider>());scar.GetComponent<Renderer>().sharedMaterial=corruption;
                    }
                    PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
        }
        private static void Dust()
        {
            var go=new GameObject("Drifting abyss motes");go.transform.SetParent(root);go.transform.position=new Vector3(0,3,24);
            var particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.startLifetime=14;main.startSpeed=.12f;main.startSize=.035f;main.startColor=new Color(.3f,.6f,.7f,.3f);main.maxParticles=400;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=particles.emission;emission.rateOverTime=18;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(30,8,80);
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=dormant;particles.Play();
        }
        private static Material Mat(string name,Color color,Color emission=default)
        {
            string path=$"Assets/_Game/Materials/Ruins/{name}.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.2f);
            if(emission.maxColorComponent>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",emission);}
            EditorUtility.SetDirty(mat);return mat;
        }
        private static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,bool collision=true)=>Primitive(PrimitiveType.Cube,name,position,scale,material,collision);
        private static GameObject Cylinder(string name,Vector3 position,Vector3 scale,Material material,bool collision)=>Primitive(PrimitiveType.Cylinder,name,position,scale,material,collision);
        private static GameObject Primitive(PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material,bool collision)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root);go.transform.position=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static Transform Marker(string name,Vector3 position){var t=new GameObject(name).transform;t.SetParent(root);t.position=position;return t;}
        private static Light Lamp(Vector3 position,bool beacon)
        {
            Cylinder("Rune lantern base",position-Vector3.up*.65f,new Vector3(.5f,.35f,.5f),trim,false);
            Box("Rune lantern",position,new Vector3(.22f,.65f,.22f),beacon?glow:dormant,false);
            var light=LightAt("Guiding lantern",position+Vector3.up*.5f,new Color(.25f,.7f,.85f),beacon?1.5f:.6f,6);if(!beacon)lights.Add(light);return light;
        }
        private static Light LightAt(string name,Vector3 p,Color color,float intensity,float range){var l=Marker(name,p).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;return l;}
        private static void Set(Object obj,string field,Object value){var s=new SerializedObject(obj);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetVector(Object obj,string field,Vector3 value){var s=new SerializedObject(obj);s.FindProperty(field).vector3Value=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetArray(SerializedObject obj,string field,Object[] values){var a=obj.FindProperty(field);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
    }
}

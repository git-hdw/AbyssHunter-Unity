using System;
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
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    // Builds geography from polygons and ramps. No first-level meshes or layout are copied.
    public static class FloatingRiftMapBuilder
    {
        private const string Folder = "Assets/_Game/Art/FloatingRift";
        private static Transform root;
        private static readonly List<Vector3[]> decks = new();
        private static readonly List<Light> lamps = new();
        private static readonly List<Renderer> runes = new();
        private static Material floor, rim, rock, cyan, dormant, violet, gold;
        private static int meshIndex;

        [MenuItem("Tools/深渊猎手/关卡/制作全新浮空裂隙地图")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory("Logs");
            File.Copy("Assets/Scenes/AbyssDepths.unity","Logs/AbyssDepths-before-floating-rift-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/AbyssDepths.unity");
            var mission=Object.FindFirstObjectByType<AbyssExpedition>();
            root=mission.transform;
            var surface=root.GetComponent<NavMeshSurface>();var navigation=surface.navMeshData;surface.RemoveData();
            // Retain configured gameplay components; replace every old map child and fixed pickup.
            foreach(var pickup in Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None))Object.DestroyImmediate(pickup.gameObject);
            foreach(Transform child in root.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            root.name="裂隙回廊";
            var marker=new GameObject("浮空裂隙地图");marker.transform.SetParent(root,false);
            decks.Clear();lamps.Clear();runes.Clear();meshIndex=0;
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Game/Art","FloatingRift");
            floor=Material("浮岛岩板",new Color(.19f,.25f,.32f));
            rim=Material("浅钛边框",new Color(.32f,.4f,.46f));
            rock=Material("悬浮玄晶",new Color(.065f,.075f,.14f));
            cyan=Material("青色引导",new Color(.12f,.65f,.8f),2);
            dormant=Material("沉寂引导",new Color(.055f,.13f,.19f));
            violet=Material("裂隙紫晶",new Color(.38f,.12f,.67f),1.2f);
            gold=Material("祭坛核心",new Color(.95f,.55f,.12f),2);

            Island("起航浮岛",new Vector3(0,0,0),22,18,3);
            Island("双层守望台",new Vector3(-16,1.5f,30),24,20,4);
            Island("补给悬台",new Vector3(-38,1.5f,30),10,14,2);
            var altar=Enumerable.Range(0,12).Select(i=>new Vector3(8+14*Mathf.Cos(i*Mathf.PI/6),3,60+14*Mathf.Sin(i*Mathf.PI/6))).Reverse().ToArray();
            Deck("中央祭坛浮岛",altar);
            Underside(altar,8);
            Ramp("起航引桥",new Vector3(0,0,7),new Vector3(0,0,14),8);
            Island("起航桥平转台",new Vector3(0,0,14),8,8,0);
            Ramp("西行升空桥",new Vector3(-4,0,14),new Vector3(-12,1.5f,14),8);
            Island("守望桥平转台",new Vector3(-16,1.5f,14),8,8,0);
            Ramp("守望台接桥",new Vector3(-16,1.5f,14),new Vector3(-16,1.5f,21),8);
            Ramp("祭坛下层接桥",new Vector3(-16,1.5f,39),new Vector3(-16,1.5f,44),8);
            Island("长桥下层平转台",new Vector3(-16,1.5f,44),8,8,0);
            Ramp("中央升空长桥",new Vector3(-12,1.5f,44),new Vector3(4,3,44),8);
            Island("长桥上层平转台",new Vector3(8,3,44),8,8,0);
            Ramp("祭坛上层接桥",new Vector3(8,3,44),new Vector3(8,3,48),8);
            Ramp("补给侧桥南",new Vector3(-27,1.5f,25),new Vector3(-34,1.5f,25),5);
            Ramp("补给侧桥北",new Vector3(-34,1.5f,35),new Vector3(-27,1.5f,35),5);
            BuildPerimeter();
            Decorate();

            var player=Object.FindFirstObjectByType<PlayerMovement>();player.transform.SetPositionAndRotation(new Vector3(0,1,-5),Quaternion.identity);
            var beacon=Point("归途锚点",new Vector3(0,.1f,-6));
            Shape(PrimitiveType.Cylinder,"起航信标底座",new Vector3(0,.07f,-6),new Vector3(2.4f,.07f,2.4f),rim);
            var beaconFlame=Crystal("归途火种",new Vector3(0,1.1f,-6),new Vector3(.4f,.8f,.4f),gold).transform;
            var beaconLight=Light("归途暖光",new Vector3(0,1.8f,-6),new Color(1,.65f,.3f),.08f,7);
            var core=Point("中央核心交互",new Vector3(8,3.1f,62));
            Shape(PrimitiveType.Cylinder,"中央星环",new Vector3(8,3.025f,62),new Vector3(7,.025f,7),rim);
            Shape(PrimitiveType.Cylinder,"核心光环",new Vector3(8,3.06f,62),new Vector3(4.5f,.02f,4.5f),dormant);
            var coreVisual=Crystal("悬浮裂隙核心",new Vector3(8,5,62),new Vector3(1.1f,2,1.1f),gold);
            var altarLight=Light("祭坛紫光",new Vector3(8,7,62),new Color(.55f,.35f,1),3,15);
            var ember=Crystal("核心归灯火种",coreVisual.transform.position,Vector3.one*.2f,gold).transform;ember.gameObject.SetActive(false);
            var fragments=new List<Transform>();
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;
                fragments.Add(Crystal("祭坛环绕晶体",new Vector3(8+10*Mathf.Cos(a),5.5f,60+10*Mathf.Sin(a)),new Vector3(.65f,2.5f,.65f),violet).transform);
            }
            var route=new[]{new Vector3(8,3.03f,54),new Vector3(8,3.03f,48),new Vector3(8,3.03f,44),new Vector3(0,2.53f,44),new Vector3(-8,2.03f,44),new Vector3(-16,1.53f,44),new Vector3(-16,1.53f,36),new Vector3(-16,1.53f,26),new Vector3(-16,1.53f,19),new Vector3(-16,1.53f,14),new Vector3(-8,.78f,14),new Vector3(0,.03f,14),new Vector3(0,.03f,7),new Vector3(0,.03f,0)};
            foreach(var p in route)
            {
                runes.Add(Shape(PrimitiveType.Cylinder,"归航光标",p,new Vector3(.6f,.025f,.6f),dormant).GetComponent<Renderer>());
                var lamp=Light("归航微光",p+Vector3.up*1.2f,new Color(.2f,.6f,1),.13f,4);lamps.Add(lamp);
            }
            var potion=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Pickup_HealthPotion.prefab"));
            potion.name="侧廊补给 · 生命药水";potion.transform.SetParent(root);potion.transform.position=new Vector3(-38,2.15f,30);
            var pickupData=new SerializedObject(potion.GetComponent<PickupItem>());Set(pickupData,"item",AssetDatabase.LoadAssetAtPath<HealingItemData>("Assets/_Game/Items/HealthPotion.asset"));pickupData.ApplyModifiedPropertiesWithoutUndo();

            var waves=Object.FindFirstObjectByType<EnemyWaveSpawner>();ConfigureEncounters(waves);
            var data=new SerializedObject(mission);
            Set(data,"watchSupply",potion.GetComponent<PickupItem>());
            Set(data,"encounters",waves);Set(data,"hunter",player);Set(data,"core",core);Set(data,"beacon",beacon);Set(data,"coreVisual",coreVisual);
            Set(data,"altarLight",altarLight);Set(data,"beaconLight",beaconLight);Set(data,"awakenedRune",cyan);
            Array(data,"returnLights",lamps.Cast<Object>().ToArray());Array(data,"returnRunes",runes.Cast<Object>().ToArray());
            data.FindProperty("requiredShards").intValue=6;data.FindProperty("levelTitle").stringValue="浮空裂隙";
            Strings(data,"objectives",new[]{"肃清起航浮岛","突破悬浮守望台","肃清中央祭坛"});
            Strings(data,"clearTitles",new[]{"起航浮岛已肃清","守望台已夺回","中央核心已解封"});
            Strings(data,"clearDetails",new[]{"沿宽桥向西上行，补给位于守望台西侧。","沿北侧长桥向东，进入上层中央祭坛。","取回核心，携六枚碎片返回起航浮岛。"});data.ApplyModifiedPropertiesWithoutUndo();
            var atmosphere=new SerializedObject(root.GetComponent<AbyssAtmosphere>());
            Array(atmosphere,"routeLights",lamps.Cast<Object>().ToArray());Array(atmosphere,"routeRunes",runes.Cast<Object>().ToArray());
            Set(atmosphere,"awakenedRune",cyan);Set(atmosphere,"altarLight",altarLight);Set(atmosphere,"beaconLight",beaconLight);Set(atmosphere,"core",coreVisual.transform);
            Set(atmosphere,"travellingEmber",ember);Set(atmosphere,"beaconFlame",beaconFlame);Set(atmosphere,"lantern",Object.FindFirstObjectByType<LastLight>());
            Array(atmosphere,"floatingFragments",fragments.Cast<Object>().ToArray());atmosphere.FindProperty("useConfiguredRouteOrder").boolValue=true;atmosphere.ApplyModifiedPropertiesWithoutUndo();
            var sound=new SerializedObject(root.GetComponent<AbyssSoundscape>());
            sound.FindProperty("chainsPosition").vector3Value=new Vector3(-16,3,30);sound.FindProperty("altarPosition").vector3Value=new Vector3(8,5,62);sound.FindProperty("stonesPosition").vector3Value=new Vector3(-30,-4,40);
            var winds=sound.FindProperty("windPositions");winds.arraySize=2;winds.GetArrayElementAtIndex(0).vector3Value=new Vector3(-8,-2,14);winds.GetArrayElementAtIndex(1).vector3Value=new Vector3(-4,0,44);sound.ApplyModifiedPropertiesWithoutUndo();
            var camera=Object.FindFirstObjectByType<TopDownCamera>();var framing=new SerializedObject(camera);
            framing.FindProperty("offset").vector3Value=new Vector3(0,15,-11);framing.FindProperty("environmentalFraming").boolValue=true;
            var centers=framing.FindProperty("bridgeCenters");centers.arraySize=2;centers.GetArrayElementAtIndex(0).floatValue=14;centers.GetArrayElementAtIndex(1).floatValue=44;
            framing.FindProperty("sanctumStart").floatValue=48;framing.ApplyModifiedPropertiesWithoutUndo();
            Camera.main.backgroundColor=new Color(.025f,.025f,.07f);RenderSettings.fogColor=new Color(.045f,.04f,.105f);RenderSettings.fogDensity=.007f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.36f,.42f,.57f);RenderSettings.ambientEquatorColor=new Color(.2f,.25f,.36f);RenderSettings.ambientGroundColor=new Color(.12f,.1f,.22f);
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);sun.intensity=1.65f;sun.color=new Color(.78f,.87f,1);sun.transform.rotation=Quaternion.Euler(52,-28,0);
            Physics.SyncTransforms();surface.navMeshData=null;surface.BuildNavMesh();surface.navMeshData.name="AbyssDepthsNavMesh";
            EditorUtility.CopySerialized(surface.navMeshData,navigation);surface.RemoveData();Object.DestroyImmediate(surface.navMeshData);surface.navMeshData=navigation;surface.AddData();EditorUtility.SetDirty(navigation);
            RiftArtPolish.ApplyToScene();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Verify();
        }

        public static void Verify()
        {
            var mission=Object.FindFirstObjectByType<AbyssExpedition>();var report=new List<string>();
            foreach(var destination in new[]{mission.Core.position,new Vector3(-38,1.5f,30)})
            {
                bool start=NavMesh.SamplePosition(mission.Beacon.position,out var a,3,NavMesh.AllAreas),end=NavMesh.SamplePosition(destination,out var b,3,NavMesh.AllAreas);var path=new NavMeshPath();
                bool valid=start&&end&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                report.Add((valid?"PASS | ":"FAIL | ")+"Floating rift navigation to "+destination);
            }
            File.WriteAllLines("Logs/floating-rift-navigation.txt",report);
            if(report.Any(r=>r.StartsWith("FAIL")))throw new InvalidOperationException("Floating rift navigation failed");
        }
        private static void ConfigureEncounters(EnemyWaveSpawner waves)
        {
            var data=new SerializedObject(waves);var encounters=data.FindProperty("encounters");encounters.arraySize=3;var all=new List<Object>();
            Vector3[] centers={new(0,0,2),new(-16,1.5f,30),new(8,3,58)};
            Vector3[] approaches={new(0,.1f,0),new(-16,1.6f,23),new(8,3.1f,51)};
            string[] titles={"起航浮岛 · 裂隙巡卫","悬浮守望台 · 重锤封锁","中央祭坛 · 双锤守核"};int[] normal={3,1,2},heavy={0,1,2};
            for(int i=0;i<3;i++)
            {
                var e=encounters.GetArrayElementAtIndex(i);e.FindPropertyRelative("title").stringValue=titles[i];e.FindPropertyRelative("normalCount").intValue=normal[i];e.FindPropertyRelative("heavyCount").intValue=heavy[i];
                e.FindPropertyRelative("activationRadius").floatValue=4;e.FindPropertyRelative("approach").objectReferenceValue=Point("战区接近点"+i,approaches[i]);
                var positions=e.FindPropertyRelative("positions");positions.arraySize=4;
                for(int j=0;j<4;j++){var p=Point("战区出生点"+i+"-"+j,centers[i]+new Vector3(j%2==0?-3:3,1,j/2==0?-2:3));positions.GetArrayElementAtIndex(j).objectReferenceValue=p;all.Add(p);}
            }
            Array(data,"spawnPoints",all.ToArray());data.ApplyModifiedPropertiesWithoutUndo();
            RiftCasterSetup.Configure(waves);
        }
        private static void Island(string name,Vector3 c,float width,float length,float bevel)
        {
            float x=width/2,z=length/2;
            if(bevel==0){Deck(name,new[]{c+new Vector3(-x,0,-z),c+new Vector3(x,0,-z),c+new Vector3(x,0,z),c+new Vector3(-x,0,z)});return;}
            var polygon=new[]{new Vector3(-x+bevel,0,-z),new Vector3(x-bevel,0,-z),new Vector3(x,0,-z+bevel),new Vector3(x,0,z-bevel),new Vector3(x-bevel,0,z),new Vector3(-x+bevel,0,z),new Vector3(-x,0,z-bevel),new Vector3(-x,0,-z+bevel)}.Select(p=>p+c).ToArray();
            Deck(name,polygon);Underside(polygon,6);
        }
        private static void Ramp(string name,Vector3 a,Vector3 b,float width)
        {
            Vector3 d=b-a;d.y=0;Vector3 right=Vector3.Cross(Vector3.up,d.normalized)*width/2;
            Deck(name,new[]{a-right,a+right,b+right,b-right});
        }
        private static void Deck(string name,Vector3[] points)
        {
            decks.Add(points);var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root,false);go.isStatic=true;
            var mesh=Prism(points,.8f,1);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=floor;go.GetComponent<MeshCollider>().sharedMesh=mesh;
        }
        private static void Underside(Vector3[] points,float depth)
        {
            var go=new GameObject("倒悬晶岩",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=Prism(points.Select(p=>p-Vector3.up*.75f).ToArray(),depth,.4f);go.GetComponent<MeshRenderer>().sharedMaterial=rock;
        }
        private static Mesh Prism(Vector3[] points,float depth,float taper)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();Vector3 center=points.Aggregate(Vector3.zero,(a,b)=>a+b)/points.Length;
            void Tri(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);}
            bool positive=points.Select((p,i)=>Vector3.Cross(p-center,points[(i+1)%points.Length]-center).y).Sum()>0;
            for(int i=0;i<points.Length;i++)
            {
                Vector3 a=points[i],b=points[(i+1)%points.Length],lowA=center+(a-center)*taper-Vector3.up*depth,lowB=center+(b-center)*taper-Vector3.up*depth;
                // Determine winding per face, including arbitrary sloped decks.
                if(Vector3.Cross(a-center,b-center).y>0)Tri(center,a,b);else Tri(center,b,a);
                if(positive){Tri(a,lowA,lowB);Tri(a,lowB,b);Tri(center-Vector3.up*depth,lowB,lowA);}
                else{Tri(a,lowB,lowA);Tri(a,b,lowB);Tri(center-Vector3.up*depth,lowA,lowB);}
            }
            var mesh=new Mesh{name="RiftMesh"+meshIndex};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            return SaveMesh(mesh);
        }
        private static Mesh SaveMesh(Mesh mesh)
        {
            string path=Folder+"/Geometry"+(meshIndex++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static bool Inside(Vector3 p,Vector3[] polygon)
        {
            bool inside=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
                if((polygon[i].z>p.z)!=(polygon[j].z>p.z) && p.x<(polygon[j].x-polygon[i].x)*(p.z-polygon[i].z)/(polygon[j].z-polygon[i].z)+polygon[i].x)inside=!inside;
            return inside;
        }
        private static void BuildPerimeter()
        {
            foreach(var polygon in decks)
            {
                var center=polygon.Aggregate(Vector3.zero,(a,b)=>a+b)/polygon.Length;
                for(int i=0;i<polygon.Length;i++)
                {
                    var a=polygon[i];var b=polygon[(i+1)%polygon.Length];Vector3 along=b-a;int pieces=Mathf.CeilToInt(along.magnitude);
                    Vector3 outward=Vector3.Cross(Vector3.up,along.normalized);if(Vector3.Dot(outward,(a+b)*.5f-center)<0)outward=-outward;
                    for(int j=0;j<pieces;j++)
                    {
                        Vector3 from=Vector3.Lerp(a,b,(float)j/pieces),to=Vector3.Lerp(a,b,(float)(j+1)/pieces),mid=(from+to)*.5f;
                        if(decks.Any(other=>other!=polygon && Inside(mid+outward*.15f,other)))continue;
                        var rail=Shape(PrimitiveType.Cube,"浮岛实体护栏",mid+Vector3.up*.75f,new Vector3(.3f,1.5f,Vector3.Distance(from,to)+.08f),rim,true);
                        Vector3 flat=to-from;flat.y=0;rail.transform.rotation=Quaternion.LookRotation(flat);
                        var modifier=rail.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
                    }
                }
            }
        }
        private static void Decorate()
        {
            foreach(var c in new[]{new Vector3(0,.035f,0),new Vector3(-16,1.535f,30),new Vector3(8,3.035f,60)})
            {
                Shape(PrimitiveType.Cylinder,"浮岛罗盘盘面",c,new Vector3(8,.025f,8),rim);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var tile=Shape(PrimitiveType.Cube,"罗盘刻线",c+new Vector3(Mathf.Cos(a)*4,0,Mathf.Sin(a)*4),new Vector3(.08f,.04f,1.6f),cyan);tile.transform.rotation=Quaternion.Euler(0,90-a*Mathf.Rad2Deg,0);}
            }
            var random=new System.Random(713);
            for(int i=0;i<38;i++)
            {
                float x=i%2==0?-49-(float)random.NextDouble()*10:28+(float)random.NextDouble()*12;
                var p=new Vector3(x,-8+(float)random.NextDouble()*5,-8+i*2.2f);
                Crystal("深渊悬浮晶群",p,new Vector3(1+i%3,5+i%5,1+i%3),i%4==0?violet:rock);
            }
            foreach(var p in new[]{new Vector3(-7,1,3),new Vector3(7,1,3),new Vector3(-23,3,31),new Vector3(-9,3,31)})
            {Crystal("航标晶柱",p,new Vector3(.8f,2,.8f),cyan);Light("航标照明",p+Vector3.up,new Color(.25f,.7f,1),1.5f,7);}
        }
        private static GameObject Shape(PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material,bool collision=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;
            if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static GameObject Crystal(string name,Vector3 p,Vector3 scale,Material material)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI/2,b=(i+1)*Mathf.PI/2;
                Vector3 left=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.5f,right=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*.5f;
                int n=vertices.Count;vertices.Add(Vector3.up);vertices.Add(right);vertices.Add(left);vertices.Add(Vector3.down);vertices.Add(left);vertices.Add(right);
                for(int j=0;j<6;j++)triangles.Add(n+j);
            }
            var mesh=new Mesh{name="RiftCrystal"+meshIndex};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.position=p;go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);go.GetComponent<Renderer>().sharedMaterial=material;go.transform.rotation=Quaternion.Euler(18,45,12);return go;
        }
        private static Transform Point(string name,Vector3 p){var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=p;return go.transform;}
        private static Light Light(string name,Vector3 p,Color color,float intensity,float range)
        {var light=Point(name,p).gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;return light;}
        private static Material Material(string name,Color color,float emission=0)
        {
            string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.32f);material.SetFloat("_Metallic",.2f);
            if(emission>0){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*emission);}EditorUtility.SetDirty(material);return material;
        }
        private static void Set(SerializedObject data,string field,Object value)=>data.FindProperty(field).objectReferenceValue=value;
        private static void Array(SerializedObject data,string field,Object[] values){var p=data.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
        private static void Strings(SerializedObject data,string field,string[] values){var p=data.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).stringValue=values[i];}
    }
}

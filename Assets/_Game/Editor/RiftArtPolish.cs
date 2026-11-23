using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace AbyssHunter.EditorTools
{
    // An idempotent display-only pass, also called by the geography generator.
    public static partial class RiftArtPolish
    {
        private const string Folder="Assets/_Game/Art/RiftPolish";
        private static Transform art;
        private static Material basalt, edge, bronze, blue, purple, amber;
        private static Mesh shard;
        private static int piece;

        [MenuItem("Tools/深渊猎手/环境/精修第二关浮空裂隙")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory("Logs");
            File.Copy("Assets/Scenes/AbyssDepths.unity","Logs/AbyssDepths-before-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/AbyssDepths.unity");
            ApplyToScene();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            FloatingRiftMapBuilder.Verify();
        }
        public static void ApplyToScene()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="AbyssDepths")throw new InvalidOperationException("Rift art requires AbyssDepths");
            var root=Object.FindFirstObjectByType<AbyssExpedition>().transform;
            var previous=root.Find("裂隙精修美术");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();piece=0;
            art=new GameObject("裂隙精修美术").transform;art.SetParent(root,false);
            basalt=Mat("深层玄武岩",new Color(.15f,.20f,.28f));edge=Mat("破损护墙",new Color(.25f,.31f,.38f));bronze=Mat("古老铜金",new Color(.42f,.31f,.16f),0,.55f);
            blue=Mat("青蓝灵晶",new Color(.16f,.62f,.76f),2.1f,.3f);purple=Mat("紫色裂隙",new Color(.35f,.14f,.55f),1.25f,.25f);amber=Mat("核心橙金",new Color(1,.52f,.13f),2.6f,.4f);
            shard=SaveMesh("崩裂岩晶",ShardMesh());
            var grain=StoneTexture();
            var floors=new[]{Mat("起航冷灰石",new Color(.34f,.41f,.46f)),Mat("守望蓝黑石",new Color(.28f,.33f,.42f)),Mat("祭坛紫灰石",new Color(.34f,.33f,.43f))};
            foreach(var material in floors){material.SetTexture("_BaseMap",grain);EditorUtility.SetDirty(material);}
            edge.SetTexture("_BaseMap",grain);EditorUtility.SetDirty(edge);
            var mesh=Object.Instantiate(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
            var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)if(vertices[i].y>0)vertices[i].y-=.055f*(1+Mathf.Sin(vertices[i].x*7+vertices[i].z*9));
            mesh.vertices=vertices;mesh.RecalculateNormals();var wallMesh=SaveMesh("缺角墙段",mesh);
            var walls=new List<MeshFilter>();
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.transform.IsChildOf(art)))
            {
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
                if(renderer.GetComponent<MeshCollider>()!=null)
                {
                    // UVs belong to a new render mesh; never change the collision mesh asset.
                    var visual=Object.Instantiate(renderer.GetComponent<MeshCollider>().sharedMesh);
                    visual.uv=visual.vertices.Select(p=>new Vector2(p.x*.08f,p.z*.08f)).ToArray();
                    filter.sharedMesh=SaveMesh("地面_"+renderer.name,visual);renderer.sharedMaterial=floors[renderer.transform.position.z>46?2:0];
                    float z=renderer.bounds.center.z;renderer.sharedMaterial=floors[z>47?2:z>20?1:0];
                }
                if(renderer.name=="浮岛实体护栏")
                {
                    renderer.enabled=true;renderer.sharedMaterial=edge;walls.Add(filter);
                    filter.sharedMesh=wallMesh;
                }
                if(renderer.name=="倒悬晶岩")renderer.sharedMaterial=basalt;
                if(renderer.name is "中央星环" or "浮岛罗盘盘面")renderer.sharedMaterial=basalt;
                if(renderer.name=="核心光环")renderer.sharedMaterial=bronze;
                if(renderer.name=="航标晶柱"){renderer.sharedMaterial=blue;renderer.transform.localScale=new Vector3(1.1f,2.5f,1.1f);}
                if(renderer.name=="悬浮裂隙核心"){renderer.sharedMaterial=amber;renderer.transform.localScale=new Vector3(1.6f,2.5f,1.6f);}
                if(renderer.name=="祭坛环绕晶体"){renderer.sharedMaterial=purple;renderer.transform.localScale=new Vector3(1,2.6f,1);}
                if(renderer.name=="深渊悬浮晶群")renderer.sharedMaterial=renderer.transform.position.z%3<1?purple:basalt;
            }
            int index=0;
            foreach(var wall in walls)
            {
                if(index++%5!=0)continue;
                var p=wall.transform.position;var away=p-new Vector3(p.z>47?8:p.z>20?-16:0,p.y,p.z>47?60:p.z>20?30:0);away.y=0;
                p+=away.normalized*.75f; p.y-=1.7f;
                Rock("外缘断裂岩瓣",p,new Vector3(.8f,2.4f+index%3,.9f),basalt,Quaternion.Euler(index*13,index*27,17));
                Rock("脱落碎片",p+away.normalized*1.1f-Vector3.up*(2+index%4),new Vector3(.5f,.8f,.6f),basalt,Quaternion.Euler(index*33,21,38));
            }
            // Three different arena signatures, keeping their fighting floors clear.
            if (!KayKitEntranceSetup.Available) { Ring(new Vector3(0,.06f,0),4.5f,.12f,bronze,32);
            Ring(new Vector3(0,.075f,0),2.3f,.10f,bronze,24);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var mark=Box("起航罗盘星刻",new Vector3(Mathf.Cos(a)*3,.085f,Mathf.Sin(a)*3),new Vector3(.12f,.025f,1.2f),bronze);mark.transform.rotation=Quaternion.Euler(0,90-a*Mathf.Rad2Deg,0);}
            }
            foreach(int side in new[]{-1,1})
            {
                Cluster(new Vector3(side*7,1,3),blue,1.2f);
                Cluster(new Vector3(-16+side*8,2.6f,34),purple,1.4f);
                Rock("守望断裂尖碑",new Vector3(-16+side*10,3.4f,31),new Vector3(1.8f,4.5f,1.3f),basalt,Quaternion.Euler(side*8,25,side*12));
                Cluster(new Vector3(8+side*11,4.2f,61),purple,1.6f);
            }
            foreach(float z in new[]{14f,44f})
            {
                var c=z<20?new Vector3(-8,-2,z):new Vector3(-4,0,z);
                Rock("升空桥倒悬桥脊",c,new Vector3(16,3,3),basalt,Quaternion.Euler(0,0,4));
                for(int j=0;j<5;j++)Rock("桥下断裂悬片",c+new Vector3(j*3-6,-4-j%2,0),new Vector3(.6f,2+j%3,.7f),j%3==0?purple:basalt,Quaternion.Euler(5,j*19,12));
            }
            Ring(new Vector3(8,3.12f,62),3.5f,.16f,bronze,32);
            Ring(new Vector3(8,3.13f,62),5.5f,.10f,purple,32);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;var p=new Vector3(8+Mathf.Cos(a)*5.5f,3.15f,62+Mathf.Sin(a)*5.5f);
                var mark=Box("祭坛仪式刻印",p,new Vector3(.12f,.025f,.65f),i%3==0?amber:bronze);mark.transform.rotation=Quaternion.Euler(0,90-a*Mathf.Rad2Deg,0);
            }
            Background(); Architecture(root); Lighting();
            Combine(walls);
            KayKitEntranceSetup.ApplyToScene(root);
            KayKitWatchSetup.ApplyToScene(root);
            KayKitAltarSetup.ApplyToScene(root);
            RiftFloorSetup.ApplyToScene(root);
            RiftCrystalVisibility.ApplyToScene(root);
            AssetDatabase.SaveAssets();
        }
        private static void Cluster(Vector3 p,Material material,float scale)
        {
            for(int i=0;i<3;i++)Rock("边缘晶簇",p+new Vector3((i-1)*.65f,0,(i%2)*.45f),new Vector3(.5f,1.4f+i*.45f,.55f)*scale,material,Quaternion.Euler(i*9,35+i*31,(i-1)*18));
            var light=new GameObject("灵晶局部辉光").AddComponent<Light>();light.transform.SetParent(art,false);light.transform.position=p+Vector3.up*1.5f;
            light.type=LightType.Point;light.color=material==blue?new Color(.2f,.7f,1):new Color(.55f,.25f,1);light.intensity=2.4f;light.range=7;light.shadows=LightShadows.None;
        }
        private static void Background()
        {
            var random=new System.Random(1907);
            for(int layer=0;layer<3;layer++)for(int side=-1;side<=1;side+=2)for(int i=0;i<8;i++)
            {
                float x=side<0?-51-layer*12:29+layer*12;
                var p=new Vector3(x+(float)random.NextDouble()*9,-10-layer*12+(float)random.NextDouble()*6,-8+i*12+(float)random.NextDouble()*7);
                Rock("深层漂浮残岛",p,new Vector3(4+i%3,5+layer*3+i%4,3+i%3),basalt,Quaternion.Euler(i*11,i*37,layer*17));
                if(i%3==0)Rock("深处细碎晶群",p+Vector3.up*2,new Vector3(.6f,3,.7f),purple,Quaternion.Euler(9,11,20));
            }
            var mist=Mat("深渊层雾",new Color(.13f,.21f,.32f));mist.shader=Shader.Find("AbyssHunter/DepthMist");mist.SetColor("_BaseColor",new Color(.12f,.22f,.34f,.20f));EditorUtility.SetDirty(mist);
            for(int i=0;i<3;i++)
            {
                var fog=Primitive(PrimitiveType.Quad,"深渊层雾",new Vector3(-10,-5-i*9,32),new Vector3(120,135,1),mist);fog.transform.rotation=Quaternion.Euler(90,0,i*13);fog.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            for(int side=-1;side<=1;side+=2)
            {
                var go=new GameObject("裂隙低量浮尘");go.transform.SetParent(art,false);go.transform.position=new Vector3(side<0?-32:19,-2,32);
                var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.startLifetime=12;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.06f,.16f);main.maxParticles=18;main.startColor=new Color(.25f,.46f,.66f,.45f);main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=ps.emission;emission.rateOverTime=.9f;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(15,5,85);
                var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.y=-.18f;velocity.x=.05f*side;
                var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=shard;renderer.sharedMaterial=basalt;renderer.shadowCastingMode=ShadowCastingMode.Off;ps.Play();
            }
        }
        private static void Lighting()
        {
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);sun.color=new Color(.76f,.85f,1);sun.intensity=1.75f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.8f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.42f,.5f,.63f);RenderSettings.ambientEquatorColor=new Color(.28f,.34f,.45f);RenderSettings.ambientGroundColor=new Color(.10f,.13f,.22f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.055f,.085f,.145f);RenderSettings.fogDensity=.009f;
            Camera.main.backgroundColor=new Color(.025f,.04f,.075f);
            var glow=GameObject.Find("祭坛紫光").GetComponent<Light>();glow.color=new Color(1,.53f,.2f);glow.intensity=3.5f;glow.range=11;
            string path=Folder+"/RiftVolume.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(profile==null){AssetDatabase.CopyAsset("Assets/Settings/SampleSceneProfile.asset",path);profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);}
            var color=profile.TryGet<ColorAdjustments>(out var grading)?grading:profile.Add<ColorAdjustments>();if(!AssetDatabase.Contains(color))AssetDatabase.AddObjectToAsset(color,profile);
            color.postExposure.Override(.45f);color.contrast.Override(10);color.saturation.Override(-6);EditorUtility.SetDirty(color);
            if(profile.TryGet<Tonemapping>(out var tone)){tone.mode.Override(TonemappingMode.ACES);EditorUtility.SetDirty(tone);}
            if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.28f);bloom.threshold.Override(1.2f);EditorUtility.SetDirty(bloom);}
            if(profile.TryGet<Vignette>(out var vignette)){vignette.intensity.Override(.12f);EditorUtility.SetDirty(vignette);}
            if(profile.TryGet<MotionBlur>(out var blur)){blur.active=false;EditorUtility.SetDirty(blur);}
            foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))volume.sharedProfile=profile;
            EditorUtility.SetDirty(profile);var camera=Camera.main.GetUniversalAdditionalCameraData();camera.renderPostProcessing=true;camera.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;camera.antialiasingQuality=AntialiasingQuality.High;
        }
        private static void Ring(Vector3 c,float radius,float thickness,Material material,int count)
        {
            for(int i=0;i<count;i++)
            {
                if(i%8==3)continue;float a=i*Mathf.PI*2/count;var p=c+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                var part=Box("断续仪式环",p,new Vector3(thickness,.035f,radius*Mathf.PI*2/count*.86f),material);part.transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
            }
        }
        private static GameObject Box(string name,Vector3 p,Vector3 scale,Material material)=>Primitive(PrimitiveType.Cube,name,p,scale,material);
        private static GameObject Primitive(PrimitiveType type,string name,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(art,false);go.transform.position=p;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        private static void Rock(string name,Vector3 p,Vector3 scale,Material material,Quaternion rotation)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(art,false);go.transform.position=p;go.transform.localScale=scale;go.transform.rotation=rotation;go.GetComponent<MeshFilter>().sharedMesh=shard;go.GetComponent<MeshRenderer>().sharedMaterial=material;}
        private static Mesh ShardMesh()
        {
            var v=new List<Vector3>();var t=new List<int>();
            void Tri(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);}
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7,b=(i+1)*Mathf.PI*2/7;var p=new Vector3(Mathf.Cos(a),.06f*Mathf.Sin(i*5),Mathf.Sin(a))*(.4f+.07f*Mathf.Sin(i*9));var q=new Vector3(Mathf.Cos(b),.06f*Mathf.Sin((i+1)*5),Mathf.Sin(b))*(.4f+.07f*Mathf.Sin((i+1)*9));
                Tri(new Vector3(.12f,.6f,.04f),q,p);Tri(new Vector3(-.09f,-.65f,0),p,q);
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh SaveMesh(string name,Mesh mesh)
        {
            mesh.name=name;string path=Folder+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
        }
        private static Material Mat(string name,Color color,float emission=0,float metallic=.1f)
        {
            string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.22f);mat.SetFloat("_Metallic",metallic);
            if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}EditorUtility.SetDirty(mat);return mat;
        }
        private static Texture2D StoneTexture()
        {
            string path=Folder+"/裂隙岩板细纹.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null){texture=new Texture2D(512,512,TextureFormat.RGBA32,true){name="裂隙岩板细纹",wrapMode=TextureWrapMode.Repeat,anisoLevel=4};AssetDatabase.CreateAsset(texture,path);}else texture.Reinitialize(512,512);
            var pixels=new Color[512*512];
            for(int y=0;y<512;y++)for(int x=0;x<512;x++)
            {
                float u=x/128f,v=y/128f,first=100,second=100,localX=0,localY=0;int nearest=0;
                int cx=Mathf.FloorToInt(u),cy=Mathf.FloorToInt(v);
                for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++)
                {
                    int sx=cx+i,sy=cy+j,hash=((sx%4+4)%4)*17+((sy%4+4)%4)*31;
                    float dx=sx+.5f+Mathf.Sin(hash*7)*.23f-u,dy=sy+.5f+Mathf.Cos(hash*11)*.23f-v,d=dx*dx+dy*dy;
                    if(d<first){second=first;first=d;nearest=hash;localX=dx;localY=dy;}else if(d<second)second=d;
                }
                float seam=second-first<.027f?.53f:1;
                float crack=nearest%5==0 && Mathf.Abs(localX+localY*.35f)<.009f && localY>-.2f?.72f:1;
                float grain=.76f+(nearest%5)*.027f+Mathf.PerlinNoise(x*.12f,y*.12f)*.10f;
                float n=grain*seam*crack;pixels[y*512+x]=new Color(n,n,n);
            }
            texture.SetPixels(pixels);texture.Apply();EditorUtility.SetDirty(texture);return texture;
        }
        private static void Combine(List<MeshFilter> walls)
        {
            var filters=art.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<ParticleSystemRenderer>()==null && f.GetComponent<Renderer>()?.sharedMaterial?.shader.name!="AbyssHunter/DepthMist").Concat(walls).ToArray();
            foreach(var group in filters.GroupBy(f=>(f.GetComponent<Renderer>().sharedMaterial,Mathf.FloorToInt(f.transform.position.x/24),Mathf.FloorToInt(f.transform.position.z/24))))
            {
                var items=group.ToArray();var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(items.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=art.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();
                string name="环境合批_"+piece++;var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(art,false);go.GetComponent<MeshFilter>().sharedMesh=SaveMesh(name,mesh);go.GetComponent<Renderer>().sharedMaterial=group.Key.Item1;
                foreach(var item in items)item.GetComponent<Renderer>().enabled=false;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssHunter.EditorTools
{
    public static partial class RiftArtPolish
    {
        private static void Architecture(Transform root)
        {
            var pale=Mat("层理灰石",new Color(.34f,.39f,.44f));
            var inlay=Mat("暗银雕刻",new Color(.25f,.33f,.4f),0,.45f);
            var circuit=Mat("雕刻青光",new Color(.12f,.52f,.58f),.8f,.25f);
            var gem=SaveMesh("肩切灵晶",CutGem(6));
            var gemWide=SaveMesh("簇状厚晶",CutGem(5));
            var broken=SaveMesh("沉积断层岩",BrokenSlab());
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.transform.IsChildOf(art)))
            {
                if(r.name is "浮岛罗盘盘面" or "中央星环" or "核心光环")r.enabled=false;
                if(r.name=="深渊悬浮晶群")r.enabled=false;
                if(r.name=="航标晶柱")
                {
                    r.GetComponent<MeshFilter>().sharedMesh=gem;r.transform.localScale=new Vector3(1.35f,2.1f,1.35f);
                    Facility(new Vector3(r.transform.position.x,r.transform.position.z>20?1.52f:.02f,r.transform.position.z),pale,inlay,circuit);
                }
                if(r.name=="悬浮裂隙核心")
                {
                    r.GetComponent<MeshFilter>().sharedMesh=gem;r.transform.position=new Vector3(8,5.5f,62);r.transform.localScale=new Vector3(1.75f,2.3f,1.75f);
                }
            }
            int fragment=0;
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="祭坛环绕晶体"))
            {
                int i=fragment++;float a=i*Mathf.PI/3+.09f*Mathf.Sin(i*3);
                r.transform.position=new Vector3(8+(9.6f+i%2)*Mathf.Cos(a),4.9f+(i%3)*.3f,60+(9.6f+i%2)*Mathf.Sin(a));
                r.transform.localScale=new Vector3(.7f+i%3*.25f,1.6f+i%4*.37f,.85f+i%2*.3f);
                r.GetComponent<MeshFilter>().sharedMesh=i%2==0?gem:gemWide;
            }
            int crystal=0;
            foreach(var f in art.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="边缘晶簇"))
            {
                int i=crystal++;f.sharedMesh=i%2==0?gemWide:gem;
                f.transform.localScale=Vector3.Scale(f.transform.localScale,new Vector3(.8f+i%3*.18f,.75f+i%4*.11f,1));
                f.transform.rotation=Quaternion.Euler(i%3*8,29+i*47,(i%3-1)*14);
            }
            // Shallow carved strata stay near the existing floor, with no new collision surface.
            if (!KayKitEntranceSetup.Available) {
                var start=new Vector3(0,.02f,0);
                Moulding("起航下层石环",start,3.35f,4.1f,.07f,pale,32,0,360,true);
                Moulding("起航内层铜环",start+Vector3.up*.06f,2.6f,3.3f,.06f,inlay,32,0,360,false);
                Glyphs(start+Vector3.up*.145f,3.65f,16,circuit);
            }
            FloorFan(new Vector3(-16,1.52f,30),pale,inlay);
            // Line the pre-existing side opening, without spanning it or changing either side bridge.
            var mouth=new GameObject("裂隙井口断层",typeof(MeshFilter),typeof(MeshRenderer));mouth.transform.SetParent(art,false);
            mouth.GetComponent<MeshFilter>().sharedMesh=SaveMesh("井口内衬",WellLining());mouth.GetComponent<MeshRenderer>().sharedMaterial=basalt;
            var altar=new Vector3(8,3.025f,62);
            Moulding("祭坛外层断石",altar,3.75f,4.65f,.08f,pale,40,0,360,true);
            Moulding("祭坛中层浮雕",altar+Vector3.up*.06f,2.8f,3.7f,.075f,inlay,40,0,360,false);
            Moulding("祭坛内层冠座",altar+Vector3.up*.12f,1.7f,2.7f,.07f,bronze,32,0,360,true);
            Glyphs(altar+Vector3.up*.20f,3.2f,24,amber);
            // An open crown floats over the core; its front is absent to keep the hunter visible.
            Moulding("核心悬浮断冠",new Vector3(8,6.15f,62),2.0f,2.22f,.24f,bronze,24,5,175,true);
            Glyphs(new Vector3(8,6.43f,62),2.1f,12,amber,5,175);
            foreach(var f in art.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="深层漂浮残岛"))f.sharedMesh=broken;
            BackgroundArchitecture(pale,broken);
            // Keep the surfaces darker than the carved edges, and soften emission rather than adding bloom.
            blue.SetColor("_EmissionColor",new Color(.16f,.62f,.76f)*1.55f);
            amber.SetColor("_EmissionColor",new Color(1,.52f,.13f)*2.1f);
            UnityEditor.EditorUtility.SetDirty(blue);UnityEditor.EditorUtility.SetDirty(amber);
        }
        private static void Facility(Vector3 p,Material stone,Material trim,Material glow)
        {
            if (p.z < 20 && KayKitEntranceSetup.Available) return;
            Moulding("灵晶八角基础",p, .35f,1.35f,.16f,stone,8,0,360,false);
            Moulding("灵晶抱箍",p+Vector3.up*.95f,.72f,.98f,.20f,trim,16,12,345,true);
            Glyphs(p+Vector3.up*.19f,1.08f,8,glow);
            for(int i=0;i<3;i++)
            {
                float angle=(30+i*120)*Mathf.Deg2Rad;
                var brace=new GameObject("能量设施弧形支肋",typeof(MeshFilter),typeof(MeshRenderer));brace.transform.SetParent(art,false);
                brace.transform.position=p+new Vector3(Mathf.Cos(angle)*1.03f,.18f,Mathf.Sin(angle)*1.03f);
                brace.transform.rotation=Quaternion.Euler(90,-angle*Mathf.Rad2Deg,0);
                brace.GetComponent<MeshFilter>().sharedMesh=SaveMesh("弧肋模型",Annulus(.67f,.85f,.20f,10,5,100,false));brace.GetComponent<MeshRenderer>().sharedMaterial=stone;
            }
        }
        private static void Moulding(string name,Vector3 p,float inner,float outer,float h,Material material,int segments,float from,float to,bool broken)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(art,false);go.transform.position=p;
            go.GetComponent<MeshFilter>().sharedMesh=SaveMesh(name,Annulus(inner,outer,h,segments,from,to,broken));go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        private static Mesh Annulus(float inner,float outer,float h,int count,float from,float to,bool broken)
        {
            var b=new Sculpture();
            for(int i=0;i<count;i++)
            {
                if(broken && (i%13==4 || i%17==8))continue;
                float a=Mathf.Lerp(from,to,(float)i/count)*Mathf.Deg2Rad,c=Mathf.Lerp(from,to,(float)(i+1)/count)*Mathf.Deg2Rad;
                Vector3 P(float angle,float r,float y)=>new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r);
                float lip=Mathf.Min(.09f,(outer-inner)*.18f);
                var ia=P(a,inner,0);var ib=P(c,inner,0);var oa=P(a,outer,0);var ob=P(c,outer,0);
                var itA=P(a,inner+lip,h);var itB=P(c,inner+lip,h);var otA=P(a,outer-lip,h);var otB=P(c,outer-lip,h);
                b.Quad(itA,itB,otB,otA);b.Quad(oa,otA,otB,ob);b.Quad(ia,ib,itB,itA);b.Quad(ia,oa,ob,ib);
                b.Quad(ia,itA,otA,oa);b.Quad(ib,ob,otB,itB);
            }
            return b.Mesh();
        }
        private static void Glyphs(Vector3 c,float radius,int count,Material material,float from=0,float to=360)
        {
            var b=new Sculpture();
            for(int i=0;i<count;i++)
            {
                float a=Mathf.Lerp(from,to,(float)i/count)*Mathf.Deg2Rad;var outward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var tangent=new Vector3(-outward.z,0,outward.x);var p=c+outward*radius;
                b.Ribbon(p-outward*.19f,p+outward*.19f,.035f);
                b.Ribbon(p-outward*.08f-tangent*.12f,p+outward*.08f+tangent*.12f,.028f);
                if(i%2==0)b.Ribbon(p-outward*.08f+tangent*.12f,p+outward*.08f-tangent*.12f,.028f);
            }
            var go=new GameObject("环座雕刻符文",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(art,false);go.GetComponent<MeshFilter>().sharedMesh=SaveMesh("雕刻符文"+piece++,b.Mesh());go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        private static Mesh CutGem(int sides)
        {
            var b=new Sculpture();float[] heights={-.5f,-.23f,.28f,.55f,.78f};float[] radii={.09f,.47f,.43f,.24f,0};
            Vector3 P(int ring,int i){float a=i*Mathf.PI*2/sides+ring*.055f;return new Vector3(Mathf.Cos(a)*radii[ring],heights[ring],Mathf.Sin(a)*radii[ring]);}
            for(int r=0;r<4;r++)for(int i=0;i<sides;i++){int n=(i+1)%sides;b.Quad(P(r,i),P(r+1,i),P(r+1,n),P(r,n));}
            for(int i=1;i<sides-1;i++)b.Tri(P(0,0),P(0,i+1),P(0,i));return b.Mesh();
        }
        private static Mesh BrokenSlab()
        {
            var b=new Sculpture();var top=new List<Vector3>();var bottom=new List<Vector3>();
            for(int i=0;i<9;i++){float a=i*Mathf.PI*2/9,r=.45f+.07f*Mathf.Sin(i*13);top.Add(new Vector3(Mathf.Cos(a)*r,.1f+Mathf.Sin(i*7)*.04f,Mathf.Sin(a)*r));bottom.Add(new Vector3(Mathf.Cos(a)*r*.65f,-.4f-Mathf.Abs(Mathf.Sin(i*4))*.2f,Mathf.Sin(a)*r*.65f));}
            for(int i=0;i<9;i++){int n=(i+1)%9;b.Tri(Vector3.up*.11f,top[n],top[i]);b.Quad(top[i],top[n],bottom[n],bottom[i]);b.Tri(Vector3.down*.5f,bottom[i],bottom[n]);}return b.Mesh();
        }
        private static Mesh WellLining()
        {
            var b=new Sculpture();var c=new Vector3(-30.5f,1.47f,30);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8,n=(i+1)*Mathf.PI/8;
                Vector3 P(float angle,float depth){float r=1-depth*.05f;return c+new Vector3(Mathf.Cos(angle)*3.4f*r,-depth,Mathf.Sin(angle)*2.35f*r);}
                b.Quad(P(a,0),P(a,2.5f),P(n,2.5f),P(n,0));
            }
            return b.Mesh();
        }
        private static void FloorFan(Vector3 c,Material stone,Material trim)
        {
            var b=new Sculpture();
            for(int i=0;i<14;i++)
            {
                if(i==2 || i==7 || i==8)continue;float a=i*Mathf.PI/7,n=(i+1)*Mathf.PI/7;
                Vector3 P(float angle,float r)=>c+new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);
                float r=5.1f+.65f*Mathf.Sin(i*11);b.Quad(P(a+.025f,3.4f),P(n-.025f,3.4f),P(n-.025f,r),P(a+.025f,r));
            }
            var go=new GameObject("守望破损放射铺石",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(art,false);go.GetComponent<MeshFilter>().sharedMesh=SaveMesh("守望铺石",b.Mesh());go.GetComponent<Renderer>().sharedMaterial=trim;
            Moulding("守望断续石环",c,3.0f,3.35f,.04f,stone,28,0,360,true);
        }
        private static void BackgroundArchitecture(Material stone,Mesh slab)
        {
            var faded=new[]{Mat("近层沉没遗迹",new Color(.15f,.20f,.27f)),Mat("中层雾中遗迹",new Color(.10f,.15f,.22f)),Mat("远层消隐遗迹",new Color(.065f,.10f,.16f))};
            Vector3[] sites={new(16,-5,1),new(-17,-7,4),new(-33,-8,39),new(1,-10,28),new(28,-8,58),new(-11,-12,65)};
            for(int i=0;i<sites.Length;i++)
            {
                var p=sites[i];int layer=i%3;
                var go=new GameObject("深渊断层遗迹",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(art,false);go.transform.position=p;go.transform.localScale=new Vector3(7,3,5);go.transform.rotation=Quaternion.Euler(9,i*37,7);go.GetComponent<MeshFilter>().sharedMesh=slab;go.GetComponent<Renderer>().sharedMaterial=faded[layer];go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var arch=new GameObject("沉没拱廊残影",typeof(MeshFilter),typeof(MeshRenderer));arch.transform.SetParent(art,false);arch.transform.position=p+Vector3.up*.3f;arch.transform.rotation=Quaternion.Euler(-90,i*41,0);
                arch.GetComponent<MeshFilter>().sharedMesh=SaveMesh("远景断拱",Annulus(1.6f,2.05f,.65f,20,5,175,true));arch.GetComponent<Renderer>().sharedMaterial=faded[layer];arch.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        private sealed class Sculpture
        {
            private readonly List<Vector3> vertices=new();private readonly List<int> triangles=new();
            public void Tri(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);}
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
            public void Ribbon(Vector3 a,Vector3 b,float width){var t=Vector3.Cross(Vector3.up,(b-a).normalized)*width;Quad(a+t,a-t,b-t,b+t);}
            public Mesh Mesh(){var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        }
    }
}

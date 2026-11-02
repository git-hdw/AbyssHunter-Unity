using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssHunter.EditorTools
{
    public static partial class AbyssArtSetup
    {
        // Render-only pass. Never edits transforms with gameplay colliders or navigation data.
        private static void PortfolioArtPass(GameObject ruins)
        {
            var limestone=Mat("风化石灰边",new Color(.42f,.45f,.47f),.18f);
            var slate=Mat("冷灰页岩",new Color(.28f,.35f,.40f),.22f);
            var rune=Mat("静谧青纹",new Color(.16f,.47f,.5f),.2f,0,null,new Color(.12f,.55f,.6f));
            var pillar=MeshAsset("八棱断柱",()=>FacetedRock(false));
            var crag=MeshAsset("不规则深渊岩",()=>FacetedRock(true));
            int index=0;
            foreach(var renderer in ruins.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.transform.IsChildOf(art)))
            {
                if(renderer.name is "Fractured pillar" or "Arch left" or "Arch right")
                {
                    renderer.GetComponent<MeshFilter>().sharedMesh=pillar;
                    renderer.sharedMaterial=index++%3==0?limestone:slate;
                }
                if(renderer.name=="Distant sunken tower")
                {renderer.GetComponent<MeshFilter>().sharedMesh=crag;renderer.sharedMaterial=dark;renderer.enabled=false;}
                if(renderer.name is "Pillar crown" or "Pillar footing")renderer.sharedMaterial=limestone;
            }
            // Material and mesh variation without moving the authoritative slab colliders.
            var slabs=ruins.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name is "Worn flagstone" or "Repaired bridge slab").ToArray();
            var variants=Enumerable.Range(0,7).Select(i=>MeshAsset("缺角石板"+i,()=>ChippedSlab(i))).ToArray();
            for(int i=0;i<slabs.Length;i++)
            {
                int hash=Mathf.Abs(Mathf.RoundToInt(slabs[i].transform.position.x*19+slabs[i].transform.position.z*37));
                slabs[i].sharedMaterial=stoneVariants[hash%5];slabs[i].GetComponent<MeshFilter>().sharedMesh=variants[hash%7];
            }
            foreach(var r in art.GetComponentsInChildren<MeshRenderer>())
                if(r.name is "深渊岩脊" or "裂隙晶簇" or "边缘碎石")r.GetComponent<MeshFilter>().sharedMesh=crag;
            // Break the repeated silhouette of the abyss into three irregular depth bands.
            var random=new System.Random(7391);
            foreach(var r in art.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="深渊岩脊"))
            {
                var t=r.transform; t.position+=new Vector3((float)random.NextDouble()*6-3,(float)random.NextDouble()*7-3,(float)random.NextDouble()*8-4);
                t.localScale=Vector3.Scale(t.localScale,new Vector3(.7f+(float)random.NextDouble(),.6f+(float)random.NextDouble(),.7f+(float)random.NextDouble()));
            }
            foreach(float z in new[]{0f,24f,48f})foreach(int side in new[]{-1,1})
            {
                // Stepped ribs at the perimeter frame each arena, leaving its centre empty.
                for(int j=0;j<3;j++)
                {
                    var p=new Vector3(side*(8.9f+j*.38f),.9f-j*.24f,z+3+j*.4f);
                    Shape(PrimitiveType.Cube,"断墙阶肋",p,new Vector3(.7f,1.8f-j*.48f,2.4f),j%2==0?limestone:slate,true).transform.Rotate(0,side*9,side*4);
                }
            }
            foreach(float z in new[]{-1f,19f,47f})foreach(int side in new[]{-1,1})
                Brazier(new Vector3(side*6.3f,.15f,z),limestone);
            // A shattered archivolt marks the vestibule instead of another complete cube doorway.
            for(int i=0;i<7;i++)
            {
                if(i==3 || i==4)continue;
                float a=(25+i*21)*Mathf.Deg2Rad;
                var block=Shape(PrimitiveType.Cube,"前厅破损拱券",new Vector3(Mathf.Cos(a)*6.2f,3.9f+Mathf.Sin(a)*2.1f,-5.1f),new Vector3(1.2f,.55f,1.35f),i%3==0?limestone:stone,true);
                block.transform.Rotate(0,0,a*Mathf.Rad2Deg-90);
            }
            foreach(float z in new[]{12f,36f})foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<4;i++)
                    Shape(PrimitiveType.Cube,"桥侧铜铆钉",new Vector3(side*3.16f,.73f,z-3+i*2),new Vector3(.25f,.07f,.3f),metal);
                var p=new Vector3(side*5,-1.8f,z);
                var shard=Shape(PrimitiveType.Cube,"桥下稀疏紫晶",p,new Vector3(.4f,2.8f,.6f),purple);shard.GetComponent<MeshFilter>().sharedMesh=crag;
            }
            for(int ring=0;ring<2;ring++)for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12,radius=ring==0?3.6f:5.1f;
                var p=new Vector3(Mathf.Cos(a)*radius,.035f,52+Mathf.Sin(a)*radius);
                var glyph=Shape(PrimitiveType.Cube,"仪式断环",p,new Vector3(.10f,.025f,ring==0?.55f:.85f),i%3==0?rune:metal);
                glyph.transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                if(i%6==0)Shape(PrimitiveType.Cube,"仪式星刻",p+new Vector3(0,.015f,0),new Vector3(.38f,.02f,.08f),rune).transform.Rotate(0,-a*Mathf.Rad2Deg,0);
            }
        }
        private static void Brazier(Vector3 p,Material baseStone)
        {
            Shape(PrimitiveType.Cylinder,"火盆八角石座",p,new Vector3(.85f,.18f,.85f),baseStone);
            Shape(PrimitiveType.Cylinder,"火盆青铜托",p+Vector3.up*.65f,new Vector3(.65f,.55f,.65f),metal);
            Shape(PrimitiveType.Cylinder,"火盆碗口",p+Vector3.up*1.18f,new Vector3(1,.12f,1),metal);
            var fire=Shape(PrimitiveType.Sphere,"前厅暖色火焰",p+Vector3.up*1.5f,new Vector3(.28f,.7f,.28f),flame);fire.GetComponent<MeshFilter>().sharedMesh=flameMesh;
            var light=new GameObject("外围火盆暖光").AddComponent<Light>();light.transform.SetParent(art,false);light.transform.position=p+Vector3.up*2;
            light.type=LightType.Point;light.color=new Color(1,.53f,.22f);light.intensity=3.5f;light.range=7;light.shadows=LightShadows.None;
        }
        private static Mesh ChippedSlab(int seed)
        {
            var mesh=UnityEngine.Object.Instantiate(bevel);var vertices=mesh.vertices;
            // Equal positions receive equal offsets, keeping the hard-normal face seams closed.
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];float wave=Mathf.Sin(p.x*83+p.z*47+seed*11);
                vertices[i]=new Vector3(p.x*(.96f+.03f*wave),p.y,p.z*(.96f+.025f*wave));
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh FacetedRock(bool irregular)
        {
            var v=new List<Vector3>();var t=new List<int>();
            void Face(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);}
            Vector3 Point(int ring,int i)
            {
                float a=i*Mathf.PI/4;float[] y={-.5f,-.42f,.29f,.43f};float[] r={.5f,.43f,.35f,.44f};
                float radius=r[ring]*(irregular?1+Mathf.Sin(i*7+ring*13)*.22f:1);
                return new Vector3(Mathf.Cos(a)*radius,y[ring]+(ring==3?Mathf.Sin(i*9)*.07f:0),Mathf.Sin(a)*radius);
            }
            for(int i=0;i<8;i++)
            {
                int next=(i+1)%8;
                Face(Vector3.down*.5f,Point(0,i),Point(0,next));
                Face(Vector3.up*.42f,Point(3,next),Point(3,i));
                for(int ring=0;ring<3;ring++){Face(Point(ring,i),Point(ring+1,next),Point(ring,next));Face(Point(ring,i),Point(ring+1,i),Point(ring+1,next));}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}

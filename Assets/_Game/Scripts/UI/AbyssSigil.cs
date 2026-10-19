using UnityEngine;
using UnityEngine.UI;

namespace AbyssHunter.UI
{
    // Original, resolution-independent menu artwork: a blade inside a broken abyss seal.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AbyssSigil : MaskableGraphic
    {
        private void Update() => SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 c = rectTransform.rect.center;
            float r = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .42f;
            for (int ring = 0; ring < 5; ring++)
            {
                float radius = r * (1 - ring * .105f);
                for (int s = 0; s < 120; s++)
                {
                    if ((s + ring * 5) % 30 > 25) continue;
                    float turn = Time.unscaledTime * .025f * (ring % 2 == 0 ? 1 : -1);
                    float a = s * Mathf.PI / 60 + turn, b = (s + .8f) * Mathf.PI / 60 + turn;
                    Color col = ring == 1 ? new Color(.3f,.8f,.77f,.6f) : new Color(.17f,.4f,.46f,.35f);
                    Quad(vh, c + Dir(a)*radius, c + Dir(b)*radius,
                        c + Dir(b)*(radius-2), c + Dir(a)*(radius-2), col);
                }
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                Vector2 p = c + Dir(a) * r * 1.07f;
                Quad(vh, p+new Vector2(0,5),p+new Vector2(3,0),p-new Vector2(0,5),p-new Vector2(3,0),PresentationTheme.Gold);
            }
            Quad(vh,c+new Vector2(0,210),c+new Vector2(30,-60),c+new Vector2(0,-105),c+new Vector2(-30,-60),new Color(.23f,.65f,.65f));
            Tri(vh,c+new Vector2(0,210),c+new Vector2(0,-105),c+new Vector2(-30,-60),PresentationTheme.Cyan);
            Quad(vh,c+new Vector2(-92,-60),c+new Vector2(0,-42),c+new Vector2(92,-60),c+new Vector2(0,-82),PresentationTheme.Gold);
            Quad(vh,c+new Vector2(-8,-80),c+new Vector2(8,-80),c+new Vector2(8,-155),c+new Vector2(-8,-155),PresentationTheme.Gold);
            Quad(vh,c+new Vector2(0,-150),c+new Vector2(15,-170),c+new Vector2(0,-190),c+new Vector2(-15,-170),PresentationTheme.Gold);
        }
        private static Vector2 Dir(float a) => new(Mathf.Cos(a), Mathf.Sin(a));
        private static void Tri(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int i=v.currentVertCount;
            v.AddVert(a,col,Vector2.zero);v.AddVert(b,col,Vector2.zero);v.AddVert(c,col,Vector2.zero);
            v.AddTriangle(i,i+1,i+2);
        }
        private static void Quad(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
        { Tri(v,a,b,c,col); Tri(v,a,c,d,col); }
    }
}

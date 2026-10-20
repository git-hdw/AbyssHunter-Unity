using AbyssHunter.Character;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssHunter.World
{
    [RequireComponent(typeof(Renderer))]
    public sealed class AbyssOccluder : MonoBehaviour
    {
        private Renderer surface;
        private Material original, faded;
        private Camera view;
        private Transform hunter;
        private Color color;
        private float visibility = 1;
        private ShadowCastingMode originalShadows;
        private bool usingFaded;
        public bool IsFaded => visibility < .95f;
        private void Start()
        {
            surface = GetComponent<Renderer>();
            original = surface.sharedMaterial;
            originalShadows = surface.shadowCastingMode;
            color = original.GetColor("_BaseColor");
            faded = new Material(original);
            faded.SetFloat("_Surface", 1); faded.SetFloat("_ZWrite", 0);
            faded.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            faded.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            faded.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            faded.SetOverrideTag("RenderType", "Transparent");
            faded.renderQueue = (int)RenderQueue.Transparent;
            view = Camera.main;
            hunter = FindFirstObjectByType<PlayerMovement>()?.transform;
        }
        private void LateUpdate()
        {
            if (view == null || hunter == null) return;
            Vector3 delta = hunter.position + Vector3.up * .35f - view.transform.position;
            var bounds = surface.bounds; bounds.Expand(.6f);
            bool blocks = bounds.IntersectRay(new Ray(view.transform.position, delta.normalized), out float distance) && distance < delta.magnitude;
            visibility = Mathf.MoveTowards(visibility, blocks ? .12f : 1, Time.unscaledDeltaTime * 4);
            bool transparent = visibility < .999f;
            if (transparent != usingFaded)
            {
                surface.sharedMaterial = transparent ? faded : original;
                surface.shadowCastingMode = transparent ? ShadowCastingMode.Off : originalShadows;
                usingFaded = transparent;
            }
            if (transparent) { color.a = visibility; faded.SetColor("_BaseColor", color); }
        }
        private void OnDestroy() { if (faded != null) Destroy(faded); }
    }
}

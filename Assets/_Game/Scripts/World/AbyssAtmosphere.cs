using System.Collections;
using System.Linq;
using AbyssHunter.UI;
using UnityEngine;

namespace AbyssHunter.World
{
    public sealed class AbyssAtmosphere : MonoBehaviour
    {
        [SerializeField] private Light[] routeLights;
        [SerializeField] private Renderer[] routeRunes;
        [SerializeField] private Material awakenedRune;
        [SerializeField] private Light altarLight, beaconLight;
        [SerializeField] private Transform core, travellingEmber, beaconFlame;
        [SerializeField] private LastLight lantern;
        [SerializeField] private Transform[] floatingFragments = System.Array.Empty<Transform>();
        private Vector3[] fragmentOrigins;
        private AbyssExpedition mission;
        private bool recovering;
        private Vector3 coreOrigin, beaconScale;
        private float time, beaconStrength;
        public bool RouteAwakened { get; private set; }
        public float BeaconStrength => beaconStrength;
        private void Awake()
        {
            mission = GetComponent<AbyssExpedition>();
            coreOrigin = core.localPosition;
            beaconScale = beaconFlame.localScale;
            travellingEmber.gameObject.SetActive(false);
            beaconFlame.localScale = Vector3.zero;
            fragmentOrigins = floatingFragments.Select(t => t.localPosition).ToArray();
        }
        public void RecoverCore()
        {
            if (!recovering) { recovering = true; StartCoroutine(AwakenRoute()); }
        }
        private IEnumerator AwakenRoute()
        {
            Vector3 origin = core.position;
            travellingEmber.gameObject.SetActive(true);
            for (float t = 0; t < 1; t += Time.deltaTime / .8f)
            {
                travellingEmber.position = Vector3.Lerp(origin, lantern.transform.position, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .7f;
                travellingEmber.localScale = Vector3.one * Mathf.Lerp(.22f, .06f, t);
                altarLight.intensity = Mathf.Lerp(2.5f, .3f, t);
                yield return null;
            }
            travellingEmber.gameObject.SetActive(false);
            foreach (var rune in routeRunes.OrderByDescending(r => r.transform.position.z))
            {
                rune.sharedMaterial = awakenedRune;
                yield return new WaitForSeconds(.045f);
            }
            foreach (var lamp in routeLights.OrderByDescending(l => l.transform.position.z))
            {
                lamp.color = new Color(.3f, .63f, .7f);
                lamp.intensity = 1.1f;
                yield return new WaitForSeconds(.08f);
            }
            RouteAwakened = true;
        }
        private void Update()
        {
            var ui = GamePresentation.Current;
            bool pause = ui != null && (ui.State == GamePresentation.ScreenState.Paused || ui.State == GamePresentation.ScreenState.Inventory);
            float dt = pause ? 0 : GamePresentation.AnimationDelta;
            time += dt;
            for (int i = 0; i < floatingFragments.Length; i++)
            {
                var fragment = floatingFragments[i];
                fragment.localPosition = fragmentOrigins[i] + Vector3.up * Mathf.Sin(time * .9f + i * .7f) * .1f;
                fragment.Rotate(Vector3.up, dt * (i % 2 == 0 ? 5 : -5), Space.World);
            }
            if (core.gameObject.activeSelf) core.localPosition = coreOrigin + Vector3.up * Mathf.Sin(time * 1.4f) * .12f;
            beaconStrength = Mathf.MoveTowards(beaconStrength, mission.BeaconLit ? 1 : 0, dt * 1.1f);
            beaconFlame.localScale = beaconScale * beaconStrength * (1 + Mathf.Sin(time * 4) * .035f);
            beaconLight.color = new Color(1, .62f, .23f);
            beaconLight.range = Mathf.Lerp(2, 7, beaconStrength);
            beaconLight.intensity = Mathf.Lerp(.08f, 4.5f, beaconStrength);
        }
    }
}

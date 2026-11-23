using System.Collections.Generic;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace AbyssHunter.Enemy
{
    [RequireComponent(typeof(EnemyController))]
    public sealed class HeavyAttackPresentation : MonoBehaviour
    {
        [SerializeField] private Material telegraphMaterial;
        private EnemyController controller;
        private MeshRenderer sector;
        private Mesh mesh;
        private MaterialPropertyBlock properties;
        private TextMesh phaseLabel;
        private AudioSource source;
        private AudioClip chargeSound, impactSound;
        private bool audioPaused;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ChargeRadiusId = Shader.PropertyToID("_ChargeRadius");

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            properties = new MaterialPropertyBlock();
            var warning = new GameObject("Heavy attack sector", typeof(MeshFilter), typeof(MeshRenderer));
            warning.transform.SetParent(transform, false);
            warning.transform.localPosition = Vector3.down * (GetComponent<NavMeshAgent>().baseOffset - .04f);
            mesh = BuildSector(controller.HeavyAttack.range, controller.HeavyAttack.angle);
            warning.GetComponent<MeshFilter>().sharedMesh = mesh;
            sector = warning.GetComponent<MeshRenderer>();
            sector.sharedMaterial = telegraphMaterial;
            sector.shadowCastingMode = ShadowCastingMode.Off; sector.receiveShadows = false;
            var label = new GameObject("Heavy phase label"); label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0, 3.4f, 0);
            phaseLabel = label.AddComponent<TextMesh>();
            phaseLabel.font = AbyssHunter.UI.GameFont.Chinese;
            label.GetComponent<MeshRenderer>().sharedMaterial = phaseLabel.font.material;
            phaseLabel.fontSize = 48; phaseLabel.characterSize = .055f;
            phaseLabel.anchor = TextAnchor.MiddleCenter;
            source = gameObject.AddComponent<AudioSource>();source.playOnAwake = false;source.spatialBlend = 0;source.volume = .3f;
            chargeSound = Tone("Heavy charge", .8f, true);
            impactSound = Tone("Heavy impact", .3f, false);
        }

        private void OnEnable()
        {
            controller.StateChanged += OnState;
            controller.HeavyImpact += OnImpact;
            sector.enabled = false;
            audioPaused = false;
        }
        private void OnDisable()
        {
            controller.StateChanged -= OnState;
            controller.HeavyImpact -= OnImpact;
            sector.enabled = false; phaseLabel.text = ""; source.Stop();
        }
        private void OnState(EnemyState state)
        {
            if (state == EnemyState.Windup) { source.pitch = .8f / controller.HeavyAttack.windup; source.PlayOneShot(chargeSound); }
            if (state == EnemyState.Idle) { source.Stop(); sector.enabled = false; }
        }
        private void OnImpact()
        {
            source.pitch = 1; source.PlayOneShot(impactSound);
            HitImpactVfx.Spawn(transform.position + transform.forward * 1.7f + Vector3.down * .7f, new Color(1,.55f,.15f));
            if (Camera.main != null) Camera.main.GetComponent<TopDownCamera>()?.Shake(.16f,.1f);
        }
        private void LateUpdate()
        {
            if (!controller.enabled) { sector.enabled = false; phaseLabel.text = ""; return; }
            var state = controller.CurrentState;
            sector.enabled = state == EnemyState.Windup || state == EnemyState.Strike;
            Color color;
            float chargeRadius = -1f;
            bool impactImminent = false;
            if (state == EnemyState.Windup)
            {
                bool locked = controller.PhaseElapsed >= controller.HeavyAttack.trackingTime;
                float lockedDuration = controller.HeavyAttack.windup - controller.HeavyAttack.trackingTime;
                float remaining = controller.HeavyAttack.windup - controller.PhaseElapsed;
                impactImminent = locked && remaining <= Mathf.Min(.2f, lockedDuration * .5f);
                color = locked ? new Color(1,.25f,.06f) : new Color(1,.7f,.15f);
                phaseLabel.text = impactImminent ? "即将落锤" : locked ? "方向已锁定" : "重锤兵 · 蓄力";
                color.a = .9f;
                chargeRadius = controller.HeavyAttack.range *
                    Mathf.Clamp01(controller.PhaseElapsed / controller.HeavyAttack.windup);
            }
            else if (state == EnemyState.Strike) {color = Color.white;phaseLabel.text = "重击！";}
            else if (state == EnemyState.Recovery)
            {
                color = new Color(.3f, 1, .85f);
                phaseLabel.text = controller.IsRecoveryStaggered ? "反击命中" : "破绽 · 趁机反击";
            }
            else {color = new Color(1,.75f,.3f);phaseLabel.text = "重锤兵";}
            phaseLabel.color = impactImminent ? new Color(1f, .84f, .48f) : color;
            phaseLabel.characterSize = impactImminent ? .068f : .055f;
            if (sector.enabled)
            {
                properties.SetColor(ColorId, color);
                properties.SetFloat(ChargeRadiusId, chargeRadius);
                sector.SetPropertyBlock(properties);
            }
            if (Camera.main != null) phaseLabel.transform.rotation = Camera.main.transform.rotation;
            bool paused = Time.timeScale <= 0;
            if (paused != audioPaused)
            {
                if (paused) source.Pause(); else source.UnPause();
                audioPaused = paused;
            }
        }

        private static Mesh BuildSector(float radius, float angle)
        {
            const int steps=40;
            var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            void Triangle(Vector3 a,Vector3 b,Vector3 c,float alpha)
            {
                int n=vertices.Count; vertices.Add(a);vertices.Add(b);vertices.Add(c);
                for(int k=0;k<3;k++){colors.Add(new Color(1,1,1,alpha));indices.Add(n+k);}
            }
            Vector3 Point(float degrees,float r) => new Vector3(Mathf.Sin(degrees*Mathf.Deg2Rad)*r,0,Mathf.Cos(degrees*Mathf.Deg2Rad)*r);
            for(int i=0;i<steps;i++)
            {
                float a=-angle*.5f+angle*i/steps,b=-angle*.5f+angle*(i+1)/steps;
                Triangle(Vector3.zero,Point(a,radius),Point(b,radius),.2f);
                Triangle(Point(a,radius-.06f),Point(a,radius),Point(b,radius),1);
                Triangle(Point(a,radius-.06f),Point(b,radius),Point(b,radius-.06f),1);
            }
            Triangle(Vector3.zero,Point(-angle*.5f,radius),Point(-angle*.5f+1.2f,radius),1);
            Triangle(Vector3.zero,Point(angle*.5f-1.2f,radius),Point(angle*.5f,radius),1);
            var result=new Mesh{name="Heavy attack sector"};result.SetVertices(vertices);result.SetColors(colors);result.SetTriangles(indices,0);result.RecalculateBounds();return result;
        }
        private static AudioClip Tone(string name,float duration,bool rising)
        {
            const int rate=24000;var data=new float[(int)(rate*duration)];
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate, u=t/duration;
                float phase=rising ? 2*Mathf.PI*(100*t+150*t*t) : 2*Mathf.PI*(65*t+10*(1-Mathf.Exp(-t*30)));
                data[i]=Mathf.Sin(phase)*(rising?Mathf.Sin(u*Mathf.PI)*.3f:Mathf.Exp(-u*7)*.8f);
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        private void OnDestroy(){Destroy(mesh);Destroy(chargeSound);Destroy(impactSound);}
    }
}

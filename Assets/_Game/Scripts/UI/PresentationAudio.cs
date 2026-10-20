using UnityEngine;

namespace AbyssHunter.UI
{
    // Original synthesized ambient bed; no third-party music or runtime file loading.
    internal sealed class PresentationAudio : MonoBehaviour
    {
        private AudioClip ambience;
        private AudioSource source;
        private AbyssHunter.World.AbyssExpedition expedition;
        private void Awake()
        {
            const int rate=24000;
            const float duration=12.8f;
            var samples=new float[(int)(rate*duration)];
            float[] tones={110,130.8128f,164.8138f,196};
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate;
                float fade=Mathf.Min(1,Mathf.Min(t,duration-t)*2);
                float value=0;
                foreach(float hz in tones) value+=Mathf.Sin(2*Mathf.PI*hz*t)*.024f;
                float beat=t%.8f;
                value+=Mathf.Sin(2*Mathf.PI*55*t)*Mathf.Exp(-beat*12)*.065f;
                samples[i]=value*fade*(.8f+.2f*Mathf.Sin(t*Mathf.PI/6.4f));
            }
            ambience=AudioClip.Create("Abyss - original ambient",samples.Length,1,rate,false);
            ambience.SetData(samples,0);
            source=gameObject.AddComponent<AudioSource>();
            expedition=FindFirstObjectByType<AbyssHunter.World.AbyssExpedition>();
            source.clip=ambience; source.loop=true; source.volume=.45f; source.spatialBlend=0;
            source.Play();
        }
        private void Update()
        {
            float target = expedition == null ? .45f : expedition.BeaconLit ? .07f : .2f;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * .25f);
        }
        private void OnDestroy() {if(ambience!=null) Destroy(ambience);}
    }
}

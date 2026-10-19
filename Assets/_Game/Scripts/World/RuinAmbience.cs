using AbyssHunter.Character;
using AbyssHunter.UI;
using UnityEngine;

namespace AbyssHunter.World
{
    // Quiet stone footfalls complement the heavy enemy's existing charge and impact sounds.
    public sealed class RuinAmbience : MonoBehaviour
    {
        private PlayerMovement hunter;
        private Vector3 previous;
        private float distance;
        private AudioSource source;
        private AudioClip step;
        private void Start()
        {
            hunter=FindFirstObjectByType<PlayerMovement>(); previous=hunter.transform.position;
            source=gameObject.AddComponent<AudioSource>();source.volume=.14f;source.playOnAwake=false;
            const int rate=16000;var samples=new float[2400];var noise=new System.Random(719);
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate;samples[i]=((float)noise.NextDouble()*2-1)*Mathf.Exp(-t*45)*.5f+Mathf.Sin(t*2*Mathf.PI*100)*Mathf.Exp(-t*60)*.3f;}
            step=AudioClip.Create("石板脚步",samples.Length,1,rate,false);step.SetData(samples,0);
        }
        private void Update()
        {
            if(hunter==null)return;
            float moved=Vector3.Distance(previous,hunter.transform.position);previous=hunter.transform.position;
            if(GamePresentation.BlocksGameplay||hunter.IsDodging||moved>1)return;
            distance+=moved;
            if(distance>1.4f){distance=0;source.PlayOneShot(step);}
        }
        private void OnDestroy(){if(step!=null)Destroy(step);}
    }
}

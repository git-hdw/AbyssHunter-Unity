#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using AbyssHunter.Inventory;
using AbyssHunter.Save;
using AbyssHunter.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace AbyssHunter.World
{
    // Editor-only fixtures: lethal damage isolates progression checks from combat balancing.
    public sealed class ExpeditionVerification : MonoBehaviour
    {
        private readonly List<string> report=new();
        private Keyboard keyboard;
        private PlayerMovement player;
        private Health health;
        private AbyssExpedition mission;
        private EnemyWaveSpawner spawner;
        private int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if(!SessionState.GetBool("AbyssHunter.ExpeditionTest",false))return;
            SessionState.EraseBool("AbyssHunter.ExpeditionTest");
            DontDestroyOnLoad(new GameObject("Expedition verification").AddComponent<ExpeditionVerification>());
        }
        private void Check(bool pass,string message){report.Add((pass?"PASS | ":"FAIL | ")+message);File.WriteAllLines("Logs/expedition-verification.txt",report);}
        private void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Check(false,message+"\n"+stack);}}
        private IEnumerator Start()
        {
            Application.runInBackground=true;Application.logMessageReceived+=OnLog;
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();Directory.CreateDirectory("Logs/expedition-review");
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool moved;object value=null;var top=stack.Peek();
                try{moved=top.MoveNext();if(moved)value=top.Current;}catch(Exception e){Check(false,e.ToString());break;}
                if(!moved){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
            }
            Check(errors==0,"No runtime errors or exceptions.");report.Add("DONE");File.WriteAllLines("Logs/expedition-verification.txt",report);
            InputSystem.RemoveDevice(keyboard);EditorApplication.isPlaying=false;
        }
        private void Place(Vector3 position){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=position;cc.enabled=true;}
        private IEnumerator Press(Key key)
        {keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
        private IEnumerator Walk(Vector3 target)
        {
            float end=Time.realtimeSinceStartup+18;
            while(Time.realtimeSinceStartup<end)
            {
                Vector3 delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.7f)break;
                var keys=new List<Key>();if(delta.x>.35f)keys.Add(Key.D);else if(delta.x<-.35f)keys.Add(Key.A);
                if(delta.z>.35f)keys.Add(Key.W);else if(delta.z<-.35f)keys.Add(Key.S);
                keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));yield return null;
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            var remaining=target-player.transform.position;remaining.y=0;
            Check(remaining.magnitude<1,"Actual WASD reaches "+target+" (remaining "+remaining.magnitude.ToString("F2")+").");
        }
        private void Snapshot(string name)
        {
            Canvas.ForceUpdateCanvases();var texts=GamePresentation.Current.GetComponentsInChildren<Text>();
            Check(texts.All(t=>t.font==GameFont.Chinese&&t.text.All(c=>c<'\u4e00'||c>'\u9fff'||t.font.HasCharacter(c))),name+": Chinese glyphs available.");
            var clipped=texts.Where(t=>t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text)&&t.preferredHeight>t.rectTransform.rect.height+1).Select(t=>t.name+":"+t.text).ToArray();
            Check(clipped.Length==0,name+": UI text fits. "+string.Join(" / ",clipped));
            ScreenCapture.CaptureScreenshot("Logs/expedition-review/"+name+".png");
        }
        private IEnumerator Run()
        {
            yield return null;player=FindFirstObjectByType<PlayerMovement>();health=player.GetComponent<Health>();
            mission=FindFirstObjectByType<AbyssExpedition>();spawner=FindFirstObjectByType<EnemyWaveSpawner>();health.SetInvulnerable(true);
            Check(mission!=null&&spawner.SpatialEncounters,"Scene contains expedition and spatial encounters.");
            Check(!mission.TryInteract(),"Opening blocks interaction.");
            float deadline=Time.realtimeSinceStartup+8;while(GamePresentation.BlocksGameplay&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(!mission.TryInteract()&&!mission.BeaconLit,"Cannot activate beacon without core.");
            var save=FindFirstObjectByType<SaveManager>();save.SaveGame();Check(!save.HasCheckpoint,"Player-only checkpoint is disabled for expedition.");
            Place(mission.Core.position+Vector3.up);Check(!mission.TryInteract()&&!mission.HasCore,"Cannot take sealed core before hunting.");Place(new Vector3(0,1,-5));
            foreach(float z in new[]{0f,24f,48f,52f})
            {
                var path=new NavMeshPath();bool found=NavMesh.CalculatePath(new Vector3(0,0,-5),new Vector3(0,0,z),NavMesh.AllAreas,path);
                Check(found&&path.status==NavMeshPathStatus.PathComplete,"Navigation connects entrance to z="+z);
            }
            yield return Walk(new Vector3(0,0,-3));
            for(int area=1;area<=3;area++)
            {
                if(area>1)
                {
                    yield return new WaitForSeconds(1);
                    Check(spawner.CurrentWave==area-1&&spawner.ActiveEnemies==0,"Next encounter waits for physical approach: "+area);
                    yield return Walk(new Vector3(0,0,(area-1)*24-4));
                }
                deadline=Time.realtimeSinceStartup+8;
                int expected=area==1?2:area==2?1:3;
                while(spawner.ActiveEnemies<expected&&Time.realtimeSinceStartup<deadline)yield return null;
                Check(spawner.CurrentWave==area&&spawner.ActiveEnemies==expected,"Correct authored encounter spawned: "+area);
                yield return new WaitForSeconds(.4f);Snapshot("0"+area+"-encounter");
                foreach(var enemy in FindObjectsByType<PooledEnemy>(FindObjectsSortMode.None))enemy.GetComponent<Health>().TakeDamage(1000);
                yield return new WaitForSeconds(1.4f);
                Check(spawner.CompletedEncounters==area,"Encounter clears once: "+area);
                foreach(var drop in FindObjectsByType<PickupItem>(FindObjectsSortMode.None).Where(p=>p.Item.ItemId=="abyss_shard").ToArray())
                    if(drop!=null)yield return Walk(drop.transform.position);
                yield return Walk(new Vector3(0,0,(area-1)*24));
            }
            Check(GamePresentation.Current.DefeatedCount==6&&!MatchResult.HasEnded,"Six kills do not end expedition before extraction.");
            Check(mission.CarriedShards==6,"Six enemies guarantee exactly six collectible fragments.");
            yield return Walk(mission.Core.position);GamePresentation.Current.ShowPause();
            Check(!mission.TryInteract()&&!mission.HasCore,"Pause prevents core collection.");GamePresentation.Current.Resume();
            yield return Press(Key.E);Check(mission.HasCore&&!MatchResult.HasEnded,"Real E input recovers core without premature victory.");
            Check(!mission.TryInteract(),"Core cannot be collected twice.");Snapshot("04-core-recovered");
            // Follow the same bridges back using movement input; navigation is never bypassed here.
            yield return Walk(new Vector3(0,0,36));yield return Walk(new Vector3(0,0,12));yield return Walk(mission.Beacon.position);
            var inventory=player.GetComponent<PlayerInventory>();var shard=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Game/Items/AbyssShard.asset");
            inventory.TryConsume(shard,6);Check(!mission.TryInteract()&&!mission.BeaconLit,"Insufficient fragments cannot activate beacon.");
            inventory.TryAdd(shard,6,out _);yield return Press(Key.E);
            Check(mission.BeaconLit&&mission.CarriedShards==2&&mission.DepositedShards==4&&!MatchResult.HasEnded,"Beacon consumes exactly four fragments and awaits deliberate extraction.");
            Snapshot("05-beacon-lit");
            Place(new Vector3(0,1,0));Check(!mission.TryInteract(),"Cannot extract remotely.");Place(mission.Beacon.position+Vector3.up);
            GamePresentation.Current.ShowInventory();Check(!mission.TryInteract(),"Inventory pauses extraction.");GamePresentation.Current.Resume();
            yield return Press(Key.E);yield return new WaitForSecondsRealtime(1.3f);
            Check(mission.Extracted&&MatchResult.HasEnded&&GamePresentation.Current.State==GamePresentation.ScreenState.Result,"Real E input extracts and shows victory.");
            Check(!mission.TryInteract(),"Completion is idempotent.");Snapshot("06-extracted");
            GamePresentation.Current.Restart();yield return new WaitForSecondsRealtime(1);
            var fresh=FindFirstObjectByType<AbyssExpedition>();Check(fresh!=null&&!fresh.HasCore&&!fresh.BeaconLit&&!MatchResult.HasEnded,"Restart resets mission, core and beacon.");
        }
        private void OnDestroy()=>Application.logMessageReceived-=OnLog;
    }
}
#endif

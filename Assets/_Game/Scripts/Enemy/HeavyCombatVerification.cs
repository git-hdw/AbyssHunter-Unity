#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AbyssHunter.Enemy
{
    // Editor-only integration fixtures. Not part of normal gameplay or demo input replay.
    public sealed class HeavyCombatVerification : MonoBehaviour
    {
        private readonly List<string> report=new();
        private Keyboard keyboard;private Mouse mouse;
        private PlayerMovement player;private Health playerHealth;private PooledEnemy brute;
        private EnemyPool pool;private int errors;
        private static string Output => Path.GetFullPath("Logs/heavy-combat-verification.txt");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if(!SessionState.GetBool("AbyssHunter.HeavyTest",false))return;
            SessionState.EraseBool("AbyssHunter.HeavyTest");
            var obj=new GameObject("Heavy combat integration verification").AddComponent<HeavyCombatVerification>();
            DontDestroyOnLoad(obj);
        }
        private void Awake()
        {
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
            Application.runInBackground=true;Application.logMessageReceived+=OnLog;
        }
        private void OnLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception){errors++;Check(false,message+"\n"+stack);}}
        private void Check(bool pass,string message){report.Add((pass?"PASS | ":"FAIL | ")+message);File.WriteAllLines(Output,report);}
        private IEnumerator Start()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                var top=stack.Peek();bool moved;object current=null;
                try{moved=top.MoveNext();if(moved)current=top.Current;}
                catch(Exception e){Check(false,e.ToString());break;}
                if(!moved){stack.Pop();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            Check(errors==0,"No runtime errors or exceptions.");
            report.Add("DONE | Heavy combat integration verification completed.");File.WriteAllLines(Output,report);
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            EditorApplication.isPlaying=false;
        }
        private IEnumerator Press(Key key,float hold=.04f)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(hold);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        }
        private IEnumerator WaitState(EnemyState state,float timeout=4)
        {
            float end=Time.realtimeSinceStartup+timeout;
            while(brute.Controller.CurrentState!=state && Time.realtimeSinceStartup<end)yield return null;
            Check(brute.Controller.CurrentState==state,"Reached heavy phase: "+state);
        }
        private void PlacePlayer(Vector3 p)
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=p;cc.enabled=true;
        }
        private void ResetDuel()
        {
            brute.gameObject.SetActive(false);
            brute.transform.SetPositionAndRotation(new Vector3(-4,1,0),Quaternion.identity);
            brute.gameObject.SetActive(true);
            brute.GetComponent<NavMeshAgent>().Warp(new Vector3(-4,1,0));
            playerHealth.RestoreFullHealth();PlacePlayer(new Vector3(-4,1,2));
            brute.Controller.SetTarget(player.transform,true);
        }
        private IEnumerator Run()
        {
            Directory.CreateDirectory("Logs/heavy-combat");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="SampleScene") {SceneFlow.LoadGame();yield return null;}
            var spawner=FindFirstObjectByType<EnemyWaveSpawner>();spawner.StopAllCoroutines();
            player=FindFirstObjectByType<PlayerMovement>();playerHealth=player.GetComponent<Health>();
            pool=FindObjectsByType<EnemyPool>(FindObjectsSortMode.None).First(p=>p.name=="HeavyEnemyPool");
            yield return pool.WaitUntilReady();
            while(GamePresentation.BlocksGameplay)yield return null;
            Check(pool.IsReady&&!pool.HasFailed,"Heavy Addressable prefab loads and prewarms.");
            brute=pool.Get(new Vector3(-4,1,0),Quaternion.identity);ResetDuel();
            var c=brute.Controller;var hp=brute.GetComponent<Health>();
            Check(c.IsHeavy && hp.MaximumHealth==150,"Heavy archetype has profile and 150 HP.");
            Check(c.IsInsideHeavySector(brute.transform.position+Vector3.forward*2) &&
                !c.IsInsideHeavySector(brute.transform.position+Vector3.back*2) &&
                !c.IsInsideHeavySector(brute.transform.position+Vector3.forward*3.2f),"Sector includes front, excludes rear and out-of-range points.");
            yield return WaitState(EnemyState.Windup);
            hp.TakeDamage(25);
            Check(hp.CurrentHealth==125 && !c.TryApplyKnockback(Vector3.back,1.2f,.15f),"Windup takes damage but resists knockback and interruption.");
            c.ApplyAttackDamage();Check(playerHealth.CurrentHealth==100,"Legacy animation event cannot damage during heavy windup.");
            while(c.PhaseElapsed<.4f)yield return null;
            GamePresentation.Current.ShowPause();float elapsed=c.PhaseElapsed;
            yield return new WaitForSecondsRealtime(.3f);
            Check(Mathf.Approximately(c.PhaseElapsed,elapsed)&&playerHealth.CurrentHealth==100,"Pause freezes telegraph and attack phase.");
            ScreenCapture.CaptureScreenshot("Logs/heavy-combat/01-paused-windup.png");
            GamePresentation.Current.Resume();
            yield return null;ScreenCapture.CaptureScreenshot("Logs/heavy-combat/02-locked.png");
            var phaseLabel=brute.GetComponentInChildren<TextMesh>();
            Check(phaseLabel.font==GameFont.Chinese && phaseLabel.text=="方向已锁定" &&
                phaseLabel.text.All(ch=>phaseLabel.font.HasCharacter(ch)),"Heavy lock-on cue renders with the bundled Chinese font.");
            yield return WaitState(EnemyState.Recovery);
            yield return null;
            Check(phaseLabel.text=="破绽 · 趁机反击" && phaseLabel.text.All(ch=>phaseLabel.font.HasCharacter(ch)),
                "Heavy punish cue renders in Chinese without missing characters.");
            Check(playerHealth.CurrentHealth==75,"Standing in the committed sector takes exactly 25 damage.");
            c.ApplyAttackDamage();c.ApplyAttackDamage();
            Check(playerHealth.CurrentHealth==75,"Repeated clip events do not duplicate the heavy hit.");
            Check(c.TryApplyKnockback(Vector3.back,1.2f,.15f),"Recovery accepts a punish knockback.");
            Check(!c.TryApplyKnockback(Vector3.back,1.2f,.15f)&&Mathf.Approximately(c.RecoveryDuration,1.35f),"Recovery extends once to 1.35 seconds; repeated hits cannot stunlock.");
            ScreenCapture.CaptureScreenshot("Logs/heavy-combat/03-recovery.png");
            PlacePlayer(new Vector3(-4,1,8));yield return new WaitForSeconds(1.6f);
            Check(c.CurrentState==EnemyState.Chase,"Recovery finishes and resumes pursuit.");

            ResetDuel();yield return WaitState(EnemyState.Windup);
            while(c.PhaseElapsed<.4f)yield return null;
            Quaternion facing=c.transform.rotation;
            PlacePlayer(c.transform.position+Vector3.right*2);
            yield return new WaitForSeconds(.2f);
            Check(Quaternion.Angle(facing,c.transform.rotation)<.5f,"Final windup locks direction instead of tracking a sidestep.");
            yield return WaitState(EnemyState.Recovery);
            Check(playerHealth.CurrentHealth==100,"Sidestepping the locked sector avoids damage and still earns recovery.");

            // Zero displacement isolates the invulnerability window from spatial avoidance.
            var speed=typeof(PlayerMovement).GetField("dodgeSpeed",BindingFlags.Instance|BindingFlags.NonPublic);
            float original=(float)speed.GetValue(player);speed.SetValue(player,0f);
            ResetDuel();yield return WaitState(EnemyState.Windup);
            while(c.PhaseElapsed<.1f)yield return null;
            yield return Press(Key.Space);Check(playerHealth.IsInvulnerable,"Actual Space input grants dodge invulnerability.");
            yield return WaitState(EnemyState.Recovery);
            Check(playerHealth.CurrentHealth==75,"An early dodge ending inside the impact area takes damage.");
            yield return new WaitForSeconds(.3f);
            ResetDuel();yield return WaitState(EnemyState.Windup);
            while(c.PhaseElapsed<.68f)yield return null;
            yield return Press(Key.Space);yield return WaitState(EnemyState.Recovery);
            Check(playerHealth.CurrentHealth==100,"A timed dodge covering impact avoids damage while remaining in range.");
            speed.SetValue(player,original);

            ResetDuel();yield return WaitState(EnemyState.Windup);
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Verification obstacle";
            blocker.transform.position = new Vector3(-4, 1, 1);
            blocker.transform.localScale = new Vector3(2, 3, .2f);
            yield return WaitState(EnemyState.Recovery);
            Check(playerHealth.CurrentHealth==100,"Solid arena geometry blocks a heavy hit through a wall.");
            Destroy(blocker);yield return null;

            PlacePlayer(brute.transform.position + Vector3.forward * 1.6f);
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse,new MouseState {buttons=1,position=new Vector2(800,450)});
            yield return new WaitForSecondsRealtime(.04f);
            InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(800,450)});
            yield return new WaitForSeconds(.45f);
            Check(hp.CurrentHealth==125 && Mathf.Approximately(c.RecoveryDuration,1.35f),
                "A real mouse attack lands through its animation event and extends the recovery punish window.");

            ResetDuel();yield return WaitState(EnemyState.Windup);
            int id=brute.GetInstanceID();hp.TakeDamage(999);
            yield return new WaitForSeconds(.9f);
            Check(playerHealth.CurrentHealth==100 && !c.enabled,"Death cancels a pending slam.");
            yield return new WaitForSeconds(.3f);
            Check(!brute.gameObject.activeSelf && pool.AvailableCount==1,"Dead brute returns to its own pool.");
            brute=pool.Get(new Vector3(-4,1,0),Quaternion.identity);c=brute.Controller;hp=brute.GetComponent<Health>();
            Check(brute.GetInstanceID()==id && hp.CurrentHealth==150 && c.CurrentState==EnemyState.Idle && !c.IsArmored,
                "Reusing the same instance resets health, attack phase and armor.");
            Check(!brute.transform.Find("Heavy attack sector").GetComponent<MeshRenderer>().enabled,
                "Reused brute has no stale attack telegraph.");

            // Normal attack cancellation is verified with real mouse and Space input.
            PlacePlayer(new Vector3(-4,1,1.6f));brute.Controller.enabled=false;
            player.transform.rotation=Quaternion.LookRotation(brute.transform.position-player.transform.position);
            var attack=player.GetComponent<PlayerMeleeAttack>();int attacks=0;attack.Attacked+=()=>attacks++;
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,position=new Vector2(800,450)});
            yield return new WaitForSecondsRealtime(.04f);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(800,450)});
            yield return Press(Key.Space);
            attack.ApplyAttackDamage();yield return new WaitForSecondsRealtime(.28f);
            Check(attacks>=1 && hp.CurrentHealth==150,$"Dodge cancels an uncommitted player attack, including late animation events (attacks={attacks}, enemy HP={hp.CurrentHealth}).");
            brute.gameObject.SetActive(false);

            // Exercise the authored encounter sequence through actual pooled spawns.
            SceneFlow.LoadGame();yield return null;
            while(GamePresentation.BlocksGameplay)yield return null;
            player=FindFirstObjectByType<PlayerMovement>();playerHealth=player.GetComponent<Health>();playerHealth.SetInvulnerable(true);
            spawner=FindFirstObjectByType<EnemyWaveSpawner>();
            var counts=new Dictionary<int,HashSet<int>>();var heavyCounts=new Dictionary<int,HashSet<int>>();
            float deadline=Time.realtimeSinceStartup+35;
            while(!MatchResult.HasEnded && Time.realtimeSinceStartup<deadline)
            {
                AbyssHunter.World.ExpeditionTestDriver.Advance(player);
                foreach(var enemy in FindObjectsByType<PooledEnemy>(FindObjectsSortMode.None))
                {
                    int wave=spawner.CurrentWave;
                    if(!counts.ContainsKey(wave)){counts[wave]=new HashSet<int>();heavyCounts[wave]=new HashSet<int>();}
                    counts[wave].Add(enemy.GetInstanceID());if(enemy.Controller.IsHeavy)heavyCounts[wave].Add(enemy.GetInstanceID());
                    enemy.GetComponent<Health>().TakeDamage(999);
                }
                yield return new WaitForSecondsRealtime(.1f);
            }
            Check(counts.ContainsKey(1)&&counts[1].Count==2&&heavyCounts[1].Count==0,"Encounter 1 introduces two ordinary enemies.");
            Check(counts.ContainsKey(2)&&counts[2].Count==1&&heavyCounts[2].Count==1,"Encounter 2 isolates one heavy enemy.");
            Check(counts.ContainsKey(3)&&counts[3].Count==3&&heavyCounts[3].Count==1,"Encounter 3 mixes one heavy and two ordinary enemies.");
            yield return new WaitForSecondsRealtime(1);
            Check(GamePresentation.Current.State==GamePresentation.ScreenState.Result && GamePresentation.Current.DefeatedCount==6,
                "Mixed encounters complete with six correct defeat counts and victory.");
        }
        private void OnDestroy()=>Application.logMessageReceived-=OnLog;
    }
}
#endif

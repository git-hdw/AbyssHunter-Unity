#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.World;
using AbyssHunter.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssHunter.UI
{
    // Editor-only synthetic identity. Exercises local UI/progression, never claims PlayFab success.
    public sealed class AccountFlowVerification : MonoBehaviour
    {
        private readonly List<string> report=new();
        private string directory;
        private GamePresentation UI=>GamePresentation.Current;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if(!SessionState.GetBool("AbyssHunter.AccountFlowTest",false))return;
            SessionState.EraseBool("AbyssHunter.AccountFlowTest");
            DontDestroyOnLoad(new GameObject("Account flow fixture").AddComponent<AccountFlowVerification>());
        }
        private void Check(bool pass,string text){report.Add((pass?"PASS | ":"FAIL | ")+text);File.WriteAllLines("Logs/account-flow-verification.txt",report);}
        private Button Button(string name)=>UI.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name && b.IsActive());
        private void Click(string name){var b=Button(name);if(b==null)throw new Exception("Missing button "+name);b.onClick.Invoke();}
        private void SetAccount(string id)
        {
            var auth=PlayFabAuthentication.Instance;
            typeof(PlayFabAuthentication).GetProperty("PlayerId").SetValue(auth,id);
            typeof(PlayFabAuthentication).GetProperty("TitleId").SetValue(auth,"LOCAL-FIXTURE");
            typeof(PlayFabAuthentication).GetProperty("Progress").SetValue(auth,new AccountProgress(directory,"LOCAL-FIXTURE",id));
        }
        private IEnumerator Until(Func<bool> ready,float seconds=20)
        {float end=Time.realtimeSinceStartup+seconds;while(!ready()){if(Time.realtimeSinceStartup>end)throw new TimeoutException();yield return null;}}
        private IEnumerator Start()
        {
            directory=Path.GetFullPath("Logs/account-flow-"+Guid.NewGuid().ToString("N"));
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool moved;object value=null;var top=stack.Peek();
                try{moved=top.MoveNext();if(moved)value=top.Current;}catch(Exception e){Check(false,e.ToString());break;}
                if(!moved){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
            }
            report.Add("DONE | Synthetic editor identity only; real authentication and email not tested.");File.WriteAllLines("Logs/account-flow-verification.txt",report);
            EditorApplication.isPlaying=false;
        }
        private void Inspect(string screen)
        {
            Canvas.ForceUpdateCanvases();
            var labels=UI.GetComponentsInChildren<Text>().Where(t=>!string.IsNullOrEmpty(t.text)&&t.isActiveAndEnabled).ToArray();
            var clipped=labels.Where(t=>t.preferredHeight>t.rectTransform.rect.height+1).Select(t=>t.name).ToArray();
            Check(clipped.Length==0,screen+" labels fit: "+string.Join(",",clipped));
            ScreenCapture.CaptureScreenshot("Logs/"+screen+".png");
            if(screen.StartsWith("030-depths") || screen=="030-second-bridge")
                File.AppendAllText("Logs/rift-render-samples.txt",DateTime.Now.ToString("s")+" | "+screen+" | batches="+UnityEditor.UnityStats.batches+" | triangles="+UnityEditor.UnityStats.triangles+Environment.NewLine);
        }
        private IEnumerator WalkTo(PlayerMovement player,Vector3 destination)
        {
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            try
            {
                UnityEngine.AI.NavMesh.SamplePosition(destination,out var end,4,UnityEngine.AI.NavMesh.AllAreas);
                var path=new UnityEngine.AI.NavMeshPath();
                if(!UnityEngine.AI.NavMesh.CalculatePath(player.transform.position,end.position,UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new Exception("No walking path");
                foreach(var corner in path.corners.Skip(1))
                {
                    float deadline=Time.realtimeSinceStartup+30, horizontal=0, vertical=0;
                    while(true)
                    {
                        Vector3 d=corner-player.transform.position;d.y=0;if(d.magnitude<.2f)break;
                        if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("WASD route blocked at "+player.transform.position+" toward "+corner);
                        var keys=new List<UnityEngine.InputSystem.Key>();
                        // Duty-cycle the axes to follow the path segment instead of cutting every turn at 45 degrees.
                        var direction=d.normalized;horizontal+=Mathf.Abs(direction.x);vertical+=Mathf.Abs(direction.z);
                        if(horizontal>=1){keys.Add(d.x>0?UnityEngine.InputSystem.Key.D:UnityEngine.InputSystem.Key.A);horizontal-=1;}
                        if(vertical>=1){keys.Add(d.z>0?UnityEngine.InputSystem.Key.W:UnityEngine.InputSystem.Key.S);vertical-=1;}
                        keyboard.MakeCurrent();UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys.ToArray()));
                        foreach(var enemy in FindObjectsByType<AbyssHunter.Enemy.PooledEnemy>(FindObjectsSortMode.None))enemy.GetComponent<Health>().TakeDamage(1000);
                        yield return null;
                    }
                }
                Check(Vector3.Distance(player.transform.position,end.position)<1.5f,"Actual WASD reaches depths waypoint "+destination);
            }
            finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);}
        }
        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            Check(UI.State==GamePresentation.ScreenState.Login && GamePresentation.BlocksGameplay,"Unauthenticated boot blocks gameplay");Inspect("030-login");yield return new WaitForSecondsRealtime(.5f);
            Click("AccountBack");Inspect("030-register");yield return new WaitForSecondsRealtime(.5f);
            UI.GetComponentsInChildren<InputField>().First(f=>f.name=="Password").text="example-password";
            UI.GetComponentsInChildren<InputField>().First(f=>f.name=="Confirmation").text="different";
            Click("AccountSubmit");
            Check(!PlayFabAuthentication.Instance.IsBusy && UI.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("不一致")),"Mismatching confirmation stays local");
            Click("AccountBack");Click("ForgotPassword");Inspect("030-recovery");yield return new WaitForSecondsRealtime(.5f);
            Check(UI.GetComponentsInChildren<InputField>().Length==1,"Recovery only asks for email");
            SetAccount("A");SceneFlow.LoadMenu();yield return null;yield return new WaitForSecondsRealtime(1);
            Click("BeginRun");Check(UI.State==GamePresentation.ScreenState.LevelSelect && !Button("Level abyss-depths").interactable,"New account has second level locked");Inspect("030-selection-locked");yield return new WaitForSecondsRealtime(.5f);
            Click("Level sunken-ruins");yield return Until(()=>UI.State==GamePresentation.ScreenState.Playing);
            var player=FindFirstObjectByType<PlayerMovement>();player.GetComponent<Health>().TakeDamage(1000);
            yield return Until(()=>UI.State==GamePresentation.ScreenState.Result);
            Check(!PlayFabAuthentication.Instance.Progress.Completed("sunken-ruins"),"Death does not unlock next level");
            UI.Restart();yield return Until(()=>UI.State==GamePresentation.ScreenState.Playing);
            player=FindFirstObjectByType<PlayerMovement>();player.GetComponent<Health>().SetInvulnerable(true);
            float deadline=Time.realtimeSinceStartup+90;
            while(UI.State!=GamePresentation.ScreenState.Result && Time.realtimeSinceStartup<deadline)
            {ExpeditionTestDriver.Advance(player);foreach(var enemy in FindObjectsByType<AbyssHunter.Enemy.PooledEnemy>(FindObjectsSortMode.None))enemy.GetComponent<Health>().TakeDamage(1000);yield return new WaitForSecondsRealtime(.1f);}
            Check(UI.State==GamePresentation.ScreenState.Result && PlayFabAuthentication.Instance.Progress.Completed("sunken-ruins"),"Successful extraction saves completion");
            Check(Button("NextLevel")!=null,"Victory exposes next level");Click("NextLevel");yield return Until(()=>UI.State==GamePresentation.ScreenState.Playing);
            Check(LevelCatalog.Current.Id=="abyss-depths","Next level loads depths");Inspect("030-depths-entry");yield return new WaitForSecondsRealtime(.5f);
            player=FindFirstObjectByType<PlayerMovement>();player.GetComponent<Health>().SetInvulnerable(true);
            Check(FindObjectsByType<Renderer>(FindObjectsSortMode.None).All(r=>r.sharedMaterials.All(m=>m!=null && m.shader.name!="Hidden/InternalErrorShader")),"Floating rift materials and shaders are valid");
            Check(player.GetComponentInChildren<LastLight>().Strength>.7f,"Floating rift hunter lantern is lit");
            var floating=GameObject.Find("祭坛环绕晶体").transform;var origin=floating.position;
            yield return new WaitForSecondsRealtime(.3f);
            Check(Vector3.Distance(origin,floating.position)>.001f,"New altar crystals float independently");
            UI.ShowPause();var paused=floating.position;yield return new WaitForSecondsRealtime(.2f);
            Check(Vector3.Distance(paused,floating.position)<.001f,"Pause freezes new altar crystal motion");UI.Resume();
            yield return WalkTo(player,new Vector3(-38,1.5f,30));Inspect("030-depths-side");yield return new WaitForSecondsRealtime(.5f);
            var depthsWaves=FindFirstObjectByType<AbyssHunter.Enemy.EnemyWaveSpawner>();
            while(depthsWaves.CompletedEncounters<depthsWaves.TotalWaves)
            {
                int encounter=depthsWaves.CompletedEncounters+1;
                yield return WalkTo(player,depthsWaves.NextApproach.position);
                float end=Time.realtimeSinceStartup+30;
                while(depthsWaves.CompletedEncounters<encounter)
                {
                    if(Time.realtimeSinceStartup>end)throw new TimeoutException("Depths encounter "+encounter);
                    foreach(var enemy in FindObjectsByType<AbyssHunter.Enemy.PooledEnemy>(FindObjectsSortMode.None))enemy.GetComponent<Health>().TakeDamage(1000);
                    yield return null;
                }
                if(encounter==2)
                {
                    foreach(var waypoint in new[]{new Vector3(-16,1.5f,39),new Vector3(-16,1.5f,44),new Vector3(8,3,44),new Vector3(8,3,48)})
                        yield return WalkTo(player,waypoint);
                    yield return WalkTo(player,new Vector3(8,3,44));
                    yield return ProbeBridgeRailing(player);
                    Inspect("030-second-bridge");yield return new WaitForSecondsRealtime(.5f);
                }
            }
            yield return WalkTo(player,FindFirstObjectByType<AbyssExpedition>().Core.position);Inspect("030-depths-altar");yield return new WaitForSecondsRealtime(.5f);
            var mission=FindFirstObjectByType<AbyssExpedition>();
            Check(mission.TryInteract() && mission.HasCore,"New floating altar core can be recovered");
            yield return new WaitForSeconds(4);
            Check(mission.GetComponent<AbyssAtmosphere>().RouteAwakened,"New fourteen-marker return route awakens");
            Check(FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(a=>a.name=="桥下风声" && a.isPlaying)==2,"Both floating bridges play positioned wind sources");
            yield return WalkTo(player,FindFirstObjectByType<AbyssExpedition>().Beacon.position);
            Check(depthsWaves.CompletedEncounters==3 && !PlayFabAuthentication.Instance.Progress.Completed("abyss-depths"),"Clearing all encounters without extraction does not complete level");
            UI.ShowPause();Check(Time.timeScale==0,"Depths pauses");UI.Restart();yield return Until(()=>UI.State==GamePresentation.ScreenState.Playing);
            Check(LevelCatalog.Current.Id=="abyss-depths","Depths restarts itself");
            UI.ReturnToMenu();yield return Until(()=>UI.State==GamePresentation.ScreenState.Menu);Click("SignOut");yield return null;
            Check(!PlayFabAuthentication.Instance.IsSignedIn && UI.State==GamePresentation.ScreenState.Login,"Sign out clears session");
            SetAccount("B");SceneFlow.LoadMenu();yield return null;Click("BeginRun");
            Check(!Button("Level abyss-depths").interactable,"Other account cannot use first account unlock");
            SetAccount("A");Check(PlayFabAuthentication.Instance.Progress.Completed("sunken-ruins"),"Original account restores its saved completion");
        }
        private IEnumerator ProbeBridgeRailing(PlayerMovement player)
        {
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.D));
                yield return null;
                float end=Time.realtimeSinceStartup+.7f;
                while(Time.realtimeSinceStartup<end)
                {
                    keyboard.MakeCurrent();
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.Space));
                    yield return null;
                }
                var p=player.transform.position;
                Check(p.x>8.3f && p.x<12.2f && p.y>3.5f && Mathf.Abs(p.z-44)<1,"Upper bridge outer railing stops held movement and dodge without a fall: "+p);
            }
            finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);}
        }
    }
}
#endif

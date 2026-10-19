#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using AbyssHunter.Inventory;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace AbyssHunter.UI
{
    // Editor-only input replay. Never alters health, damage, enemy AI or game progression.
    // Capture output comes from the running game, including its mixed audio.
    public sealed class PortfolioCapture : MonoBehaviour
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private bool recording, finished;
        private string folder;
        private int frame, phase;
        private float clock, phaseStart, nextAttack, nextDodge;
        private PlayerMovement player;
        private Health health;
        private readonly List<string> report=new();
        private BinaryWriter audioWriter;
        private int samplesWritten;
        private GamePresentation.ScreenState lastState;
        private float stateSince;
        private int healingSlot = -1;
        private float healingSince;
        private float healCooldown;
        private bool healingClicked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string mode=SessionState.GetString("AbyssHunter.Capture","");
            if(string.IsNullOrEmpty(mode)) return;
            SessionState.EraseString("AbyssHunter.Capture");
            var c=new GameObject("Portfolio input replay").AddComponent<PortfolioCapture>();
            c.recording=mode=="record"; c.Initialize(); DontDestroyOnLoad(c.gameObject);
        }
        private void Initialize()
        {
            folder=Path.GetFullPath(recording?"Recordings/Presentation/raw":"Logs/presentation-review");
            if(recording && Directory.Exists(folder))
                Directory.Move(folder,folder+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(folder);
            UnityEngine.Random.InitState(13);
            keyboard=InputSystem.AddDevice<Keyboard>("PortfolioKeyboard");
            mouse=InputSystem.AddDevice<Mouse>("PortfolioMouse");
            Application.runInBackground=true;
            Application.logMessageReceived+=OnLog;
            Time.captureFramerate=recording?30:0;
            if(recording)
            {
                audioWriter=new BinaryWriter(File.Create(Path.Combine(folder,"audio.wav")));
                audioWriter.Write(new byte[44]);
                AudioRenderer.Start();
                StartCoroutine(CaptureFrames());
            }
            Log("START: Actual gameplay driven by virtual keyboard/mouse; no combat overrides.");
        }
        private void OnLog(string message,string stack,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) Log("ERROR: "+message+"\n"+stack);
        }
        private void Log(string value)
        {
            report.Add($"{clock:F2}s | {value}");
            File.WriteAllLines(Path.Combine(folder,"report.txt"),report);
        }
        private void SendKeys(params Key[] keys) { keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
        private void Snapshot(string name) { if(!recording) ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png")); Log("SCREEN: "+name); }

        private void Update()
        {
            if(finished) return;
            clock+=recording?1f/30:Time.unscaledDeltaTime;
            var ui=GamePresentation.Current;
            if(ui==null) return;
            if(ui.State!=lastState) {lastState=ui.State;stateSince=clock;Log("STATE: "+lastState);}
            SendKeys();
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.45f)});
            if(phase==0 && clock>3) {Snapshot("01-menu");phase=1;phaseStart=clock;}
            else if(phase==1 && clock-phaseStart>1) {SendKeys(Key.Enter);phase=2;}
            else if(phase==2 && ui.State==GamePresentation.ScreenState.Intro) {Snapshot("02-opening");phase=3;}
            else if(phase==3 && ui.State==GamePresentation.ScreenState.Playing)
            {
                player=FindFirstObjectByType<PlayerMovement>();health=player.GetComponent<Health>();
                phase=4;phaseStart=clock;
            }
            else if(phase==4 && clock-phaseStart>1.5f) {Snapshot("03-hud");SendKeys(Key.Escape);phase=5;phaseStart=clock;}
            else if(phase==5 && clock-phaseStart>1) {Snapshot("04-pause");SendKeys(Key.Escape);phase=6;phaseStart=clock;}
            else if(phase==6 && clock-phaseStart>.5f) {SendKeys(Key.I);phase=7;phaseStart=clock;}
            else if(phase==7 && clock-phaseStart>1) {Snapshot("05-inventory");SendKeys(Key.I);phase=8;}
            else if(phase==8 && ui.State==GamePresentation.ScreenState.Playing) Play();
            else if(phase==8 && ui.State==GamePresentation.ScreenState.Inventory && healingSlot>=0)
            {
                Vector2 pos=new((470+(healingSlot%4)*220)*Screen.width/1600f,
                    Screen.height-(413+(healingSlot/4)*73)*Screen.height/900f);
                ushort buttons=0;
                if(clock-healingSince>.8f && !healingClicked)
                {
                    healingClicked=true;
                    buttons=1;
                }
                InputSystem.QueueStateEvent(mouse,new MouseState{buttons=buttons,position=pos});
                if(clock-healingSince>1.6f) {SendKeys(Key.I);healingSlot=-1;healCooldown=clock+2;}
            }
            if(ui.State==GamePresentation.ScreenState.Result && phase<9)
            {
                phase=9; phaseStart=clock; Log($"RESULT: HP {health.CurrentHealth}, defeated {ui.DefeatedCount}, time {ui.RunSeconds:F2}");
                Snapshot("06-result");
            }
            if(phase==9 && clock-phaseStart>5) {Snapshot("07-result-settled");Finish();}
            if(clock>240) {Log("TIMEOUT");Finish();}
        }
        private void Play()
        {
            if(healingSlot>=0) return;
            var inventory=player.GetComponent<PlayerInventory>();
            if(health.CurrentHealth<=70 && clock>healCooldown)
                for(int i=0;i<inventory.Slots.Count;i++)
                    if(!inventory.Slots[i].IsEmpty && inventory.Slots[i].Item is HealingItemData)
                    {healingSlot=i;healingSince=clock;healingClicked=false;SendKeys(Key.I);return;}
            EnemyController nearest=null;
            float distance=float.MaxValue;
            foreach(var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            {
                if(!e.enabled || e.GetComponent<Health>().IsDead) continue;
                float d=Vector3.Distance(player.transform.position,e.transform.position);
                if(d<distance) {distance=d;nearest=e;}
            }
            var keys=new List<Key>();
            Vector3 desired=Vector3.zero;
            if(nearest!=null)
            {
                Vector3 toward=nearest.transform.position-player.transform.position;toward.y=0;
                if(distance>1.55f) desired=toward.normalized;
                if(nearest.IsHeavy && nearest.CurrentState==EnemyState.Windup && nearest.PhaseElapsed>.32f && distance<3.7f)
                {
                    desired=Vector3.Cross(Vector3.up,toward.normalized);
                    if(Time.time>nextDodge){keys.Add(Key.Space);nextDodge=Time.time+.85f;}
                }
                if(health.CurrentHealth<40 && nearest.CurrentState==EnemyState.Attack && distance<2.5f && Time.time>nextDodge)
                {
                    desired=-toward.normalized;
                    keys.Add(Key.Space); nextDodge=Time.time+.85f;
                }
                if(distance<3 && Time.time>nextAttack)
                {
                    InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,position=new Vector2(Screen.width*.5f,Screen.height*.45f)});
                    nextAttack=Time.time+.51f;
                }
            }
            else
            {
                var drops=FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
                float best=float.MaxValue;
                foreach(var drop in drops)
                {
                    float d=Vector3.Distance(drop.transform.position,player.transform.position);
                    if(d<best) {best=d;desired=(drop.transform.position-player.transform.position).normalized;}
                }
                var mission=FindFirstObjectByType<AbyssHunter.World.AbyssExpedition>();
                if(mission!=null&&best>6)
                {
                    var target=mission.Destination;
                    if(target!=null)
                    {
                        Vector3 goal=target.position-player.transform.position;goal.y=0;
                        // Bridges run down the center; align before crossing their narrower width.
                        float z=player.transform.position.z;
                        if((z>5&&z<19||z>29&&z<43)&&Mathf.Abs(player.transform.position.x)>1.4f)
                            desired=new Vector3(-Mathf.Sign(player.transform.position.x),0,0);
                        else desired=goal.normalized;
                        if(goal.magnitude<2.3f&&mission.HuntComplete){desired=Vector3.zero;if(clock%1f<.08f)keys.Add(Key.E);}
                    }
                }
            }
            var cam=Camera.main.transform;
            Vector3 forward=cam.forward;forward.y=0;forward.Normalize();
            float x=Vector3.Dot(desired,cam.right),y=Vector3.Dot(desired,forward);
            if(x>.3f) keys.Add(Key.D);else if(x<-.3f) keys.Add(Key.A);
            if(y>.3f) keys.Add(Key.W);else if(y<-.3f) keys.Add(Key.S);
            SendKeys(keys.ToArray());
        }
        private IEnumerator CaptureFrames()
        {
            while(!finished)
            {
                yield return new WaitForEndOfFrame();
                if(finished) break;
                var texture=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder,$"frame-{frame:000000}.jpg"),texture.EncodeToJPG(92));
                Destroy(texture); frame++;
                int count=AudioRenderer.GetSampleCountForCaptureFrame();
                if(count>0)
                {
                    using var buffer=new NativeArray<float>(count*2,Allocator.Temp);
                    AudioRenderer.Render(buffer);
                    for(int i=0;i<buffer.Length;i++) audioWriter.Write((short)(Mathf.Clamp(buffer[i],-1,1)*32767));
                    samplesWritten+=buffer.Length;
                }
                if(frame%300==0) Log("CAPTURE: "+frame+" frames");
            }
        }
        private void Finish()
        {
            if(finished) return;
            finished=true;Log("DONE: "+frame+" frames");
            if(recording)
            {
                AudioRenderer.Stop();
                audioWriter.Seek(0,SeekOrigin.Begin);
                audioWriter.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));audioWriter.Write(36+samplesWritten*2);
                audioWriter.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));audioWriter.Write(16);audioWriter.Write((short)1);audioWriter.Write((short)2);
                audioWriter.Write(AudioSettings.outputSampleRate);audioWriter.Write(AudioSettings.outputSampleRate*4);audioWriter.Write((short)4);audioWriter.Write((short)16);
                audioWriter.Write(System.Text.Encoding.ASCII.GetBytes("data"));audioWriter.Write(samplesWritten*2);audioWriter.Close();audioWriter=null;
            }
            Time.captureFramerate=0;
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            EditorApplication.isPlaying=false;
        }
        private void OnDestroy()
        {
            Application.logMessageReceived-=OnLog;
            if(!finished) {Time.captureFramerate=0;if(recording)AudioRenderer.Stop();audioWriter?.Dispose();}
        }
    }
}
#endif




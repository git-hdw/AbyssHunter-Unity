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
using AbyssHunter.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public sealed class PresentationVerification : MonoBehaviour
{
    private Keyboard keyboard;
    private Mouse mouse;
    private readonly List<string> checks=new();
    private int errors;
    private GamePresentation UI => GamePresentation.Current;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if(!File.Exists("Logs/run-presentation-verification.flag")) return;
        File.Delete("Logs/run-presentation-verification.flag");
        var obj=new GameObject("Presentation verification").AddComponent<PresentationVerification>();
        DontDestroyOnLoad(obj);
    }
    private void Awake()
    {
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
        Application.runInBackground=true;
        Application.logMessageReceived+=OnLog;
    }
    private void OnLog(string text,string stack,LogType type)
    {if(type==LogType.Error || type==LogType.Exception) {errors++; Check(false,text+"\n"+stack);}}
    private void Check(bool passed,string message)
    {checks.Add((passed?"PASS | ":"FAIL | ")+message);File.WriteAllLines("Logs/presentation-verification.txt",checks);}
    private IEnumerator Press(Key key)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.08f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.08f);
    }
    private Button FindButton(string name) => FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name && b.IsActive() && b.GetComponentInParent<Canvas>().enabled);
    private IEnumerator Click(string name)
    {
        mouse.MakeCurrent();
        var button=FindButton(name);Check(button!=null,"Button exists: "+name);if(button==null)yield break;
        Vector2 position=RectTransformUtility.WorldToScreenPoint(null,button.transform.TransformPoint(((RectTransform)button.transform).rect.center));
        InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return null;yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=position,buttons=1});yield return null;yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return new WaitForSecondsRealtime(.2f);
    }
    private IEnumerator Start()
    {
        Directory.CreateDirectory("Logs/presentation-review");
        yield return new WaitForSecondsRealtime(.5f);
        Check(UI!=null && UI.State==GamePresentation.ScreenState.Menu,"Menu bootstraps with one presentation owner.");
        InspectChinese("01-menu");
        yield return Click("Controls");
        Check(FindButton("Close")!=null,"Instructions modal opens via pointer click.");
        InspectChinese("02-controls");
        yield return Press(Key.Escape);
        Check(FindButton("Close")==null,"Escape closes instructions.");
        yield return Click("BeginRun");yield return new WaitForSecondsRealtime(.5f);
        Check(UI.State==GamePresentation.ScreenState.Intro && Time.timeScale==0 && GamePresentation.BlocksGameplay,"Opening pauses world and blocks player input.");
        InspectChinese("03-opening");
        var player=FindFirstObjectByType<PlayerMovement>();var health=player.GetComponent<Health>();
        Check(Camera.main.transform.position.y>player.transform.position.y+5,"Opening camera snaps to its gameplay framing.");
        yield return new WaitForSecondsRealtime(3.1f);
        Check(UI.State==GamePresentation.ScreenState.Playing && !GamePresentation.BlocksGameplay,"Countdown enters gameplay.");
        InspectChinese("04-hud");
        int attacks=0;player.GetComponent<PlayerMeleeAttack>().Attacked+=()=>attacks++;
        var position=player.transform.position;yield return Press(Key.D);
        Check(Vector3.Distance(position,player.transform.position)>.1f,"Gameplay movement still works.");
        yield return Press(Key.Escape);
        Check(UI.State==GamePresentation.ScreenState.Paused && Time.timeScale==0,"Escape opens pause and stops time.");
        InspectChinese("05-pause");
        position=player.transform.position;int before=attacks;float time=UI.RunSeconds;
        yield return Press(Key.W);yield return Press(Key.Space);
        InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,position=new Vector2(60,200)});yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(60,200)});yield return new WaitForSecondsRealtime(.2f);
        Check(Vector3.Distance(position,player.transform.position)<.001f && attacks==before && UI.RunSeconds==time,"Pause prevents movement, dodge, attack and run timer advancement.");
        yield return Click("Resume");
        Check(UI.State==GamePresentation.ScreenState.Playing && Time.timeScale>0,"Resume button restores gameplay.");
        yield return Press(Key.I);
        Check(UI.State==GamePresentation.ScreenState.Inventory && Time.timeScale==0,"Inventory pauses world.");
        // Explicit test fixture only; never part of the public demo capture.
        health.TakeDamage(30);
        var inventory=player.GetComponent<PlayerInventory>();
        var potion=AssetDatabase.LoadAssetAtPath<HealingItemData>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:HealingItemData")[0]));
        inventory.TryAdd(potion,1,out _);UI.ShowInventory();
        InspectChinese("06-inventory");
        yield return Click("Slot 0");
        Check(health.CurrentHealth==100 && inventory.GetTotalQuantity(potion)==0,"Potion button consumes one potion and restores health.");
        yield return Press(Key.I);
        Check(UI.State==GamePresentation.ScreenState.Playing,"I closes inventory and resumes.");
        health.SetInvulnerable(true);
        float deadline=Time.realtimeSinceStartup+40;
        while(UI.State!=GamePresentation.ScreenState.Result && Time.realtimeSinceStartup<deadline)
        {
            AbyssHunter.World.ExpeditionTestDriver.Advance(player);
            foreach(var enemy in FindObjectsByType<PooledEnemy>(FindObjectsSortMode.None)) enemy.GetComponent<Health>().TakeDamage(1000);
            yield return new WaitForSecondsRealtime(.1f);
        }
        Check(UI.State==GamePresentation.ScreenState.Result && UI.DefeatedCount==6 && MatchResult.HasEnded && Time.timeScale==0,"Controlled wave fixture reaches victory, counts 6 defeats and freezes result.");
        yield return new WaitForSecondsRealtime(.5f);InspectChinese("07-victory");
        yield return Click("Retry");yield return new WaitForSecondsRealtime(.5f);
        Check(UI.State==GamePresentation.ScreenState.Intro && UI.DefeatedCount==0 && !MatchResult.HasEnded,"Retry button creates a fresh run with reset statistics.");
        yield return new WaitForSecondsRealtime(3.1f);
        health=FindFirstObjectByType<PlayerMovement>().GetComponent<Health>();health.TakeDamage(1000);
        yield return new WaitForSecondsRealtime(1);
        Check(UI.State==GamePresentation.ScreenState.Result && health.IsDead && Time.timeScale==0,"Controlled lethal damage reaches defeat result.");
        InspectChinese("08-defeat");
        yield return Press(Key.F9);
        Check(health.IsDead && MatchResult.HasEnded,"Checkpoint input cannot revive an ended match.");
        yield return Click("Menu");yield return new WaitForSecondsRealtime(.5f);
        Check(UI.State==GamePresentation.ScreenState.Menu && Time.timeScale==1,"Result menu button restores main menu and normal time.");
        Check(errors==0,"No runtime errors or exceptions.");
        checks.Add("DONE | Presentation interaction verification complete.");File.WriteAllLines("Logs/presentation-verification.txt",checks);
        InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
        EditorApplication.isPlaying=false;
    }
    private void OnDestroy()=>Application.logMessageReceived-=OnLog;

    private void InspectChinese(string screen)
    {
        Canvas.ForceUpdateCanvases();
        var labels=UI.GetComponentsInChildren<Text>().Where(t=>t.isActiveAndEnabled).ToArray();
        var missing=labels.SelectMany(t=>t.text.Where(c=>c>='\u4e00'&&c<='\u9fff'&&!t.font.HasCharacter(c))).Distinct().ToArray();
        Check(labels.All(t=>t.font==GameFont.Chinese)&&missing.Length==0,screen+": bundled Chinese font covers all visible characters: "+new string(missing));
        var english=labels.Where(t=>System.Text.RegularExpressions.Regex.IsMatch(t.text.Replace("WASD","").Replace("Esc",""),"[a-zA-Z]{2,}")).Select(t=>t.text).ToArray();
        Check(english.Length==0,screen+": no untranslated English copy: "+string.Join(" / ",english));
        var clipped=labels.Where(t=>!string.IsNullOrEmpty(t.text)&&t.preferredHeight>t.rectTransform.rect.height+1)
            .Select(t=>$"{t.name} ({t.preferredHeight:F1}/{t.rectTransform.rect.height:F1})").ToArray();
        Check(clipped.Length==0,screen+": translated text fits its layout: "+string.Join(" / ",clipped));
        ScreenCapture.CaptureScreenshot("Logs/presentation-review/chinese-"+screen+".png");
    }
}
#endif

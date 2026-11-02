using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssHunter.Save;
using AbyssHunter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AbyssHunter.EditorTools
{
    [InitializeOnLoad]
    public static class Release030Verification
    {
        private const string SuiteKey = "AbyssHunter.030Suite";
        static Release030Verification()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.EraseBool("AbyssHunter.LocalVerification");
                    if (SessionState.GetBool(SuiteKey,false)) EditorApplication.delayCall += Next;
                }
            };
        }
        [MenuItem("Tools/深渊猎手/验证/运行 v0.3.0 回归 %&v")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Logs");
            Profiles();
            typeof(PortfolioTools).GetMethod("SetGameResolution",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{1600,900});
            SessionState.SetBool(SuiteKey,true); SessionState.SetInt(SuiteKey+".step",0); Next();
        }
        [MenuItem("Tools/深渊猎手/验证/账号与第二关操作回归 %&k")]
        public static void AccountsOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(SuiteKey,true);SessionState.SetInt(SuiteKey+".step",4);
            typeof(PortfolioTools).GetMethod("SetGameResolution",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{1600,900});
            Next();
        }
        private static void Next()
        {
            int step=SessionState.GetInt(SuiteKey+".step",0); SessionState.SetInt(SuiteKey+".step",step+1);
            if(step>=5)
            {
                SessionState.EraseBool(SuiteKey);
                bool failed=new[]{"profiles-030","heavy-combat","expedition","presentation","atmosphere","account-flow"}.Any(n=>!File.Exists("Logs/"+n+"-verification.txt") || File.ReadAllText("Logs/"+n+"-verification.txt").Contains("FAIL |"));
                File.WriteAllText("Logs/v030-suite.txt",failed?"DONE | FAILED":"DONE | PASSED");
                if(Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-030-exit"))EditorApplication.Exit(failed?1:0);
                return;
            }
            SessionState.SetBool("AbyssHunter.LocalVerification",step!=4);
            string[] flags={"AbyssHunter.HeavyTest","AbyssHunter.ExpeditionTest","", "AbyssHunter.ArtReview", "AbyssHunter.AccountFlowTest"};
            EditorSceneManager.OpenScene((step==2 || step==4)?"Assets/Scenes/MainMenu.unity":"Assets/Scenes/SampleScene.unity");
            if(step==2)File.WriteAllText("Logs/run-presentation-verification.flag","1");else SessionState.SetBool(flags[step],true);
            EditorApplication.isPlaying=true;
        }
        public static void Profiles()
        {
            string directory=Path.GetFullPath("Logs/profile-fixtures-"+Guid.NewGuid().ToString("N"));
            var lines=new List<string>();
            void Check(bool pass,string text)=>lines.Add((pass?"PASS | ":"FAIL | ")+text);
            var first=new AccountProgress(directory,"TITLE","PLAYER-A");var other=new AccountProgress(directory,"TITLE","PLAYER-B");
            Check(!first.Completed("sunken-ruins"),"New profile starts locked");
            Check(first.RecordCompletion("sunken-ruins",120),"Completion persists");
            Check(!other.Completed("sunken-ruins"),"Accounts are isolated");
            var reload=new AccountProgress(directory,"TITLE","PLAYER-A");
            Check(reload.Completed("sunken-ruins") && reload.BestSeconds("sunken-ruins")==120,"Restart restores completion and best time");
            reload.RecordCompletion("sunken-ruins",150);reload.RecordCompletion("sunken-ruins",100);
            Check(reload.BestSeconds("sunken-ruins")==100,"Only faster successful runs replace best time");
            string path=Directory.GetFiles(directory,"*.json").Single(); File.WriteAllText(path,"broken");
            var recovered=new AccountProgress(directory,"TITLE","PLAYER-A");
            Check(recovered.Completed("sunken-ruins") && Directory.GetFiles(directory,"*.damaged-*").Length==1,"Corruption preserves original and reads backup");
            string blocked=Path.Combine(directory,"file-not-directory");File.WriteAllText(blocked,"fixture");
            var unwritable=new AccountProgress(blocked,"TITLE","PLAYER-A");
            Check(!unwritable.RecordCompletion("sunken-ruins",1) && !unwritable.Completed("sunken-ruins"),"Failed write does not unlock or claim success");
            Check(!reload.RecordCompletion("sunken-ruins",float.NaN),"Invalid time is rejected");
            lines.Add("DONE | Local fixtures only; no network authentication exercised.");File.WriteAllLines("Logs/profiles-030-verification.txt",lines);
        }
        [MenuItem("Tools/深渊猎手/构建/构建 v0.3.0 验证与发布包 %&b")]
        public static void BuildPlayers()
        {
            PortfolioTools.BuildInventoryVerification();
            File.Copy("Logs/portfolio-build.txt","Logs/v030-development-build.txt",true);
            PortfolioTools.BuildRelease();
            File.Copy("Logs/portfolio-build.txt","Logs/v030-release-build.txt",true);
        }
    }
}

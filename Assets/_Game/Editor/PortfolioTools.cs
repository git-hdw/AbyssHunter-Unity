using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AbyssHunter.EditorTools
{
    public static class PortfolioTools
    {
        [MenuItem("Tools/Abyss Hunter/Preview presentation")]
        public static void Preview() => Start("review");
        [MenuItem("Tools/Abyss Hunter/Record input-driven demo")]
        public static void Record() => Start("record");
        private static void Start(string mode)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SetGameResolution(1600,900);
            SessionState.SetString("AbyssHunter.Capture",mode);
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            EditorApplication.isPlaying=true;
        }
        private static void SetGameResolution(int width,int height)
        {
            var assembly=typeof(EditorWindow).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizesType.BaseType.GetProperty("instance").GetValue(null);
            var group=sizesType.GetMethod("GetGroup").Invoke(instance,new object[]{0});
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(enumType,1),width,height,"Portfolio Capture"});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,count-1);
            view.Focus();
        }
        [MenuItem("Tools/Abyss Hunter/Build portfolio player")]
        public static void Build()
        {
            Directory.CreateDirectory("Builds/Portfolio");
            Directory.CreateDirectory("Logs");
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var content);
            if (!string.IsNullOrEmpty(content.Error))
            {
                File.WriteAllText("Logs/portfolio-build.txt", "Addressables failed: " + content.Error);
                Debug.LogError(content.Error);
                return;
            }
            PlayerSettings.productName="深渊猎手";
            PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"},
                locationPathName="Builds/Portfolio/AbyssHunter.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            File.WriteAllText("Logs/portfolio-build.txt",$"{report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\nTime: {report.summary.totalTime}");
            if (report.summary.result == BuildResult.Succeeded)
                File.Copy("Assets/_Game/Resources/Fonts/OFL.txt", "Builds/Portfolio/字体许可.txt", true);
        }
    }
}

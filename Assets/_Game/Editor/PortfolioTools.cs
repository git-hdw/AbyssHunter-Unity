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
        [MenuItem("Tools/深渊猎手/Preview presentation")]
        public static void Preview() => Start("review");
        [MenuItem("Tools/深渊猎手/Record input-driven demo")]
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
        [MenuItem("Tools/深渊猎手/Build portfolio player")]
        public static void Build() => BuildAt("Builds/Portfolio");

        [MenuItem("Tools/深渊猎手/构建/构建 Windows 发布版")]
        public static void BuildRelease() => BuildAt("Builds/Windows-v" + PlayerSettings.bundleVersion);

        [MenuItem("Tools/深渊猎手/验证/构建碎片回归开发包")]
        public static void BuildInventoryVerification() => BuildAt("Builds/InventoryVerification", BuildOptions.Development);

        internal static void BuildAt(string output, BuildOptions options = BuildOptions.None)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(output);
            Directory.CreateDirectory("Logs");
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var content);
            if (!string.IsNullOrEmpty(content.Error))
            {
                File.WriteAllText("Logs/portfolio-build.txt", "Addressables failed: " + content.Error);
                throw new InvalidOperationException("Addressables build failed: " + content.Error);
            }
            PlayerSettings.productName="深渊猎手";
            PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var previousBuildOption = settings.BuildAddressablesWithPlayerBuild;
            BuildReport report;
            try
            {
                // Content was built above. Reuse it instead of running a second bundle build.
                settings.BuildAddressablesWithPlayerBuild = UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"},
                    locationPathName=Path.Combine(output,"AbyssHunter.exe"),target=BuildTarget.StandaloneWindows64,options=options });
            }
            finally { settings.BuildAddressablesWithPlayerBuild = previousBuildOption; }
            File.WriteAllText("Logs/portfolio-build.txt",$"{report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\nTime: {report.summary.totalTime}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Player build did not succeed: " + report.summary.result);
            if (report.summary.result == BuildResult.Succeeded)
            {
                File.Copy("Assets/_Game/Resources/Fonts/OFL.txt", Path.Combine(output,"字体许可.txt"), true);
                var licenses = Path.Combine(output,"素材许可"); Directory.CreateDirectory(licenses);
                File.Copy("Assets/KayKit/Characters/Adventurers/License.txt",Path.Combine(licenses,"KayKit-Adventurers.txt"),true);
                File.Copy("Assets/KayKit/Characters/Skeletons/License.txt",Path.Combine(licenses,"KayKit-Skeletons.txt"),true);
                File.Copy("Docs/character-models.md",Path.Combine(licenses,"角色素材说明.md"),true);
                File.Copy("CHANGELOG.md",Path.Combine(output,"更新记录.md"),true);
            }
        }
    }
}

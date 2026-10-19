using System.IO;
using AbyssHunter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssHunter.EditorTools
{
    public static class ChinesePresentationSetup
    {
        [MenuItem("Tools/Abyss Hunter/Configure Chinese presentation")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string fontPath = "Assets/_Game/Resources/Fonts/NotoSansCJKsc-Regular.otf";
            var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(fontPath);
            if (!importer.includeFontData)
            {
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            string previous = EditorSceneManager.GetActiveScene().path;
            foreach (string path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var label in root.GetComponentsInChildren<Text>(true))
                        Localize(label, font);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var labels = root.GetComponentsInChildren<Text>(true);
                    if (labels.Length == 0) continue;
                    foreach (var label in labels) Localize(label, font);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            PlayerSettings.productName = "深渊猎手";
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/chinese-ui-setup.txt", "完成：中文字体已嵌入，场景及预制体旧版提示已同步中文。");
        }

        private static void Localize(Text label, Font font)
        {
            label.font = font;
            label.text = label.text.Replace("ABYSS HUNTER", "深渊猎手")
                .Replace("VICTORY", "狩猎成功").Replace("PAUSED", "已暂停")
                .Replace("Game Over", "狩猎失败").Replace("New Text", "准备战斗……")
                .Replace("Enter", "回车键").Replace("Esc 键", "Esc").Replace("Esc", "Esc 键");
            EditorUtility.SetDirty(label);
        }

        [MenuItem("Tools/Abyss Hunter/Verify Chinese presentation")]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Logs/presentation-review");
            File.WriteAllText("Logs/run-presentation-verification.flag", "1");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.isPlaying = true;
        }
    }
}

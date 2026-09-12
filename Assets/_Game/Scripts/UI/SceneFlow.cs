using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssHunter.UI
{
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "SampleScene";

        public static void LoadGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScene);
        }

        public static void LoadMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuScene);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

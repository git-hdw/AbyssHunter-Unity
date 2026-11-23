using AbyssHunter.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssHunter.UI
{
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "SampleScene";

        internal static HunterWeapon RunWeapon { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetWeapon() => RunWeapon = HunterWeapon.Sword;

        public static void LoadGame()
        {
            LoadLevel(LevelCatalog.All[0].Id);
        }

        public static void LoadLevel(string id, HunterWeapon weapon = HunterWeapon.Sword)
        {
            var level = LevelCatalog.Find(id);
            if (level == null) return;
            if (!CanEnter(level)) { LoadMenu(); return; }
            Time.timeScale = 1f;
            RunWeapon = weapon;
            SceneManager.LoadScene(level.Scene);
        }
        public static void RestartLevel() => LoadLevel(LevelCatalog.Current.Id, RunWeapon);
        public static bool CanEnter(LevelDefinition level)
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("AbyssHunter.LocalVerification", false)) return true;
#elif DEVELOPMENT_BUILD
            // Existing explicit, development-only bundle regression. No release bypass.
            foreach (var arg in System.Environment.GetCommandLineArgs())
                if (arg.StartsWith("--inventory-report=", System.StringComparison.Ordinal)) return true;
#endif
            return PlayFabAuthentication.Instance.IsSignedIn && level.IsUnlocked;
        }

        public static void LoadMenu()
        {
            Time.timeScale = 1f;
            ResetWeapon();
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

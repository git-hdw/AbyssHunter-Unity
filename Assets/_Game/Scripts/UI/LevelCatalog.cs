using System;
using UnityEngine.SceneManagement;

namespace AbyssHunter.UI
{
    public sealed class LevelDefinition
    {
        public readonly string Id, Scene, Title, Prerequisite, Opening;
        public readonly int Shards;
        public LevelDefinition(string id, string scene, string title, string prerequisite, int shards, string opening)
        { Id = id; Scene = scene; Title = title; Prerequisite = prerequisite; Shards = shards; Opening = opening; }
        public bool IsUnlocked => string.IsNullOrEmpty(Prerequisite) || PlayFabAuthentication.Instance.Progress?.Completed(Prerequisite) == true;
    }
    public static class LevelCatalog
    {
        public static readonly LevelDefinition[] All = {
            new("sunken-ruins", "SampleScene", "沉没遗迹追猎", null, 4, "猎杀侵蚀守卫  /  夺回核心  /  点亮归途"),
            new("abyss-depths", "AbyssDepths", "裂隙回廊", "sunken-ruins", 6, "穿越悬浮群岛  /  夺回中央核心  /  六枚碎片点亮归途")
        };
        public static LevelDefinition Find(string id) => Array.Find(All, l => l.Id == id);
        public static LevelDefinition ForScene(string scene) => Array.Find(All, l => l.Scene == scene);
        public static LevelDefinition Current => ForScene(SceneManager.GetActiveScene().name) ?? All[0];
        public static string[] BuildScenes => new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity", "Assets/Scenes/AbyssDepths.unity" };
    }
}

using System.Collections;
using AbyssHunter.Character;
using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using AbyssHunter.Inventory;
using AbyssHunter.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static AbyssHunter.UI.PresentationTheme;

namespace AbyssHunter.UI
{
    // Scene-local presentation owner. Gameplay systems remain the source of all HUD/stat data.
    public sealed class GamePresentation : MonoBehaviour
    {
        public enum ScreenState { Menu, Intro, Playing, Paused, Inventory, Finishing, Result, Transition }
        public static GamePresentation Current { get; private set; }
        public static bool BlocksGameplay => Current != null && Current.State != ScreenState.Playing;
        public ScreenState State { get; private set; }
        public int DefeatedCount { get; private set; }
        public float RunSeconds { get; private set; }
        private RectTransform root, hud, modal;
        private CanvasGroup bannerGroup;
        private Text hpLabel, waveLabel, enemyLabel, timerLabel, lootLabel, dodgeLabel, bannerTitle, bannerSubtitle;
        private Image hpFill, dodgeFill;
        private Health health;
        private PlayerMovement player;
        private PlayerInventory inventory;
        private EnemyWaveSpawner spawner;
        private AbyssExpedition expedition;
        private Text interactionLabel, destinationLabel;
        private float bannerUntil, shownHealth=1;
        private bool resultWon;
        private int damageTaken;
        private int resultShards, resultHealth, resultDepth, resultDefeated;
        private float uiTime;
        // Loading or resuming the editor must not skip presentation beats in one frame.
        internal static float AnimationDelta => Time.captureFramerate > 0 ? 1f / Time.captureFramerate : Mathf.Min(Time.unscaledDeltaTime, .1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Current = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneFlow.MainMenuScene && scene.name != SceneFlow.GameScene) return;
            if (FindFirstObjectByType<GamePresentation>() != null) return;
            new GameObject("Game Presentation").AddComponent<GamePresentation>().Initialize(scene.name);
        }

        private void Initialize(string scene)
        {
            Current = this;
            gameObject.AddComponent<PresentationAudio>();
            Time.timeScale = 1;
            // Retire legacy screen visuals while preserving world-space health bars.
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.renderMode != RenderMode.WorldSpace) canvas.enabled = false;
            DisableLegacy<MainMenuController>(); DisableLegacy<PauseController>();
            DisableLegacy<GameOverController>(); DisableLegacy<VictoryController>(); DisableLegacy<InventoryUI>();
            if (EventSystem.current == null)
                new GameObject("UI Input", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var go = new GameObject("Presentation Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = (float)Screen.width / Screen.height < 16f / 9f ? 0 : 1;
            // A centered 16:9 safe area also keeps the layout intact on ultrawide screens.
            root = Rect(go.transform, "Safe Area", 0, 0, 1600, 900);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
            root.anchoredPosition = Vector2.zero;
            if (scene == SceneFlow.MainMenuScene) BuildMenu(); else BuildGame();
        }

        private static void DisableLegacy<T>() where T : Behaviour
        {
            foreach (var component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)) component.enabled = false;
        }

        private void BuildMenu()
        {
            State = ScreenState.Menu;
            var menuCamera = Camera.main;
            if (menuCamera != null)
            {
                menuCamera.clearFlags = CameraClearFlags.SolidColor;
                menuCamera.backgroundColor = Ink;
            }
            Box(root,"Backdrop",-1600,-900,4800,2700,Ink);
            for (int i=0;i<18;i++) Box(root,"Fine grid",750+i*52,0,1,900,new Color(.14f,.27f,.31f,.13f));
            Box(root,"Divider",746,96,1,704,new Color(.22f,.43f,.46f,.32f));
            var art=Rect(root,"Abyss seal",780,75,760,730).gameObject.AddComponent<AbyssSigil>(); art.raycastTarget=false;
            Label(root,"Brand","深渊猎手       独立游戏作品",86,58,700,30,16,Muted);
            Label(root,"Eyebrow","深入未知，直面深渊",90,193,650,30,18,Gold);
            var a=Label(root,"Title","深渊\n猎手",82,234,700,240,88,White); a.fontStyle=FontStyle.Bold; a.lineSpacing=.85f;
            Box(root,"Title accent",90,480,64,4,Cyan);
            Label(root,"Pitch","深入沉没遗迹，猎杀侵蚀守卫。\n夺回深渊核心，点亮归途。",90,507,570,76,25,Muted);
            Button(root,"BeginRun","开始狩猎",90,620,420,BeginRun,true);
            Button(root,"Controls","操作说明",90,690,260,ShowControls);
            Button(root,"Quit","退出游戏",364,690,146,SceneFlow.Quit);
            Label(root,"Enter hint","回车键  /  开始游戏",90,764,420,28,15,Muted);
            Label(root,"Seal caption","最后一束光",850,754,620,40,22,Gold,TextAnchor.MiddleCenter);
            Label(root,"Seal subtitle","追猎  /  夺核  /  撤离",850,801,620,28,15,Muted,TextAnchor.MiddleCenter);
            Box(root,"Footer rule",86,849,1428,1,new Color(.2f,.35f,.39f,.6f));
            Label(root,"Footer","独立制作   /   三维动作追猎",86,860,800,25,13,Muted);
            Label(root,"Edition","深渊猎手     /     01",1164,860,350,25,13,Muted,TextAnchor.MiddleRight);
            StartCoroutine(FadeIn(root));
        }

        public void BeginRun()
        {
            if (State != ScreenState.Menu) return;
            State=ScreenState.Transition;
            StartCoroutine(TransitionTo(SceneFlow.LoadGame));
        }

        private void ShowControls()
        {
            ClearModal(); modal=Overlay("狩猎手册", "沿微光深入遗迹，猎杀守卫，夺回核心，再返回入口撤离。");
            Label(modal,"Controls","W A S D     移动，沿断桥探索三处遗迹\n鼠标左键     近战攻击，自动朝向附近的存活敌人\n空格键        闪避，短暂无敌，可取消尚未命中的攻击\nI 键             打开背包，点击生命药水恢复生命\nE 键            取回核心 / 投入四枚碎片 / 撤离\nEsc 键         暂停或继续；本次远征不提供中途回档",370,369,900,235,23,White);
            Label(modal,"Tip","橙色扇区提示重击，青色提示反击。碎片靠近自动拾取，用于点亮归途信标。",370,610,900,40,18,Muted);
            Button(modal,"Close","返回",370,653,350,ClearModal,true);
        }

        private void BuildGame()
        {
            MatchResult.Reset();
            player=FindFirstObjectByType<PlayerMovement>();
            spawner=FindFirstObjectByType<EnemyWaveSpawner>();
            expedition=FindFirstObjectByType<AbyssExpedition>();
            if (player == null || spawner == null) { Debug.LogError("Presentation needs a player and wave spawner."); return; }
            health=player.GetComponent<Health>(); inventory=player.GetComponent<PlayerInventory>();
            health.Died+=OnDeath; health.DamageTaken+=OnDamage;
            spawner.WaveStarted+=OnWave; spawner.EnemyDefeated+=OnDefeated;
            if(expedition!=null){expedition.Completed+=OnVictory;expedition.Announced+=ShowAnnouncement;}
            else spawner.AllWavesCompleted+=OnVictory;
            hud=Rect(root,"Combat HUD",0,0,1600,900);
            var left=Box(hud,"Vitals plate",36,32,350,112,Panel).transform;
            Box(left,"Accent",0,0,4,112,Cyan);
            Label(left,"Name","猎手  /  生命",22,12,220,30,15,Muted);
            hpLabel=Label(left,"Health value","100 / 100",190,12,138,30,19,White,TextAnchor.MiddleRight);
            Box(left,"Health track",22,56,306,12,new Color(.13f,.23f,.27f));
            hpFill=Box(left,"Health fill",22,56,306,12,Cyan);
            Label(left,"Health caption","保持战斗，守住微光",22,78,306,26,12,Muted);
            var right=Box(hud,"Objective plate",1164,32,400,112,Panel).transform;
            waveLabel=Label(right,"Wave","波次  01 / 03",22,12,306,35,24,White);
            enemyLabel=Label(right,"Remaining","正在准备战斗",22,58,362,30,16,Gold);
            Box(hud,"Run plate",670,32,260,50,Panel);
            timerLabel=Label(hud,"Timer","用时   00:00",670,32,260,50,20,White,TextAnchor.MiddleCenter);
            lootLabel=Label(hud,"Loot","碎片  00    /    药水  00",40,159,500,32,16,White);
            var bottom=Box(hud,"Control strip",390,824,820,46,Panel).transform;
            Label(bottom,"Controls","WASD 移动   左键 攻击   空格 闪避   E 交互   I 背包   Esc 暂停",18,0,784,46,16,Muted,TextAnchor.MiddleCenter);
            interactionLabel=Label(hud,"Interaction","",380,753,840,42,23,Gold,TextAnchor.MiddleCenter);
            destinationLabel=Label(hud,"Destination","",1164,152,400,32,17,Cyan,TextAnchor.MiddleRight);
            var ability=Box(hud,"Dodge plate",1344,788,220,82,Panel).transform;
            dodgeLabel=Label(ability,"Dodge","空格  /  闪避就绪",16,10,188,32,16,Cyan);
            Box(ability,"Cooldown track",16,57,188,4,new Color(.12f,.23f,.27f));
            dodgeFill=Box(ability,"Cooldown",16,57,188,4,Cyan);
            var banner=Box(root,"Wave banner",480,174,640,108,Panel);
            bannerGroup=banner.gameObject.AddComponent<CanvasGroup>(); bannerGroup.alpha=0; bannerGroup.blocksRaycasts=false;
            Box(banner.transform,"Banner accent",0,0,640,2,Gold);
            bannerTitle=Label(banner.transform,"Title","",0,12,640,46,30,White,TextAnchor.MiddleCenter);
            bannerSubtitle=Label(banner.transform,"Subtitle","",0,60,640,30,16,Gold,TextAnchor.MiddleCenter);
            StartCoroutine(Opening());
        }

        private IEnumerator Opening()
        {
            State=ScreenState.Intro; Time.timeScale=0;
            modal=Rect(root,"Opening",0,0,1600,900);
            Box(modal,"Tint",-1600,-900,4800,2700,new Color(.02f,.05f,.08f,.65f));
            Label(modal,"Chapter","深渊远征  /  01",0,270,1600,38,19,Gold,TextAnchor.MiddleCenter);
            Label(modal,"Title","沉没遗迹追猎",0,320,1600,84,58,White,TextAnchor.MiddleCenter);
            Label(modal,"Goal","猎杀侵蚀守卫  /  夺回核心  /  点亮归途",0,420,1600,40,18,Cyan,TextAnchor.MiddleCenter);
            var count=Label(modal,"Countdown","",0,492,1600,74,48,Gold,TextAnchor.MiddleCenter);
            Label(modal,"Instruction","WASD 移动    /    鼠标左键攻击    /    空格键闪避",0,619,1600,40,22,White,TextAnchor.MiddleCenter);
            for(int i=3;i>0;i--) { count.text=i.ToString("00"); yield return WaitUnscaled(1); }
            ClearModal(); State=ScreenState.Playing; Time.timeScale=1;
        }

        private void Update()
        {
            uiTime += AnimationDelta;
            var k=Keyboard.current;
            if(k!=null)
            {
                if(State==ScreenState.Menu)
                {
                    if(modal!=null) { if(k.escapeKey.wasPressedThisFrame) ClearModal(); }
                    else if(k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) BeginRun();
                }
                else if(State==ScreenState.Playing)
                {
                    if(k.escapeKey.wasPressedThisFrame) ShowPause();
                    else if(k.iKey.wasPressedThisFrame) ShowInventory();
                }
                else if(State==ScreenState.Paused || State==ScreenState.Inventory)
                {
                    if(k.escapeKey.wasPressedThisFrame || (State==ScreenState.Inventory && k.iKey.wasPressedThisFrame)) Resume();
                    else if(k.mKey.wasPressedThisFrame) ReturnToMenu();
                }
                else if(State==ScreenState.Result)
                {
                    if(k.rKey.wasPressedThisFrame) Restart();
                    else if(k.mKey.wasPressedThisFrame) ReturnToMenu();
                }
            }
            if(hud==null || health==null) return;
            if(State==ScreenState.Playing) RunSeconds+=Time.deltaTime;
            shownHealth=Mathf.MoveTowards(shownHealth, (float)health.CurrentHealth/health.MaximumHealth,AnimationDelta*1.5f);
            hpFill.rectTransform.sizeDelta=new Vector2(306*shownHealth,12);
            hpFill.color=shownHealth<.3f?Red:Cyan;
            hpLabel.text=$"{health.CurrentHealth} / {health.MaximumHealth}";
            timerLabel.text="用时   "+FormatTime(RunSeconds);
            waveLabel.text=$"波次  {spawner.CurrentWave:00} / {spawner.TotalWaves:00}";
            enemyLabel.text=spawner.CurrentWave==0?"正在准备战斗":spawner.EnemiesLeftInWave>0?$"剩余敌人  {spawner.EnemiesLeftInWave:00}":"本波已清除  /  稍作休整";
            if(expedition!=null)
            {
                waveLabel.text=expedition.Objective;enemyLabel.text=expedition.Detail;
                interactionLabel.text=expedition.InteractionHint;
                var target=expedition.Destination;
                if(target!=null)
                {
                    Vector3 delta=target.position-player.transform.position;
                    Vector3 view=Camera.main.transform.InverseTransformDirection(delta);
                    string direction=Mathf.Abs(view.x)>Mathf.Abs(Vector3.Dot(delta,Vector3.forward)) ? (view.x>0?"右方":"左方") : delta.z>0?"前方":"后方";
                    destinationLabel.text=$"目标 · {direction}  {delta.magnitude:0} 米";
                }
            }
            lootLabel.text=$"碎片  {CountItem(false):00}    /    药水  {CountItem(true):00}";
            float ready=player.DodgeReadiness;
            dodgeFill.rectTransform.sizeDelta=new Vector2(188*ready,4);
            dodgeLabel.text=ready>=1?"空格  /  闪避就绪":"空格  /  闪避冷却中";
            if(bannerGroup!=null) bannerGroup.alpha=Mathf.MoveTowards(bannerGroup.alpha,uiTime<bannerUntil?1:0,AnimationDelta*3);
        }

        private int CountItem(bool potion)
        {
            int count=0;
            if(inventory!=null) foreach(var slot in inventory.Slots)
                if(!slot.IsEmpty && (slot.Item is HealingItemData)==potion) count+=slot.Quantity;
            return count;
        }
        public static string FormatTime(float time) => $"{(int)time/60:00}:{(int)time%60:00}";
        private void OnWave(int wave,int total)
        {
            if(expedition!=null){ShowAnnouncement(spawner.CurrentEncounterTitle,"观察攻击前摇，闪避落锤后反击。");return;}
            bannerTitle.text=wave==total?"最后一波":$"第 {wave:00} 波";
            bannerSubtitle.text=string.IsNullOrEmpty(spawner.CurrentEncounterTitle)
                ? (wave==total?"守住阵地，完成最后的狩猎。":"深渊中的敌人已发现你。")
                : spawner.CurrentEncounterTitle;
            bannerUntil=uiTime+2.5f;
        }
        private void ShowAnnouncement(string title,string subtitle)
        {bannerTitle.text=title;bannerSubtitle.text=subtitle;bannerUntil=uiTime+3.5f;}
        private void OnDefeated() => DefeatedCount++;
        private void OnDamage(int amount) => damageTaken+=amount;
        private void OnDeath() => EndRun(false);
        private void OnVictory() { if(!health.IsDead) EndRun(true); }
        private void EndRun(bool won)
        {
            if(!MatchResult.TryEnd()) return;
            // Snapshot before the player's death lifecycle can destroy its inventory.
            resultShards=CountItem(false)+(expedition!=null?expedition.DepositedShards:0); resultHealth=health.CurrentHealth;
            resultDepth=spawner.CurrentWave; resultDefeated=DefeatedCount;
            resultWon=won; State=ScreenState.Finishing;
            StartCoroutine(Finish());
        }
        private IEnumerator Finish()
        {
            yield return WaitUnscaled(.7f);
            Time.timeScale=0; State=ScreenState.Result; hud.gameObject.SetActive(false);
            bannerGroup.alpha=0; bannerUntil=0;
            ClearModal();
            modal=Overlay(resultWon?"携光归来":"微光消逝",resultWon?"深渊核心已回收，归途信标已点亮。猎手，欢迎归来。":"狩猎暂告终结。记住敌人的节奏，再次出发。");
            Color accent=resultWon?Gold:Red;
            Label(modal,"Result tag",resultWon?"远征成功":"远征失败",370,196,860,34,17,accent);
            Stat(370,"战斗用时",FormatTime(RunSeconds));
            Stat(664,"击败敌人",resultDefeated.ToString("00"));
            Stat(958,"收集碎片",resultShards.ToString("00"));
            Label(modal,"Detail",$"遭遇 {resultDepth:00} / {spawner.TotalWaves:00}     /     承受伤害 {damageTaken}     /     剩余生命 {resultHealth}",370,550,860,40,17,Muted);
            Button(modal,"Retry","R 键  /  再次狩猎",370,638,420,Restart,true);
            Button(modal,"Menu","M 键  /  返回主菜单",810,638,420,ReturnToMenu);
            Label(modal,"Footer","深渊猎手   /   最后一束光",370,727,860,30,15,Muted);
            StartCoroutine(FadeIn(modal));
        }
        private void Stat(float x,string title,string value)
        {
            var panel=Box(modal,title,x,394,272,133,new Color(.08f,.15f,.19f));
            Label(panel.transform,"Caption",title,20,13,240,28,14,Muted);
            Label(panel.transform,"Value",value,20,42,240,80,48,White);
        }
        private RectTransform Overlay(string title,string subtitle)
        {
            var r=Rect(root,"Modal",0,0,1600,900);
            var veil=Box(r,"Veil",-1600,-900,4800,2700,new Color(.015f,.035f,.055f,.88f)); veil.raycastTarget=true;
            Box(r,"Card",322,150,956,620,Panel);
            Box(r,"Accent",322,150,956,3,Cyan);
            Label(r,"Title",title,370,236,860,78,48,White);
            Label(r,"Subtitle",subtitle,370,315,860,40,20,Muted);
            return r;
        }
        public void ShowPause()
        {
            if(State!=ScreenState.Playing) return;
            State=ScreenState.Paused; Time.timeScale=0;
            modal=Overlay("稍作休整","游戏已暂停，准备好后再继续狩猎。");
            Label(modal,"Tag","已暂停  /  "+FormatTime(RunSeconds),370,196,860,34,17,Gold);
            Button(modal,"Resume","Esc 键  /  继续狩猎",370,410,860,Resume,true);
            Button(modal,"Restart","重新开始本局",370,488,860,Restart);
            Button(modal,"Menu","M 键  /  返回主菜单",370,566,860,ReturnToMenu);
            Label(modal,"Hint","WASD 移动     /     左键攻击     /     空格闪避     /     I 键背包",370,681,860,36,18,Muted);
        }
        public void ShowInventory()
        {
            if(State!=ScreenState.Playing && State!=ScreenState.Inventory) return;
            ClearModal(); State=ScreenState.Inventory; Time.timeScale=0;
            modal=Overlay("随身背包","点击生命药水恢复 30 点生命。背包打开期间，游戏暂停。");
            Label(modal,"Tag","当前生命  /  "+health.CurrentHealth+" 点",370,196,860,34,17,Gold);
            for(int i=0;i<inventory.Slots.Count;i++)
            {
                var slot=inventory.Slots[i]; int index=i;
                bool potion=!slot.IsEmpty && slot.Item is HealingItemData;
                string title=slot.IsEmpty?"空":$"{slot.Item.DisplayName} ×{slot.Quantity}";
                var b=Button(modal,"Slot "+i,title,370+(i%4)*220,385+(i/4)*73,200,()=>{inventory.TryUseSlot(index);ShowInventory();});
                b.interactable=potion && health.CurrentHealth<health.MaximumHealth;
            }
            Button(modal,"Close","I 或 Esc 键  /  返回狩猎",370,653,860,Resume,true);
        }
        public void Resume()
        {
            if(State!=ScreenState.Paused && State!=ScreenState.Inventory) return;
            ClearModal(); State=ScreenState.Playing; Time.timeScale=1;
        }
        public void Restart() { State=ScreenState.Transition; StartCoroutine(TransitionTo(SceneFlow.LoadGame)); }
        public void ReturnToMenu() { State=ScreenState.Transition; StartCoroutine(TransitionTo(SceneFlow.LoadMenu)); }
        private void ClearModal() { if(modal!=null) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); modal=null; } }
        private IEnumerator FadeIn(RectTransform target)
        {
            var group=target.gameObject.AddComponent<CanvasGroup>(); group.alpha=0;
            while(group!=null && group.alpha<1) { group.alpha+=AnimationDelta*2.5f; yield return null; }
        }
        private static IEnumerator WaitUnscaled(float duration)
        {
            for(float elapsed=0;elapsed<duration;elapsed+=AnimationDelta) yield return null;
        }
        private IEnumerator TransitionTo(System.Action action)
        {
            var fade=Box(root,"Transition",-1600,-900,4800,2700,new Color(Ink.r,Ink.g,Ink.b,0)); fade.raycastTarget=true;
            float t=0;
            while(t<1) { t+=AnimationDelta*3; fade.color=new Color(Ink.r,Ink.g,Ink.b,t); yield return null; }
            action();
        }
        private void OnDestroy()
        {
            if(health!=null) {health.Died-=OnDeath;health.DamageTaken-=OnDamage;}
            if(spawner!=null) {spawner.WaveStarted-=OnWave;spawner.EnemyDefeated-=OnDefeated;spawner.AllWavesCompleted-=OnVictory;}
            if(expedition!=null){expedition.Completed-=OnVictory;expedition.Announced-=ShowAnnouncement;}
            if(Current==this) {Current=null;Time.timeScale=1;}
        }
    }
}


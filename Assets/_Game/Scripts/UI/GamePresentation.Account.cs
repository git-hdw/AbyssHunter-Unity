using AbyssHunter.Combat;
using UnityEngine;
using UnityEngine.UI;
using static AbyssHunter.UI.PresentationTheme;

namespace AbyssHunter.UI
{
    public sealed partial class GamePresentation
    {
        private PlayFabAuthentication accountService;
        private InputField emailInput, passwordInput, confirmationInput;
        private Text accountMessage, recoveryCaption;
        private Button submitAccount;
        private string accountMode;
        private static bool selectOnReturn;

        private InputField AccountField(string name, string hint, float y, bool password)
        {
            var background = Box(modal, name, 370, y, 860, 50, new Color(.08f,.15f,.19f));
            background.raycastTarget = true;
            var field = background.gameObject.AddComponent<InputField>();
            field.textComponent = Label(background.transform,"Value","",16,0,828,50,22,White);
            field.placeholder = Label(background.transform,"Hint",hint,16,0,828,50,20,Muted);
            field.contentType = password ? InputField.ContentType.Password : InputField.ContentType.EmailAddress;
            field.characterLimit = password ? 100 : 254;
            return field;
        }
        private void ShowAccount(string mode)
        {
            if (accountService == null)
            { accountService = PlayFabAuthentication.Instance; accountService.Changed += OnAccountChanged; }
            ClearSensitiveFields(); ClearModal(); State = ScreenState.Login; accountMode = mode;
            if (mode == "resume")
            {
                modal = Overlay("欢迎回来", "正在验证这台电脑上次使用的猎手账号。");
                accountMessage = Label(modal,"AccountMessage",accountService.Message,370,430,860,80,22,Gold);
                submitAccount = Button(modal,"RetrySession","重新连接",370,618,400,accountService.ResumeLogin,true);
                Button(modal,"SwitchAccount","使用其他账号",800,618,430,accountService.SignOut);
                Button(modal,"AccountQuit","退出游戏",800,688,430,SceneFlow.Quit);
                return;
            }
            modal = Overlay(mode == "register" ? "创建猎手账号" : mode == "recover" ? "找回密码" : "猎手登录",
                mode == "recover" ? "通过注册邮箱找回密码" : PlayFabAuthentication.SupportsRememberedLogin
                    ? "登录后在此电脑保持登录，主菜单可退出账号。" : "邮箱账号联网认证 · 通关记录保存在本机");
            emailInput = AccountField("Email", "邮箱", 375, false);
            if (mode != "recover") passwordInput = AccountField("Password", "密码（6–100 个字符）", 435, true);
            if (mode == "register") confirmationInput = AccountField("Confirmation", "再次输入密码", 495, true);
            accountMessage = Label(modal,"AccountMessage",accountService.Message,370,551,860,60,18,Gold);
            submitAccount = Button(modal,"AccountSubmit",mode == "register" ? "注册并进入" : mode == "recover" ? "发送恢复邮件" : "登录",370,618,400,SubmitAccount,true);
            recoveryCaption = submitAccount.transform.Find("Label").GetComponent<Text>();
            Button(modal,"AccountBack",mode == "login" ? "注册账号" : "返回登录",800,618,430,()=>{if(!accountService.IsBusy)ShowAccount(mode=="login"?"register":"login");});
            if (mode == "login") Button(modal,"ForgotPassword","忘记密码",370,688,400,()=>{if(!accountService.IsBusy)ShowAccount("recover");});
            Button(modal,"AccountQuit","退出游戏",800,688,430,SceneFlow.Quit);
        }
        private void ClearSensitiveFields()
        {
            if (passwordInput != null) passwordInput.text = "";
            if (confirmationInput != null) confirmationInput.text = "";
            emailInput = passwordInput = confirmationInput = null;
        }
        private void SubmitAccount()
        {
            if (accountService.IsBusy) return;
            if (accountMode == "register" && passwordInput.text != confirmationInput.text)
            { accountMessage.text = "两次输入的密码不一致。"; return; }
            if (accountMode == "recover") accountService.Recover(emailInput.text);
            else if (accountMode == "register") accountService.Register(emailInput.text, passwordInput.text);
            else accountService.Login(emailInput.text, passwordInput.text);
        }
        private void OnAccountChanged()
        {
            if (accountService.IsSignedIn) { ClearSensitiveFields(); SceneFlow.LoadMenu(); return; }
            if (accountMode == "resume" && !accountService.CanResume) { ShowAccount("login"); return; }
            if (accountMessage != null) accountMessage.text = accountService.Message;
        }
        private void UpdateAccountView()
        {
            if (State == ScreenState.Menu && selectOnReturn) { selectOnReturn = false; ShowLevels(); }
            if (State != ScreenState.Login || submitAccount == null) return;
            bool busy = accountService.IsBusy;
            if (accountMode == "resume") { submitAccount.interactable = !busy; return; }
            emailInput.interactable = !busy;
            if (passwordInput != null) passwordInput.interactable = !busy;
            if (confirmationInput != null) confirmationInput.interactable = !busy;
            submitAccount.interactable = !busy && (accountMode != "recover" || accountService.RecoveryRemaining <= 0);
            if (accountMode == "recover") recoveryCaption.text = accountService.RecoveryRemaining > 0 ? $"请等待 {Mathf.CeilToInt(accountService.RecoveryRemaining)} 秒" : "发送恢复邮件";
        }
        private void ShowLevels()
        {
            if (!SceneFlow.CanEnter(LevelCatalog.All[0])) { ShowAccount("login"); return; }
            ClearModal(); State = ScreenState.LevelSelect;
            modal = Overlay("选择远征", "完成撤离才会记录通关；每次远征从满生命、空背包开始。");
            for (int i = 0; i < LevelCatalog.All.Length; i++)
            {
                var level = LevelCatalog.All[i];
                bool unlocked = SceneFlow.CanEnter(level);
                var button = Button(modal,"Level " + level.Id,level.Title,370,390+i*110,860,()=>EnterLevel(level),unlocked);
                button.interactable = unlocked;
                float best = PlayFabAuthentication.Instance.Progress?.BestSeconds(level.Id) ?? 0;
                Label(modal,"Level detail",!unlocked ? "成功撤离「沉没遗迹追猎」后解锁" : best > 0 ? "已通关 · 最佳用时 " + FormatTime(best) : "可以出发 · 尚未通关",370,448+i*110,860,30,18,Muted);
            }
            Label(modal,"ProfileNotice",PlayFabAuthentication.Instance.Progress?.Notice ?? "记录仅保存在当前电脑，换电脑需要重新解锁。",370,590,860,40,17,Gold);
            Button(modal,"BackToMenu","返回主菜单",370,665,860,()=>{ClearModal();State=ScreenState.Menu;});
        }
        private void EnterLevel(LevelDefinition level)
        {
            if (State != ScreenState.Menu && State != ScreenState.LevelSelect && State != ScreenState.Result) return;
            if (!SceneFlow.CanEnter(level)) return;
            departureLevel = level;
            previousScreen = State;
            previousModal = modal;
            if (previousModal) previousModal.gameObject.SetActive(false);
            State = ScreenState.WeaponSelect;
            modal = Overlay("选择出战武器", level.Title + "  /  选定即出发，本次远征全程使用同一把武器。");
            Button(modal, "Equip Sword", "1    猎剑      /      快速挥砍", 370, 378, 860, () => DepartWith(HunterWeapon.Sword));
            Button(modal, "Equip Axe", "2    重斧      /      双手横扫", 370, 456, 860, () => DepartWith(HunterWeapon.Axe));
            Button(modal, "Equip Crossbow", "3    轻弩      /      远程单发", 370, 534, 860, () => DepartWith(HunterWeapon.Crossbow));
            Button(modal, "WeaponBack", "返回", 370, 656, 270, BackFromWeapons);
        }

        private LevelDefinition departureLevel;
        private ScreenState previousScreen;
        private RectTransform previousModal;

        private void BackFromWeapons()
        {
            if (State != ScreenState.WeaponSelect) return;
            ClearModal();
            State = previousScreen;
            modal = previousModal;
            previousModal = null;
            if (modal) modal.gameObject.SetActive(true);
            departureLevel = null;
        }

        private void DepartWith(HunterWeapon weapon)
        {
            if (State != ScreenState.WeaponSelect || !SceneFlow.CanEnter(departureLevel)) return;
            var id = departureLevel.Id;
            State = ScreenState.Transition;
            StartCoroutine(TransitionTo(() => SceneFlow.LoadLevel(id, weapon)));
        }
        private void ReturnToSelection() { selectOnReturn=PlayFabAuthentication.Instance.IsSignedIn; ReturnToMenu(); }
    }
}

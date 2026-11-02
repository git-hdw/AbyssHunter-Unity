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
            modal = Overlay(mode == "register" ? "创建猎手账号" : mode == "recover" ? "找回密码" : "猎手登录", "邮箱账号联网认证 · 通关记录保存在本机");
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
            if (accountMessage != null) accountMessage.text = accountService.Message;
        }
        private void UpdateAccountView()
        {
            if (State == ScreenState.Menu && selectOnReturn) { selectOnReturn = false; ShowLevels(); }
            if (State != ScreenState.Login || submitAccount == null) return;
            bool busy = accountService.IsBusy;
            emailInput.interactable = !busy;
            if (passwordInput != null) passwordInput.interactable = !busy;
            if (confirmationInput != null) confirmationInput.interactable = !busy;
            submitAccount.interactable = !busy && (accountMode != "recover" || accountService.RecoveryRemaining <= 0);
            if (accountMode == "recover") recoveryCaption.text = accountService.RecoveryRemaining > 0 ? $"请等待 {Mathf.CeilToInt(accountService.RecoveryRemaining)} 秒" : "发送恢复邮件";
        }
        private void ShowLevels()
        {
            if (!PlayFabAuthentication.Instance.IsSignedIn) { ShowAccount("login"); return; }
            ClearModal(); State = ScreenState.LevelSelect;
            modal = Overlay("选择远征", "完成撤离才会记录通关；每次远征从满生命、空背包开始。");
            for (int i = 0; i < LevelCatalog.All.Length; i++)
            {
                var level = LevelCatalog.All[i];
                bool unlocked = level.IsUnlocked;
                var button = Button(modal,"Level " + level.Id,level.Title,370,390+i*110,860,()=>EnterLevel(level),unlocked);
                button.interactable = unlocked;
                float best = PlayFabAuthentication.Instance.Progress.BestSeconds(level.Id);
                Label(modal,"Level detail",!unlocked ? "成功撤离「沉没遗迹追猎」后解锁" : best > 0 ? "已通关 · 最佳用时 " + FormatTime(best) : "可以出发 · 尚未通关",370,448+i*110,860,30,18,Muted);
            }
            Label(modal,"ProfileNotice",PlayFabAuthentication.Instance.Progress.Notice ?? "记录仅保存在当前电脑，换电脑需要重新解锁。",370,590,860,40,17,Gold);
            Button(modal,"BackToMenu","返回主菜单",370,665,860,()=>{ClearModal();State=ScreenState.Menu;});
        }
        private void EnterLevel(LevelDefinition level)
        {
            if (!SceneFlow.CanEnter(level)) return;
            State=ScreenState.Transition; StartCoroutine(TransitionTo(()=>SceneFlow.LoadLevel(level.Id)));
        }
        private void ReturnToSelection() { selectOnReturn=PlayFabAuthentication.Instance.IsSignedIn; ReturnToMenu(); }
    }
}

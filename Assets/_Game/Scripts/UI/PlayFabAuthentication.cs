using System;
using System.Collections;
using AbyssHunter.Save;
using UnityEngine;
using UnityEngine.Networking;

namespace AbyssHunter.UI
{
    public interface IAccountAuthentication
    {
        string PlayerId { get; }
        bool IsBusy { get; }
        event Action Changed;
        void Register(string email, string password);
        void Login(string email, string password);
        void Recover(string email);
        void SignOut();
    }

    public sealed class PlayFabAuthentication : MonoBehaviour, IAccountAuthentication
    {
        [Serializable] private sealed class Configuration { public string titleId; }
        [Serializable] private sealed class Request { public string TitleId; public string Email; public string Password; public bool RequireBothUsernameAndEmail; }
        [Serializable] private sealed class Response { public int code; public string error; public Result data; }
        [Serializable] private sealed class Result { public string PlayFabId; }
        private static PlayFabAuthentication instance;
        public static PlayFabAuthentication Instance
        {
            get
            {
                if (instance == null) { instance = new GameObject("Account session").AddComponent<PlayFabAuthentication>(); DontDestroyOnLoad(instance.gameObject); }
                return instance;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; }
        public string PlayerId { get; private set; }
        public string TitleId { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsSignedIn => !string.IsNullOrEmpty(PlayerId);
        public string Message { get; private set; } = "请使用邮箱登录猎手账号。";
        public float RecoveryRemaining => Mathf.Max(0, recoveryUntil - Time.realtimeSinceStartup);
        public AccountProgress Progress { get; private set; }
        public event Action Changed;
        private float recoveryUntil;
        private UnityWebRequest activeRequest;
        private int generation;
        private void Awake()
        {
            var config = Resources.Load<TextAsset>("PlayFabConfig");
            try { if (config != null) TitleId = JsonUtility.FromJson<Configuration>(config.text)?.titleId?.Trim(); }
            catch (ArgumentException) { Message = "账号服务配置无法读取，请联系开发者。"; }
        }
        public void Register(string email, string password) => Begin("RegisterPlayFabUser", email, password);
        public void Login(string email, string password) => Begin("LoginWithEmailAddress", email, password);
        public void Recover(string email)
        {
            if (RecoveryRemaining > 0) return;
            Begin("SendAccountRecoveryEmail", email, null);
        }
        private void Begin(string method, string email, string password)
        {
            if (IsBusy || IsSignedIn) return;
            if (string.IsNullOrEmpty(TitleId) || !System.Text.RegularExpressions.Regex.IsMatch(TitleId, "^[A-Za-z0-9]+$"))
            { Message = "账号服务尚未配置，请设置 PlayFab Title ID。"; Changed?.Invoke(); return; }
            if (string.IsNullOrWhiteSpace(email) || (password != null && password.Length == 0))
            { Message = "请填写邮箱和密码。"; Changed?.Invoke(); return; }
            StartCoroutine(Send(method, email.Trim(), password, generation));
        }
        private IEnumerator Send(string method, string email, string password, int requestGeneration)
        {
            IsBusy = true; Message = "正在连接账号服务……"; Changed?.Invoke();
            if (password == null) recoveryUntil = Time.realtimeSinceStartup + 60;
            var body = new Request { TitleId = TitleId, Email = email, Password = password, RequireBothUsernameAndEmail = false };
            using var request = new UnityWebRequest("https://" + TitleId + ".playfabapi.com/Client/" + method, "POST");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 15;
            activeRequest = request;
            yield return request.SendWebRequest();
            activeRequest = null;
            if (requestGeneration != generation) yield break;
            Response response = null;
            try { response = JsonUtility.FromJson<Response>(request.downloadHandler.text); } catch (Exception) { }
            IsBusy = false;
            if (request.result == UnityWebRequest.Result.ConnectionError) Message = "连接失败或请求超时，请检查网络后重试。";
            else if (password == null && (response?.code == 200 || response?.error == "EmailAddressNotAvailable" || response?.error == "AccountNotFound" || response?.error == "NoContactEmailAddressFound" || response?.error == "EmailRecipientBlacklisted"))
                Message = "若邮箱已注册，请检查收件箱及垃圾邮件，按邮件指引重置密码。";
            else if (request.result == UnityWebRequest.Result.Success && response?.code == 200 && !string.IsNullOrEmpty(response.data?.PlayFabId))
            {
                PlayerId = response.data.PlayFabId;
                Progress = new AccountProgress(System.IO.Path.Combine(Application.persistentDataPath, "Accounts"), TitleId, PlayerId);
                Message = Progress.Notice ?? "登录成功。";
            }
            else Message = FriendlyError(response?.error);
            Changed?.Invoke();
        }
        private static string FriendlyError(string error) => error switch
        {
            "EmailAddressNotAvailable" or "NameNotAvailable" => "邮箱已注册，请登录或找回密码。",
            "InvalidEmailOrPassword" or "InvalidPassword" or "AccountNotFound" => "邮箱或密码不正确。",
            "InvalidEmailAddress" or "InvalidParams" => "请检查邮箱格式与密码要求（6–100 个字符）。",
            "InvalidTitleId" or "SmtpAddonNotEnabled" or "APIRequestsDisabledForTitle" => "账号或邮件服务配置有误，请联系开发者。",
            "APIRequestLimitExceeded" or "AccountBanned" => "请求受限或账号不可用，请稍后重试或联系开发者。",
            _ => "账号服务暂时不可用，请稍后重试。"
        };
        public void SignOut()
        {
            generation++; activeRequest?.Abort(); StopAllCoroutines(); activeRequest?.Dispose(); activeRequest = null;
            IsBusy = false; PlayerId = null; Progress = null; Message = "已退出账号。"; Changed?.Invoke();
        }
        private void OnDestroy() { activeRequest?.Abort(); StopAllCoroutines(); activeRequest?.Dispose(); activeRequest = null; if (instance == this) instance = null; }
    }
}

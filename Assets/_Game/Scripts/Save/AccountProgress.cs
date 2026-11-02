using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AbyssHunter.Save
{
    // Local progress only: never contains email, password or authentication tickets.
    public sealed class AccountProgress
    {
        [Serializable] public sealed class Entry { public string levelId; public bool completed; public float bestSeconds; }
        [Serializable] private sealed class Data { public int version = 1; public List<Entry> levels = new(); }
        private Data data = new();
        private readonly string path;
        public string Notice { get; private set; }
        public AccountProgress(string directory, string titleId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(titleId) || string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("Missing account identity");
            using var hash = SHA256.Create();
            string key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(titleId + ":" + playerId))).Replace("-", "");
            path = Path.Combine(directory, key + ".json");
            Load();
        }
        public bool Completed(string id) => data.levels.Exists(e => e.levelId == id && e.completed);
        public float BestSeconds(string id) => data.levels.Find(e => e.levelId == id && e.completed)?.bestSeconds ?? 0;
        private static Data Read(string file)
        {
            string json = File.ReadAllText(file);
            if (!json.TrimStart().StartsWith("{") || !json.Contains("\"version\"") || !json.Contains("\"levels\"")) throw new InvalidDataException();
            var value = JsonUtility.FromJson<Data>(json);
            if (value == null || value.version != 1 || value.levels == null) throw new InvalidDataException();
            var ids = new HashSet<string>();
            foreach (var e in value.levels)
                if (e == null || string.IsNullOrWhiteSpace(e.levelId) || !ids.Add(e.levelId) || float.IsNaN(e.bestSeconds) || float.IsInfinity(e.bestSeconds) || e.bestSeconds < 0) throw new InvalidDataException();
            return value;
        }
        private void Load()
        {
            if (!File.Exists(path)) return;
            try { data = Read(path); }
            catch (Exception)
            {
                try
                {
                    File.Copy(path, path + ".damaged-" + Guid.NewGuid().ToString("N"));
                    if (File.Exists(path + ".bak")) { data = Read(path + ".bak"); Notice = "档案损坏，已读取备份；原文件已保留。"; }
                    else Notice = "档案损坏，已恢复默认档案；原文件已保留。";
                }
                catch (Exception) { Notice = "档案无法读取，暂用默认档案；原文件保留，请检查磁盘权限。"; }
            }
        }
        public bool RecordCompletion(string levelId, float seconds)
        {
            if (string.IsNullOrWhiteSpace(levelId) || seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            var next = JsonUtility.FromJson<Data>(JsonUtility.ToJson(data));
            var entry = next.levels.Find(e => e.levelId == levelId);
            if (entry == null) { entry = new Entry { levelId = levelId }; next.levels.Add(entry); }
            if (!entry.completed || seconds < entry.bestSeconds) entry.bestSeconds = seconds;
            entry.completed = true;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(next, true));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
                else File.Move(path + ".tmp", path);
                data = next; Notice = "通关记录已保存到本机。"; return true;
            }
            catch (Exception) { Notice = "通关记录保存失败，请检查磁盘空间和权限；本次未解锁新关卡。"; return false; }
        }
    }
}

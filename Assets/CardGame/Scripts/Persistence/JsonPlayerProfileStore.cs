using System;
using System.IO;
using UnityEngine;

namespace CardGame.Persistence
{
    public interface IPlayerProfileStore
    {
        PlayerProfile Load();
        void Save(PlayerProfile profile);
    }

    /// <summary>
    /// 使用 Unity JsonUtility 的本地档案实现。接口隔离让后续替换为云存档、SQLite
    /// 或平台账号存储时不必修改战斗和 UI 代码。
    /// </summary>
    public sealed class JsonPlayerProfileStore : IPlayerProfileStore
    {
        private readonly string filePath;

        public JsonPlayerProfileStore(string filePath = null)
        {
            this.filePath = string.IsNullOrWhiteSpace(filePath)
                ? Path.Combine(Application.persistentDataPath, "card-game-profile.json")
                : filePath;
        }

        public PlayerProfile Load()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new PlayerProfile();
                }

                string json = File.ReadAllText(filePath);
                return string.IsNullOrWhiteSpace(json)
                    ? new PlayerProfile()
                    : JsonUtility.FromJson<PlayerProfile>(json) ?? new PlayerProfile();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"读取玩家档案失败，将使用默认档案。\n{exception.Message}");
                return new PlayerProfile();
            }
        }

        public void Save(PlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, JsonUtility.ToJson(profile, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"保存玩家档案失败。\n{exception.Message}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using Windows.Storage;
using System.Text.Json.Serialization;

namespace Yee_Music.Models
{
    public class AppSettings
    {
        private static AppSettings _instance;
        private static readonly object _lock = new object();
        private string _settingsFilePath;

        // 添加一个字典来存储额外的设置
        private Dictionary<string, object> _additionalSettings = new Dictionary<string, object>();

        // 单例访问器
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }
        // 设置属性
        public List<string> MusicLibraryPaths { get; set; } = new List<string>();
        public string PlayMode { get; set; } = "Sequential";

        // 修改为直接属性，不再使用GetSetting/SaveSetting
        [JsonPropertyName("IsShuffleEnabled")]
        public bool IsShuffleEnabled { get; set; } = false;

        public string LastPlayedMusicPath { get; set; } = "";
        public double LastPlaybackPosition { get; set; } = 0;
        public double Volume { get; set; } = 0.5;
        public string ThemeSetting { get; set; } = "Default";
        public string WindowMaterial { get; set; } = "Mica";
        public bool UseFallbackMaterial { get; set; } = true;
        public string TintColor { get; set; } = "#00000000";
        public bool IsFirstRun { get; set; } = true;

        // 添加额外设置的字典，用于序列化
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalSettings
        {
            get => _additionalSettings;
            set => _additionalSettings = value ?? new Dictionary<string, object>();
        }

        [JsonConstructor]
        public AppSettings()
        {
            // 保持默认值
            IsFirstRun = true;
            // 其他默认值...
            _additionalSettings = new Dictionary<string, object>();
        }

        // 私有构造函数，防止外部创建实例
        private AppSettings(string settingsFilePath)
        {
            _settingsFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");
            _additionalSettings = new Dictionary<string, object>();
        }

        public static string SettingsFilePath
        => Path.Combine(
            Environment.GetFolderPath
            (Environment.SpecialFolder.ApplicationData),
            "YeeMusic", "settings.json"
        );

        // 加载方法，现在返回单例实例
        public static AppSettings Load()
        {
            string settingsFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");

            if (_instance != null)
            {
                return _instance;
            }

            if (File.Exists(settingsFilePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(settingsFilePath);
                    var options = new JsonSerializerOptions
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    var settings = JsonSerializer.Deserialize<AppSettings>(jsonString, options);
                    settings._settingsFilePath = settingsFilePath;
                    _instance = settings;

                    Debug.WriteLine($"已加载设置 - 主题: {settings.ThemeSetting}, 材质: {settings.WindowMaterial}, IsFirstRun: {settings.IsFirstRun}, 随机播放: {settings.IsShuffleEnabled}");
                    return settings;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"加载设置时出错: {ex.Message}");
                }
            }

            var newSettings = new AppSettings();
            newSettings._settingsFilePath = settingsFilePath;
            _instance = newSettings;

            Debug.WriteLine("创建新设置 - IsFirstRun: true, 随机播放: false");
            return newSettings;
        }

        // 统一的保存方法
        public void Save()
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string jsonString = JsonSerializer.Serialize(this, options);
                File.WriteAllText(_settingsFilePath, jsonString);

                Debug.WriteLine("设置已保存到: " + _settingsFilePath);
                Debug.WriteLine($"保存的设置 - 主题: {ThemeSetting}, 材质: {WindowMaterial}, IsFirstRun: {IsFirstRun}, 随机播放: {IsShuffleEnabled}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存设置时出错: {ex.Message}");
            }
        }

        // 添加通用的设置获取方法
        public T GetSetting<T>(string key, T defaultValue)
        {
            if (_additionalSettings.TryGetValue(key, out object value))
            {
                try
                {
                    if (value is JsonElement element)
                    {
                        // 处理 JsonElement 类型
                        return (T)Convert.ChangeType(element.GetRawText(), typeof(T));
                    }
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        // 添加通用的设置保存方法
        public void SaveSetting<T>(string key, T value)
        {
            _additionalSettings[key] = value;
            Save(); // 每次更改后保存
        }

        private static string GetSettingsFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = System.IO.Path.Combine(appDataPath, "Yee Music");

            // 确保文件夹存在
            if (!System.IO.Directory.Exists(appFolder))
            {
                System.IO.Directory.CreateDirectory(appFolder);
            }

            return System.IO.Path.Combine(appFolder, "settings.json");
        }
    }
}
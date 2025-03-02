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
        public string LastPlayedMusicPath { get; set; } = "";
        public double LastPlaybackPosition { get; set; } = 0;
        public double Volume { get; set; } = 0.5;
        public string ThemeSetting { get; set; } = "Default";
        public string WindowMaterial { get; set; } = "Mica";
        public bool UseFallbackMaterial { get; set; } = true;
        public string TintColor { get; set; } = "#00000000";
        public bool IsFirstRun { get; set; } = true;
        [JsonConstructor]
        public AppSettings()
        {
            // 保持默认值
            IsFirstRun = true;
            // 其他默认值...
        }
        // 私有构造函数，防止外部创建实例
        private AppSettings(string settingsFilePath)
        {
            _settingsFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");
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
                    var settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
                    settings._settingsFilePath = settingsFilePath;
                    _instance = settings;

                    Debug.WriteLine($"已加载设置 - 主题: {settings.ThemeSetting}, 材质: {settings.WindowMaterial}, IsFirstRun: {settings.IsFirstRun}");
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

            Debug.WriteLine("创建新设置 - IsFirstRun: true");
            return newSettings;
        }

        // 统一的保存方法
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(this, options);
                File.WriteAllText(_settingsFilePath, jsonString);

                Debug.WriteLine("设置已保存到: " + _settingsFilePath);
                Debug.WriteLine($"保存的设置 - 主题: {ThemeSetting}, 材质: {WindowMaterial}, IsFirstRun: {IsFirstRun}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存设置时出错: {ex.Message}");
            }
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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition.SystemBackdrops;
using Yee_Music.Models;
using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace Yee_Music.Services
{
    public class ThemeService
    {
        private static ThemeService _instance;
        private AppSettings _settings;
        private Window _mainWindow;
        private bool _isInitialized = false;

        public static ThemeService Instance => _instance ??= new ThemeService();

        private ThemeService()
        {
            // 使用单例实例
            _settings = AppSettings.Instance;
        }

        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;

            // 加载设置
            _settings = AppSettings.Load();

            System.Diagnostics.Debug.WriteLine($"ThemeService 初始化: 加载设置 - 主题: {_settings.ThemeSetting}, 材质: {_settings.WindowMaterial}");

            // 应用保存的设置
            ApplyThemeInternal(_settings.ThemeSetting);
            ApplyWindowMaterialInternal(_settings.WindowMaterial);

            _isInitialized = true;
        }
        // 添加内部方法，不保存设置
        private void ApplyThemeInternal(string themeSetting)
        {
            ElementTheme theme = ElementTheme.Default;

            switch (themeSetting)
            {
                case "Light":
                    theme = ElementTheme.Light;
                    break;
                case "Dark":
                    theme = ElementTheme.Dark;
                    break;
                case "Default":
                default:
                    theme = ElementTheme.Default;
                    break;
            }

            // 应用主题到根元素
            if (_mainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }
        private void ApplyWindowMaterialInternal(string materialType)
        {
            if (_mainWindow == null)
                return;

            switch (materialType)
            {
                case "Mica":
                    _mainWindow.SystemBackdrop = new MicaBackdrop();
                    break;
                case "MicaAlt":
                    var micaAlt = new MicaBackdrop();
                    micaAlt.Kind = MicaKind.BaseAlt;
                    _mainWindow.SystemBackdrop = micaAlt;
                    break;
                case "Acrylic":
                    _mainWindow.SystemBackdrop = new DesktopAcrylicBackdrop();
                    break;
                case "None":
                    _mainWindow.SystemBackdrop = null;
                    break;
                default:
                    // 默认使用 Mica
                    _mainWindow.SystemBackdrop = new MicaBackdrop();
                    break;
            }
        }

        // 确保在应用主题和材质时正确保存设置
        public void ApplyTheme(string themeSetting)
        {
            ElementTheme theme = ElementTheme.Default;

            switch (themeSetting)
            {
                case "Light":
                    theme = ElementTheme.Light;
                    break;
                case "Dark":
                    theme = ElementTheme.Dark;
                    break;
                case "Default":
                default:
                    theme = ElementTheme.Default;
                    themeSetting = "Default";
                    break;
            }

            // 应用主题到根元素
            if (_mainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
                UpdateTitleBarButtonColors();
            }

            // 保存设置
            _settings.ThemeSetting = themeSetting;
            _settings.Save();

            System.Diagnostics.Debug.WriteLine($"ThemeService: 已应用主题 {themeSetting} 并保存设置");
        }

        public void ApplyWindowMaterial(string materialType)
        {
            if (_mainWindow == null)
                return;

            switch (materialType)
            {
                case "Mica":
                    _mainWindow.SystemBackdrop = new MicaBackdrop();
                    break;
                case "MicaAlt":
                    var micaAlt = new MicaBackdrop();
                    micaAlt.Kind = MicaKind.BaseAlt;
                    _mainWindow.SystemBackdrop = micaAlt;
                    break;
                case "Acrylic":
                    _mainWindow.SystemBackdrop = new DesktopAcrylicBackdrop();
                    break;
                case "None":
                    _mainWindow.SystemBackdrop = null;
                    break;
                default:
                    // 默认使用 Mica
                    _mainWindow.SystemBackdrop = new MicaBackdrop();
                    materialType = "Mica"; // 确保保存正确的值
                    break;
            }

            // 保存设置
            _settings.WindowMaterial = materialType;
            _settings.Save();

            System.Diagnostics.Debug.WriteLine($"ThemeService: 已应用窗口材质 {materialType} 并保存设置");
        }
        public void SaveCurrentSettings()
        {
            if (!_isInitialized)
                return;

            System.Diagnostics.Debug.WriteLine($"ThemeService: 保存当前个性化设置 - 主题: {_settings.ThemeSetting}, 材质: {_settings.WindowMaterial}");

            // 使用单例实例而不是创建新实例
            var settings = AppSettings.Instance;

            // 更新设置
            settings.ThemeSetting = _settings.ThemeSetting;
            settings.WindowMaterial = _settings.WindowMaterial;
            settings.UseFallbackMaterial = _settings.UseFallbackMaterial;
            settings.TintColor = _settings.TintColor;

            // 保存设置
            settings.Save();

            System.Diagnostics.Debug.WriteLine("已保存个性化设置");
        }


        public string GetCurrentTheme()
        {
            return _settings.ThemeSetting;
        }

        public string GetCurrentMaterial()
        {
            return _settings.WindowMaterial;
        }
        public void LoadSettings(AppSettings settings)
        {
            try
            {
                // 更新当前设置对象
                _settings = settings ?? AppSettings.Load();

                System.Diagnostics.Debug.WriteLine($"ThemeService已加载设置 - 主题: {_settings.ThemeSetting}, 材质: {_settings.WindowMaterial}");

                // 如果窗口已初始化，则应用设置
                if (_isInitialized && _mainWindow != null)
                {
                    ApplyThemeInternal(_settings.ThemeSetting);
                    ApplyWindowMaterialInternal(_settings.WindowMaterial);
                    System.Diagnostics.Debug.WriteLine("已应用加载的主题和材质设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载主题设置时出错: {ex.Message}");
            }
        }
        // 添加获取备用材质设置的方法
        public bool GetUseFallbackMaterial()
        {
            return _settings.UseFallbackMaterial;
        }

        // 添加获取颜色设置的方法
        public string GetTintColor()
        {
            return _settings.TintColor;
        }
        private void UpdateTitleBarButtonColors()
        {
            // 获取当前应用主题
            var currentTheme = Application.Current.RequestedTheme;

            // 获取AppWindow
            var appWindow = App.MainWindow.AppWindow;

            // 更新标题栏按钮颜色
            if (appWindow != null)
            {
                // 重新应用自定义设置
                if (currentTheme == ApplicationTheme.Dark)
                {
                    // 深色主题设置
                    App.MainWindow.AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
                    App.MainWindow.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                }
                else
                {
                    // 浅色主题设置
                    App.MainWindow.AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
                    App.MainWindow.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                }
            }
        }
    }
}

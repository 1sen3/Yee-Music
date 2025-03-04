using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Yee_Music.Pages;
using WinUIEx;
using WinRT.Interop;
using DevWinUI;
using Yee_Music.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Windows.Storage.Pickers;
using Yee_Music.ViewModels;
using Microsoft.UI.Dispatching;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music
{
    public sealed partial class MainWindow : Window
    {

        private readonly UISettings settings;
        public IntPtr HWND { get; private set; }
        private bool _isFirstRun;
        private ContentDialog _welcomeDialog;
        private string _selectedMusicLibraryPath;
        private int _welcomePageIndex = 0;
        public MainWindow()
        {
            this.InitializeComponent();

            string iconpath = GetAssetsPath("Icon.ico");
            AppWindow.SetIcon(iconpath);
            this.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;

            settings = new UISettings();
       
            ShellFrame.Content = new ShellPage();
            PlayerBarFrame.Content = new PlayerBarPage();
            HWND = WindowNative.GetWindowHandle(this);

            // 加载保存的设置并应用
            LoadAndApplySettings();

            // 初始化ViewModel
            MainWindowViewModel.Instance.Initialize(this, HWND);

            // 如果需要显示欢迎对话框，延迟执行
            if (MainWindowViewModel.Instance.ShouldShowWelcomeDialog())
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
                {
                    // 给窗口一些时间完成初始化
                    await Task.Delay(500);
                    await MainWindowViewModel.Instance.ShowWelcomeDialogAsync(this.Content.XamlRoot);
                });
            }
        }
        public static string GetAssetsPath(string fileName)
        {
            // 获取当前应用的包
            Package package = Package.Current;
            // 获取资源图片的Uri
            Uri uri = new Uri("ms-appx:///Assets/" + fileName);
            // 将Uri转换为StorageFile
            StorageFile file = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().Result;
            // 获取文件的完整路径
            return file.Path;
        }
        private void LoadAndApplySettings()
        {
            try
            {
                // 加载设置
                var settings = AppSettings.Load();

                // 应用主题设置
                ApplyTheme(settings.ThemeSetting);

                // 应用窗口材质设置
                ApplyWindowMaterial(settings.WindowMaterial);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置时出错: {ex.Message}");
            }
        }
        private void ApplyTheme(string themeSetting)
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
            if (Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }

        private void ApplyWindowMaterial(string materialType)
        {
            switch (materialType)
            {
                case "Mica":
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                    break;
                case "MicaAlt":
                    var micaAlt = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                    micaAlt.Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt;
                    this.SystemBackdrop = micaAlt;
                    break;
                case "Acrylic":
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                    break;
                case "None":
                    this.SystemBackdrop = null;
                    break;
                default:
                    // 默认使用 Mica
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                    break;
            }
        }
    }
}

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
using Microsoft.VisualBasic.Logging;

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
        public MainWindowViewModel ViewModel { get; } = MainWindowViewModel.Instance;
        public MainWindow()
        {
            try
            {
                this.InitializeComponent();

                // 设置窗口图标和标题栏
                string iconpath = GetAssetsPath("Icon.ico");
                AppWindow.SetIcon(iconpath);

                this.ExtendsContentIntoTitleBar = true;
                AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
                AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;

                // 获取窗口句柄 - 这应该尽早完成
                HWND = WindowNative.GetWindowHandle(this);

                // 限制窗口最小尺寸
                AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1366, Height = 768 });
                AppWindow.Changed += AppWindow_Changed;

                // 初始化ViewModel
                ViewModel.Initialize(this, HWND);

                // 加载设置
                LoadAndApplySettings();


                // 初始化UI设置
                settings = new UISettings();

                //// 使用简单的占位内容
                //ShellFrame.Content = new ShellPage();
                //PlayerBarFrame.Content = new PlayerBarPage();

                // 初始化WelcomeControl并传递窗口句柄
                WelcomeControl.Initialize(HWND);

                // 订阅欢迎控件事件
                WelcomeControl.SetupCompleted += (sender, path) => ViewModel.OnWelcomeCompleted(path);
                WelcomeControl.SetupSkipped += (sender, args) => ViewModel.OnWelcomeSkipped();

                // 如果不是首次运行，直接加载主内容
                if (!ViewModel.ShouldShowWelcomeContent())
                {
                    LoadMainContent();
                }
                else
                {
                    // 首次运行，开始欢迎流程
                    ViewModel.StartWelcomeProcess();
                }

                WelcomeControl.SetupCompleted -= WelcomeControl_SetupCompleted;
                WelcomeControl.SetupCompleted += WelcomeControl_SetupCompleted;

                System.Diagnostics.Debug.WriteLine("MainWindow: 已订阅WelcomeControl.SetupCompleted事件");


                // 订阅欢迎完成事件，加载主内容
                ViewModel.WelcomeCompleted += (sender, path) => LoadMainContent();



                System.Diagnostics.Debug.WriteLine("MainWindow 构造函数执行完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow 初始化时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        private void LoadMainContent()
        {
            // 加载ShellPage
            ShellFrame.Navigate(typeof(ShellPage));

            // 加载PlayerBarPage
            PlayerBarFrame.Navigate(typeof(PlayerBarPage));
        }
        private void WelcomeControl_SetupCompleted(object sender, string path)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow: WelcomeControl_SetupCompleted被触发，路径: {path}");

            // 确保在UI线程上执行
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // 隐藏欢迎控件
                    WelcomeControl.Visibility = Visibility.Collapsed;

                    // 显示主内容
                    MainContentGrid.Visibility = Visibility.Visible;

                    System.Diagnostics.Debug.WriteLine("MainWindow: 已切换到主界面");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow: 切换到主界面时出错: {ex.Message}");
                }
            });
        }
        private static void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            try
            {
                if (sender == null)
                    return;

                if (sender.Size.Height < 768 && sender.Size.Width < 1366)
                    sender.Resize(new Windows.Graphics.SizeInt32 { Width = 1366, Height = 768 });
                else if (sender.Size.Height < 768)
                    sender.Resize(new Windows.Graphics.SizeInt32 { Width = sender.Size.Width, Height = 768 });
                else if (sender.Size.Width < 1366)
                    sender.Resize(new Windows.Graphics.SizeInt32 { Width = 1366, Height = sender.Size.Height });

            }
            catch (Exception ex)
            {
                
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

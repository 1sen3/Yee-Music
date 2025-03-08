using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Yee_Music.Models;
using Windows.ApplicationModel.Store;
using Yee_Music.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Yee_Music.Services;
using Windows.System;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using CommunityToolkit.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {

        // 添加 ThemeService 的静态属性
        public static ThemeService ThemeService { get; private set; }
        public static IServiceProvider Services { get; private set; }
        public static MusicPlayer MusicPlayer => Services.GetService<MusicPlayer>();

        public static Microsoft.UI.Xaml.Window MainWindow { get; private set; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            Services = ConfigureServices();
            // 初始化 ThemeService
            ThemeService = ThemeService.Instance;

            // 添加未处理异常处理器
            this.UnhandledException += App_UnhandledException;

        }

        // 在App类中添加ThemeChanged事件
        public event EventHandler<Windows.UI.Color> ThemeChanged;

        // 添加触发ThemeChanged事件的方法
        internal void OnThemeChanged(Windows.UI.Color accentColor)
        {
            ThemeChanged?.Invoke(this, accentColor);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // 先加载必要的设置
                AppSettings settings = AppSettings.Load();
                System.Diagnostics.Debug.WriteLine($"应用启动时读取的主题设置: {settings.ThemeSetting}");
                System.Diagnostics.Debug.WriteLine($"应用启动时读取的窗口材质设置: {settings.WindowMaterial}");

                // 创建主窗口
                MainWindow = new MainWindow();
                if (MainWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误：MainWindow 创建失败");
                    return;
                }

                // 在窗口创建后初始化 ThemeService
                ThemeService.Initialize(MainWindow);

                // 注册窗口关闭事件
                MainWindow.Closed += Window_Closed;

                // 激活窗口 - 这是显示窗口的关键步骤
                MainWindow.Activate();
                System.Diagnostics.Debug.WriteLine("窗口已激活");

                // 所有其他初始化操作移到后台任务中，但不要阻塞UI线程
                DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        // 确保数据库服务已初始化
                        var databaseService = Services.GetService<DatabaseService>();
                        var playQueueService = PlayQueueService.Instance;
                        await playQueueService.InitializeAsync();

                        // 注册内存压力事件
                        MemoryManager.AppMemoryUsageLimitChanging += MemoryManager_AppMemoryUsageLimitChanging;
                        MemoryManager.AppMemoryUsageIncreased += MemoryManager_AppMemoryUsageIncreased;

                        await CoverImageCacheService.InitializeAsync();

                        System.Diagnostics.Debug.WriteLine("SMTC 服务已创建，等待第一次播放时初始化");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"后台初始化过程中出错: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用启动过程中出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                // 先获取当前的个性化设置
                string currentTheme = ThemeService.GetCurrentTheme();
                string currentMaterial = ThemeService.GetCurrentMaterial();

                System.Diagnostics.Debug.WriteLine($"应用关闭前的个性化设置 - 主题: {currentTheme}, 材质: {currentMaterial}");

                // 保存播放器状态
                MusicPlayer?.Shutdown();

                // 使用单例实例
                var settings = AppSettings.Instance;

                // 更新设置
                settings.MusicLibraryPaths = LibraryViewModel.Instance.MusicLibraryPaths.ToList();
                settings.PlayMode = MusicPlayer?.PlayMode.ToString() ?? settings.PlayMode;
                settings.LastPlayedMusicPath = MusicPlayer?.CurrentMusic?.FilePath ?? settings.LastPlayedMusicPath;
                settings.LastPlaybackPosition = MusicPlayer?.Position ?? settings.LastPlaybackPosition;
                settings.Volume = MusicPlayer?.Volume ?? settings.Volume;

                // 确保使用当前的个性化设置
                settings.ThemeSetting = currentTheme;
                settings.WindowMaterial = currentMaterial;
                settings.UseFallbackMaterial = ThemeService.GetUseFallbackMaterial();
                settings.TintColor = ThemeService.GetTintColor();

                // 保存设置
                settings.Save();

                System.Diagnostics.Debug.WriteLine($"应用关闭，已保存所有设置 - 主题: {settings.ThemeSetting}, 材质: {settings.WindowMaterial}, IsFirstRun: {settings.IsFirstRun}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭窗口时出错: {ex.Message}");
            }
        }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 注册数据库服务
            services.AddSingleton<DatabaseService>();

            // 注册服务
            services.AddSingleton<LibraryViewModel>();
            services.AddSingleton<MusicPlayer>();
            services.AddSingleton<AppSettings>();
            services.AddSingleton<PlayerBarViewModel>();
            services.AddSingleton<ThemeService>(ThemeService.Instance);
            services.AddSingleton<PlayQueueViewModel>();
            services.AddSingleton<FavoriteSongsViewModel>();


            return services.BuildServiceProvider();
        }
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // 记录异常详细信息
            string errorMessage = $"发生未处理异常:\n" +
                $"Message: {e.Message}\n" +
                $"Exception: {e.Exception}\n" +
                $"CallStack: {e.Exception?.StackTrace}";

            // 写入日志文件
            try
            {
                // 确定日志文件夹路径
                string logFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "YeeMusicLogs"
                );

                // 确保日志文件夹存在
                System.IO.Directory.CreateDirectory(logFolder);

                // 创建日志文件路径
                string logPath = System.IO.Path.Combine(
                    logFolder,
                    "crash_log.txt"
                );

                // 添加时间戳
                string logEntry = $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{errorMessage}\n";

                // 追加到日志文件
                System.IO.File.AppendAllText(logPath, logEntry);

                Debug.WriteLine($"日志已写入: {logPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"写入日志文件时出错: {ex.Message}");
            }

            // 输出到调试窗口
            Debug.WriteLine(errorMessage);

            // 标记异常已处理，防止应用崩溃
            e.Handled = true;
        }
        private void MemoryManager_AppMemoryUsageIncreased(object sender, object e)
        {
            // 当内存使用增加时，尝试清理一些缓存
            if (MemoryManager.AppMemoryUsage > 512 * 1024 * 1024) // 超过512MB时
            {
                CacheService.Instance.TrimMemory();
            }
        }
        private void MemoryManager_AppMemoryUsageLimitChanging(object sender, AppMemoryUsageLimitChangingEventArgs e)
        {
            // 当系统限制应用内存使用时，进行更激进的内存清理
            CacheService.Instance.Clear();
        }
    }
}

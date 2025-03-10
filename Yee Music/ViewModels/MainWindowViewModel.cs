using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Text;
using WinRT.Interop;
using Yee_Music.Controls;
using Yee_Music.Models;

namespace Yee_Music.ViewModels
{
    public class MainWindowViewModel
    {
        private static MainWindowViewModel _instance;
        public static MainWindowViewModel Instance => _instance ??= new MainWindowViewModel();

        private bool _isFirstRun;
        private string _selectedMusicLibraryPath;
        private int _welcomePageIndex = 0;
        private Window _mainWindow;
        private IntPtr _hwnd;

        private bool _isShowingWelcome;
        public bool IsShowingWelcome
        {
            get => _isShowingWelcome;
            private set
            {
                _isShowingWelcome = value;
                IsShowingMainContent = !value;
                WelcomeVisibilityChanged?.Invoke(this, value);
            }
        }

        private bool _isShowingMainContent = true;
        public bool IsShowingMainContent
        {
            get => _isShowingMainContent;
            private set
            {
                _isShowingMainContent = value;
                MainContentVisibilityChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<bool> WelcomeVisibilityChanged;
        public event EventHandler<bool> MainContentVisibilityChanged;
        public event EventHandler<string> WelcomeCompleted;

        private MainWindowViewModel()
        {
            var appSettings = AppSettings.Load();
            _isFirstRun = appSettings.IsFirstRun;
            IsShowingWelcome = _isFirstRun;
            IsShowingMainContent = !_isFirstRun;
            Debug.WriteLine($"MainWindowViewModel初始化，IsFirstRun: {_isFirstRun}");
        }

        public void Initialize(Window mainWindow, IntPtr hwnd)
        {
            _mainWindow = mainWindow;
            _hwnd = hwnd;
        }

        public bool ShouldShowWelcomeContent()
        {
            return _isFirstRun;
        }

        public void StartWelcomeProcess()
        {
            if (_isFirstRun)
            {
                IsShowingWelcome = true;
            }
        }
        public void OnWelcomeCompleted(string musicLibraryPath)
        {
            _selectedMusicLibraryPath = musicLibraryPath;
            FinishWelcomeProcess();

            // 切换到主内容
            IsShowingWelcome = false;

            // 通知欢迎流程完成
            WelcomeCompleted?.Invoke(this, musicLibraryPath);
        }

        // 新方法：处理欢迎流程跳过
        public void OnWelcomeSkipped()
        {
            FinishWelcomeProcess();

            // 切换到主内容
            IsShowingWelcome = false;

            // 通知欢迎流程完成
            WelcomeCompleted?.Invoke(this, null);
        }

        // 获取HWND，供WelcomeControl使用
        public IntPtr GetHwnd()
        {
            return _hwnd;
        }

        private void FinishWelcomeProcess()
        {
            try
            {
                // 使用单例实例
                var settings = AppSettings.Instance;
                settings.IsFirstRun = false;
                settings.Save();

                Debug.WriteLine("已将IsFirstRun设置为false并保存");

                // 如果选择了音乐库路径，添加到音乐库
                if (!string.IsNullOrEmpty(_selectedMusicLibraryPath))
                {
                    // 添加音乐库路径
                    LibraryViewModel.Instance.AddMusicLibraryPathWithoutPicker(_selectedMusicLibraryPath);
                    Debug.WriteLine($"已添加音乐库路径: {_selectedMusicLibraryPath}");
                }

                // 更新本地变量
                _isFirstRun = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"完成欢迎流程时出错: {ex.Message}");
            }
        }
    }
}

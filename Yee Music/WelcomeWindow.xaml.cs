using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Yee_Music.ViewModels;
using Windows.Storage.Pickers;
using Yee_Music.Models;
using System.Diagnostics;
using Windows.ApplicationModel.UserDataTasks;
using Yee_Music.Services;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WelcomeWindow : Window
    {
        private ThemeService _themeService = App.ThemeService;
        private string _selectedMusicLibraryPath;
        private int _currentPageIndex = 0;
        private readonly LibraryViewModel _libraryViewModel;
        public WelcomeWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;

            // 设置窗口图标
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon("Assets/Square44x44Logo.scale-200.png");

            appWindow.MoveAndResize(new RectInt32(_X: 560, _Y: 280, _Width: 800, _Height: 600));

            // 获取LibraryViewModel实例
            _libraryViewModel = LibraryViewModel.Instance;

            // 初始化UI状态
            UpdateButtonsState();
        }
        private void UpdateButtonsState()
        {
            _currentPageIndex = WelcomeFlipView.SelectedIndex;

            // 更新按钮状态
            PreviousButton.Visibility = _currentPageIndex > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_currentPageIndex == 0)
            {
                NextButton.Content = "下一步";
                SkipButton.Visibility = Visibility.Visible;
            }
            else if (_currentPageIndex == 1)
            {
                NextButton.Content = "下一步";
                SkipButton.Visibility = Visibility.Visible;
            }
            else if (_currentPageIndex == 2)
            {
                NextButton.Content = "开始使用";
                SkipButton.Visibility = Visibility.Collapsed;
            }
        }

        private void WelcomeFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsState();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            folderPicker.FileTypeFilter.Add("*");

            // 初始化文件选择器
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                _selectedMusicLibraryPath = folder.Path;
                MusicLibraryPathTextBox.Text = _selectedMusicLibraryPath;
                LibraryPathErrorText.Visibility = Visibility.Collapsed;
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (WelcomeFlipView.SelectedIndex > 0)
            {
                WelcomeFlipView.SelectedIndex--;
                UpdateButtonsState();
            }
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (WelcomeFlipView.SelectedIndex < WelcomeFlipView.Items.Count - 1)
            {
                // 如果当前是第一页，直接进入第二页
                if (WelcomeFlipView.SelectedIndex == 0)
                {
                    WelcomeFlipView.SelectedIndex++;
                }
                // 如果当前是第二页，验证是否选择了音乐库路径
                else if (WelcomeFlipView.SelectedIndex == 1)
                {
                    if (string.IsNullOrEmpty(_selectedMusicLibraryPath))
                    {
                        LibraryPathErrorText.Visibility = Visibility.Visible;
                        return;
                    }

                    // 修改这里：直接保存路径，不要执行命令
                    // 只记录路径，在完成设置时再添加到音乐库

                    // 进入下一页
                    WelcomeFlipView.SelectedIndex++;
                }
            }
            else
            {
                // 如果是最后一页，完成设置并启动主应用
                FinishSetupAndLaunchApp();
            }

            UpdateButtonsState();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            // 跳过设置，直接启动主应用
            FinishSetupAndLaunchApp();
        }

        private void FinishSetupAndLaunchApp()
        {
            try
            {
                // 保存首次启动标志
                AppSettings settings = AppSettings.Load();
                settings.IsFirstRun = false;
                settings.Save();

                Debug.WriteLine("已将IsFirstRun设置为false并保存");

                // 如果选择了音乐库路径，添加到音乐库
                if (!string.IsNullOrEmpty(_selectedMusicLibraryPath))
                {
                    // 添加音乐库路径
                    _libraryViewModel.AddMusicLibraryPathWithoutPicker(_selectedMusicLibraryPath);
                }

                // 创建主窗口
                var mainWindow = new MainWindow();
                mainWindow.Activate();

                // 关闭欢迎窗口
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动主窗口时出错: {ex.Message}");
            }
        }
    }
}

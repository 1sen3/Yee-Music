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
using DevWinUI;
using System.Collections.Specialized;
using Yee_Music.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using System.Text.Json;
using Microsoft.UI;
using CommunityToolkit.Mvvm.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibrarySettingsPage : Page
    {
        private AppSettings _settings;
        private List<string> _musicLibraryPaths = new List<string>();
        public LibraryViewModel ViewModel => LibraryViewModel.Instance;
        public LibrarySettingsPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;

            // 如果需要，可以在这里添加日志
            Debug.WriteLine($"LibrarySettingsPage初始化，当前音乐库路径数量: {ViewModel.MusicLibraryPaths.Count}");
        }

        private async void AddMusicLibrary_Click(object sender, RoutedEventArgs e)
        {
            // 使用ViewModel中的命令来添加音乐库
            if (ViewModel.AddMusicLibraryCommand.CanExecute(null))
            {
                await ViewModel.AddMusicLibraryCommand.ExecuteAsync(null);
            }
        }

        private async void RemoveMusicLibraryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string path)
            {
                // 使用ViewModel中的命令来删除音乐库
                if (ViewModel.RemoveMusicLibraryCommand.CanExecute(path))
                {
                    ViewModel.RemoveMusicLibraryCommand.Execute(path);
                }
            }
        }
        private async void DebugWelcomeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建一个MainWindowViewModel实例并显示欢迎对话框
                var viewModel = MainWindowViewModel.Instance;

                // 传递当前页面的XamlRoot
                await viewModel.ShowWelcomeDialogAsync(this.XamlRoot);

                // 添加调试输出
                System.Diagnostics.Debug.WriteLine("已尝试显示欢迎对话框");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示欢迎对话框时出错: {ex.Message}");

                // 可以显示一个错误提示
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "错误",
                    Content = $"无法显示欢迎对话框: {ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();
            }
        }
        private async void FreshLibraryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 显示加载指示器
                LoadingIndicator.IsActive = true;
                RefreshButton.IsEnabled = false;

                // 获取ViewModel实例
                var viewModel = ViewModel;

                // 检查命令类型并调用相应的方法
                if (viewModel.RefreshMusicListCommand is IAsyncRelayCommand asyncCommand)
                {
                    // 如果是异步命令，使用ExecuteAsync
                    await asyncCommand.ExecuteAsync(null);
                }
                else
                {
                    // 如果是普通命令，直接执行
                    viewModel.RefreshMusicListCommand.Execute(null);
                }

                // 显示成功消息
                var dialog = new ContentDialog
                {
                    Title = "刷新完成",
                    Content = "音乐库已成功刷新",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                // 显示错误消息
                var dialog = new ContentDialog
                {
                    Title = "刷新失败",
                    Content = $"刷新音乐库时出错：{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            finally
            {
                // 无论成功还是失败，都需要关闭加载指示器
                LoadingIndicator.IsActive = false;
                RefreshButton.IsEnabled = true;
            }
        }
    }
}

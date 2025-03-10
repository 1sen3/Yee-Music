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
using Microsoft.UI.Xaml.Media.Imaging;

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
        private void DebugWelcomeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取 MainWindowViewModel 实例
                var viewModel = MainWindowViewModel.Instance;

                // 重置首次运行状态（仅用于调试）
                var settings = AppSettings.Instance;
                settings.IsFirstRun = true;
                settings.Save();

                // 显示确认对话框
                ContentDialog confirmDialog = new ContentDialog
                {
                    Title = "调试欢迎界面",
                    Content = "应用将重新启动以显示欢迎界面。确定继续吗？",
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };

                confirmDialog.PrimaryButtonClick += async (s, args) =>
                {
                    // 重启应用
                    var currentExecutable = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(currentExecutable);
                    Application.Current.Exit();
                };

                // 显示对话框
                confirmDialog.ShowAsync();

                // 添加调试输出
                System.Diagnostics.Debug.WriteLine("已尝试重置欢迎界面状态");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重置欢迎界面状态时出错: {ex.Message}");

                // 显示错误提示
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "错误",
                    Content = $"无法重置欢迎界面状态: {ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };

                errorDialog.ShowAsync();
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
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 加载用户信息
            await ViewModel.LoadUserInfoAsync();
        }
        private async void EditUserInfo_Click(object sender, RoutedEventArgs e)
        {
            // 创建编辑用户信息对话框
            ContentDialog dialog = new ContentDialog
            {
                Title = "编辑个人信息",
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // 创建编辑用户信息的内容
            var editPanel = new StackPanel { Spacing = 16 };

            // 添加头像选择器
            var avatarGrid = new Grid();
            var personPicture = new PersonPicture
            {
                ProfilePicture = ViewModel.UserAvatarSource,
                Width = 100,
                Height = 100,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var avatarButton = new Button
            {
                Content = new FontIcon { Glyph = "\uE114" },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, -60, 0)
            };

            avatarGrid.Children.Add(personPicture);
            avatarGrid.Children.Add(avatarButton);
            editPanel.Children.Add(avatarGrid);

            // 添加用户名输入框
            var nameBox = new Microsoft.UI.Xaml.Controls.TextBox
            {
                Header = "用户名",
                Text = ViewModel.UserName,
                PlaceholderText = "请输入用户名"
            };
            editPanel.Children.Add(nameBox);

            // 设置对话框内容
            dialog.Content = editPanel;

            // 处理头像选择
            avatarButton.Click += async (s, args) =>
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                // WinUI 3需要初始化Picker的窗口句柄
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    using var stream = await file.OpenReadAsync();
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    personPicture.ProfilePicture = bitmap;

                    // 保存文件路径以便后续保存
                    personPicture.Tag = file.Path;
                }
            };

            // 显示对话框并处理结果
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 保存用户名
                string newUserName = nameBox.Text;
                if (!string.IsNullOrWhiteSpace(newUserName))
                {
                    ViewModel.UserName = newUserName;
                    AppSettings.Instance.UserName = newUserName;
                }

                // 保存头像
                if (personPicture.Tag is string avatarPath)
                {
                    AppSettings.Instance.UserAvatarPath = avatarPath;
                    // 更新ViewModel中的头像
                    await ViewModel.LoadUserInfoAsync();
                }

                // 保存设置
                AppSettings.Instance.Save();
            }
        }
    }
}

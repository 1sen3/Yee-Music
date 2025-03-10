using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Yee_Music.Models;
using Yee_Music.Services;

namespace Yee_Music.Controls
{
    public sealed partial class WelcomeControl : UserControl
    {
        private IntPtr _hwnd;
        private string _selectedMusicLibraryPath;
        private string _selectedAvatarPath;
        private string _userName;

        public event EventHandler<string> SetupCompleted;
        public event EventHandler SetupSkipped;

        private readonly DatabaseService _databaseService;
        public WelcomeControl()
        {
            this.InitializeComponent();
            WelcomeFlipView.SelectedIndex = 0;
            UpdateButtonState();

            _databaseService = App.Services?.GetService<DatabaseService>();
        }

        public void Initialize(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        private void WelcomeFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            // 确保控件已完全初始化
            if (WelcomeFlipView == null || NextButton == null || PreviousButton == null)
            {
                Debug.WriteLine("UpdateButtonState: 控件尚未完全初始化");
                return;
            }

            int currentIndex = WelcomeFlipView.SelectedIndex;

            // 更新上一步按钮
            PreviousButton.Visibility = currentIndex > 0 ? Visibility.Visible : Visibility.Collapsed;

            // 更新下一步/完成按钮
            if (currentIndex == 3) // 最后一页
            {
                NextButton.Content = "开始";
            }
            else
            {
                NextButton.Content = "下一步";
            }

            // 根据当前页面启用/禁用下一步按钮
            switch (currentIndex)
            {
                case 1: // 用户信息页
                    // 用户名不能为空
                    NextButton.IsEnabled = !string.IsNullOrWhiteSpace(UserNameTextBox?.Text);
                    break;
                case 2: // 音乐库设置页
                    NextButton.IsEnabled = !string.IsNullOrEmpty(_selectedMusicLibraryPath);
                    break;
                default:
                    NextButton.IsEnabled = true;
                    break;
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查窗口句柄是否有效
                if (_hwnd == IntPtr.Zero)
                {
                    Debug.WriteLine("错误：窗口句柄为空");
                    return;
                }

                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                folderPicker.FileTypeFilter.Add("*");

                // 初始化文件选择器
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, _hwnd);
                Debug.WriteLine($"使用窗口句柄初始化文件选择器: {_hwnd}");

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    _selectedMusicLibraryPath = folder.Path;
                    PathTextBox.Text = _selectedMusicLibraryPath;
                    ErrorText.Visibility = Visibility.Collapsed;

                    // 启用下一步按钮
                    NextButton.IsEnabled = true;
                    Debug.WriteLine($"已选择文件夹: {_selectedMusicLibraryPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"选择文件夹时出错: {ex.Message}");
                ErrorText.Text = "无法打开文件选择器，请重试";
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = WelcomeFlipView.SelectedIndex;

            // 保存用户信息
            if (currentIndex == 1) // 用户信息页面
            {
                _userName = UserNameTextBox.Text;
            }

            if (currentIndex == 2 && string.IsNullOrEmpty(_selectedMusicLibraryPath))
            {
                // 如果是音乐库页面且没有选择路径，显示错误
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (currentIndex < 3)
            {
                // 前进到下一页
                WelcomeFlipView.SelectedIndex++;
            }
            else
            {
                // 最后一页，点击"开始使用"按钮
                try
                {
                    // 保存用户设置
                    SaveUserSettings();

                    // 添加调试输出
                    System.Diagnostics.Debug.WriteLine($"WelcomeControl: 触发SetupCompleted事件，音乐库路径: {_selectedMusicLibraryPath}");

                    // 确保事件被触发，即使音乐库路径为空
                    string path = _selectedMusicLibraryPath ?? string.Empty;
                    SetupCompleted?.Invoke(this, path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WelcomeControl: 触发SetupCompleted事件时出错: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                }
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (WelcomeFlipView.SelectedIndex > 0)
            {
                WelcomeFlipView.SelectedIndex--;
            }
        }
        private async void SelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查窗口句柄是否有效
                if (_hwnd == IntPtr.Zero)
                {
                    Debug.WriteLine("错误：窗口句柄为空");
                    return;
                }

                var filePicker = new FileOpenPicker();
                filePicker.ViewMode = PickerViewMode.Thumbnail;
                filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                filePicker.FileTypeFilter.Add(".jpg");
                filePicker.FileTypeFilter.Add(".jpeg");
                filePicker.FileTypeFilter.Add(".png");
                filePicker.FileTypeFilter.Add(".bmp");

                // 初始化文件选择器
                WinRT.Interop.InitializeWithWindow.Initialize(filePicker, _hwnd);

                var file = await filePicker.PickSingleFileAsync();
                if (file != null)
                {
                    // 保存头像到应用数据文件夹
                    var localFolder = ApplicationData.Current.LocalFolder;
                    var avatarsFolder = await localFolder.CreateFolderAsync("Avatars", CreationCollisionOption.OpenIfExists);

                    var destinationFile = await file.CopyAsync(avatarsFolder, "userAvatar.jpg", NameCollisionOption.ReplaceExisting);
                    _selectedAvatarPath = destinationFile.Path;

                    // 更新头像显示
                    var stream = await file.OpenAsync(FileAccessMode.Read);
                    var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);

                    UserAvatar.ProfilePicture = bitmapImage;

                    Debug.WriteLine($"已选择头像: {_selectedAvatarPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"选择头像时出错: {ex.Message}");
            }
        }
        private void SaveUserSettings()
        {
            try
            {
                var settings = AppSettings.Instance;

                // 保存用户名
                if (!string.IsNullOrWhiteSpace(_userName))
                {
                    settings.UserName = _userName;
                }

                // 保存头像路径
                if (!string.IsNullOrEmpty(_selectedAvatarPath))
                {
                    settings.UserAvatarPath = _selectedAvatarPath;
                }

                // 保存音乐库路径
                if (!string.IsNullOrEmpty(_selectedMusicLibraryPath) &&
                    !settings.MusicLibraryPaths.Contains(_selectedMusicLibraryPath))
                {
                    settings.MusicLibraryPaths.Add(_selectedMusicLibraryPath);
                    _databaseService.AddMusicLibraryPathAsync(_selectedMusicLibraryPath);
                }

                // 设置为非首次运行
                settings.IsFirstRun = false;

                // 保存设置
                settings.Save();

                Debug.WriteLine($"已保存用户设置 - 用户名: {settings.UserName}, 头像路径: {settings.UserAvatarPath}, 音乐库路径: {_selectedMusicLibraryPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存用户设置时出错: {ex.Message}");
            }
        }

        // 用于TextBox文本变化时更新按钮状态
        private void UserNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 当用户名文本框内容变化时，更新按钮状态
            UpdateButtonState();
        }
    }
}
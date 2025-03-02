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

        private MainWindowViewModel()
        {
            // 检查是否首次运行
            var appSettings = AppSettings.Load();
            _isFirstRun = appSettings.IsFirstRun;
            Debug.WriteLine($"MainWindowViewModel初始化，IsFirstRun: {_isFirstRun}");
        }

        public void Initialize(Window mainWindow, IntPtr hwnd)
        {
            _mainWindow = mainWindow;
            _hwnd = hwnd;
        }

        public bool ShouldShowWelcomeDialog()
        {
            return _isFirstRun;
        }

        public async Task ShowWelcomeDialogAsync(XamlRoot xamlRoot)
        {
            try
            {
                if (!_isFirstRun && xamlRoot == null)
                    return;

                // 创建欢迎对话框
                var welcomeDialog = new ContentDialog
                {
                    XamlRoot = xamlRoot, // 使用传入的XamlRoot
                    Title = "",
                    CloseButtonText = "跳过",
                    PrimaryButtonText = "下一步",
                    DefaultButton = ContentDialogButton.Primary,
                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
                };

                // 设置初始内容
                UpdateWelcomeDialogContent(welcomeDialog);

                // 显示对话框并处理结果
                var result = await welcomeDialog.ShowAsync();

                // 处理对话框结果
                if (result == ContentDialogResult.Primary)
                {
                    // 用户点击了"下一步"或"开始使用"
                    if (_welcomePageIndex < 2)
                    {
                        _welcomePageIndex++;
                        await ShowWelcomeDialogAsync(xamlRoot);
                    }
                    else
                    {
                        // 完成欢迎流程
                        FinishWelcomeProcess();
                    }
                }
                else
                {
                    // 用户点击了"跳过"
                    FinishWelcomeProcess();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示欢迎对话框时出错: {ex.Message}");
            }
        }

        private void UpdateWelcomeDialogContent(ContentDialog dialog)
        {
            switch (_welcomePageIndex)
            {
                case 0:
                    // 第一页：欢迎介绍
                    var welcomePage = new StackPanel
                    {
                        Spacing = 20,
                        Padding = new Thickness(0, 10, 0, 10)
                    };

                    var welcomeIcon = new BitmapIcon
                    {
                        UriSource = new Uri("ms-appx:///Assets/Square44x44Logo.altform-unplated_targetsize-256.png"),
                        ShowAsMonochrome = false,
                        Width = 64,
                        Height = 64,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var welcomeTitle = new TextBlock
                    {
                        Text = "Yee Music",
                        FontSize = 24,
                        FontWeight = FontWeights.ExtraBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 10)
                    };

                    var welcomeText = new TextBlock
                    {
                        Text = "一个简洁、高效的音乐播放器。",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var welcomeText2 = new TextBlock
                    {
                        Text = "开始设置第一个音乐库吧！",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontWeight=FontWeights.Medium,
                        FontSize = 16
                    };

                    welcomePage.Children.Add(welcomeIcon);
                    welcomePage.Children.Add(welcomeTitle);
                    welcomePage.Children.Add(welcomeText);
                    welcomePage.Children.Add(welcomeText2);

                    dialog.Content = welcomePage;
                    dialog.PrimaryButtonText = "下一步";
                    break;

                case 1:
                    // 第二页：设置音乐库
                    var libraryPage = new StackPanel
                    {
                        Spacing = 20,
                        Padding = new Thickness(0, 10, 0, 10)
                    };

                    var libraryIcon = new FontIcon
                    {
                        Glyph = "\uE8B7",
                        FontSize = 64,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush
                    };

                    var libraryTitle = new TextBlock
                    {
                        Text = "设置您的音乐库",
                        FontSize = 24,
                        FontWeight = FontWeights.ExtraBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 10)
                    };

                    var libraryText = new TextBlock
                    {
                        Text = "选择包含您音乐文件的文件夹，Yee Music 将自动扫描并添加您的音乐。",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 20,
                        FontWeight=FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 20)
                    };

                    var pathPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Spacing = 10
                    };

                    var pathTextBox = new TextBox
                    {
                        Width = 400,
                        PlaceholderText = "选择音乐库文件夹路径",
                        IsReadOnly = true
                    };

                    var browseButton = new Button
                    {
                        Content = "浏览"
                    };

                    browseButton.Click += async (s, e) =>
                    {
                        var folderPicker = new FolderPicker();
                        folderPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                        folderPicker.FileTypeFilter.Add("*");

                        // 初始化文件选择器
                        InitializeWithWindow.Initialize(folderPicker, _hwnd);

                        var folder = await folderPicker.PickSingleFolderAsync();
                        if (folder != null)
                        {
                            _selectedMusicLibraryPath = folder.Path;
                            pathTextBox.Text = _selectedMusicLibraryPath;

                            // 启用下一步按钮
                            dialog.IsPrimaryButtonEnabled = true;
                        }
                    };

                    var errorText = new TextBlock
                    {
                        Text = "请选择一个有效的文件夹路径",
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                        Visibility = Visibility.Collapsed,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    pathPanel.Children.Add(pathTextBox);
                    pathPanel.Children.Add(browseButton);

                    libraryPage.Children.Add(libraryIcon);
                    libraryPage.Children.Add(libraryTitle);
                    libraryPage.Children.Add(libraryText);
                    libraryPage.Children.Add(pathPanel);
                    libraryPage.Children.Add(errorText);

                    dialog.Content = libraryPage;
                    dialog.PrimaryButtonText = "下一步";

                    // 禁用下一步按钮，直到选择了路径
                    dialog.IsPrimaryButtonEnabled = !string.IsNullOrEmpty(_selectedMusicLibraryPath);
                    break;

                case 2:
                    // 第三页：完成设置
                    var finishPage = new StackPanel
                    {
                        Spacing = 20,
                        Padding = new Thickness(0, 10, 0, 10)
                    };

                    var finishIcon = new FontIcon
                    {
                        Glyph = "\uE930",
                        FontSize = 64,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush
                    };

                    var finishTitle = new TextBlock
                    {
                        Text = "准备就绪",
                        FontSize = 24,
                        FontWeight = FontWeights.Black,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 10)
                    };

                    var finishText = new TextBlock
                    {
                        Text = "您的音乐库已设置完成，点击开始使用Yee Music吧！",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 20,
                        FontWeight=FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var finishText2 = new TextBlock
                    {
                        Text = "您随时可以在设置中添加或移除音乐库。",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 16,
                        FontWeight=FontWeights.Medium
                    };

                    finishPage.Children.Add(finishIcon);
                    finishPage.Children.Add(finishTitle);
                    finishPage.Children.Add(finishText);
                    finishPage.Children.Add(finishText2);

                    dialog.Content = finishPage;
                    dialog.PrimaryButtonText = "开始使用";
                    dialog.CloseButtonText = null; // 移除跳过按钮
                    break;
            }
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

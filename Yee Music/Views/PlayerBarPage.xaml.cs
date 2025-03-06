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
using Yee_Music.Models;
using Windows.Media.Playback;
using Microsoft.UI.Xaml.Media.Animation;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Yee_Music.ViewModels;
using Yee_Music.Services;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Yee_Music.Controls;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerBarPage : Page
    {
        public PlayerBarViewModel ViewModel { get; }
        public PlayerBarPage()
        {
            this.InitializeComponent();
            // 初始化ViewModel
            ViewModel = new PlayerBarViewModel();
            // 设置DataContext
            this.DataContext = ViewModel;

            // 订阅专辑封面更新事件
            ViewModel.AlbumArtChanged += OnAlbumArtChanged;

            // 初始加载专辑封面
            if (ViewModel.CurrentMusic != null)
            {
                UpdateAlbumArt(ViewModel.CurrentMusic);
            }

            // 注册播放列表变化事件
            ViewModel.PlayQueueChanged += ViewModel_PlayQueueChanged;

            // 初始化播放列表菜单
            InitializePlayQueueMenu();

            this.Unloaded += PlayerBarPage_Unloaded;
        }
        // 添加专辑封面更新事件处理方法
        private void OnAlbumArtChanged(MusicInfo music)
        {
            try
            {
                UpdateAlbumArt(music);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理专辑封面更新事件时出错: {ex.Message}");
                // 确保UI不会因为异常而崩溃
                ControllerCover.Source = null;
            }
        }

        // 添加更新专辑封面的方法
        private async void UpdateAlbumArt(MusicInfo music)
        {
            try
            {
                if (music == null)
                {
                    ControllerCover.Source = null;
                    Debug.WriteLine("清除专辑封面 (music为null)");
                    return;
                }

                // 获取专辑封面数据
                byte[] albumArtData = music.AlbumArt;

                if (albumArtData != null && albumArtData.Length > 0)
                {
                    // 使用 ImageService 优化图片
                    BitmapImage optimizedImage = await ImageService.GetOptimizedBitmapImageAsync(albumArtData, 100, 100);

                    if (optimizedImage != null)
                    {
                        ControllerCover.Source = optimizedImage;
                        Debug.WriteLine("已更新专辑封面");
                    }
                    else
                    {
                        ControllerCover.Source = null;
                        Debug.WriteLine("无法优化专辑封面图片");
                    }
                }
                else
                {
                    ControllerCover.Source = null;
                    Debug.WriteLine("歌曲没有专辑封面数据");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新专辑封面时出错: {ex.Message}");
                ControllerCover.Source = null;
            }
        }
        private void PlayQueueFlyout_Opening(object sender, object e)
        {
            // 重新加载播放队列菜单项
            InitializePlayQueueMenu();
        }
        private void InitializePlayQueueMenu()
        {
            try
            {
                // 清空现有菜单项（保留标题和分隔符）
                while (PlayQueueFlyout.Items.Count > 2)
                {
                    PlayQueueFlyout.Items.RemoveAt(2);
                }

                // 添加播放队列中的歌曲
                if (ViewModel.PlayQueue != null && ViewModel.PlayQueue.Count > 0)
                {
                    foreach (var music in ViewModel.PlayQueue)
                    {
                        var menuItem = CreateMusicMenuItem(music);
                        PlayQueueFlyout.Items.Add(menuItem);
                    }

                    // 隐藏"播放队列为空"菜单项
                    EmptyQueueItem.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }
                else
                {
                    // 显示"播放队列为空"菜单项
                    EmptyQueueItem.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化播放列表菜单出错: {ex.Message}");
            }
        }
        private MenuFlyoutItem CreateMusicMenuItem(MusicInfo music)
        {
            var menuItem = new MenuFlyoutItem
            {
                Text = ViewModel.CurrentMusic != null && ViewModel.CurrentMusic.FilePath == music.FilePath
                    ? $"播放中 {music.Title} - {music.Artist}"
                    : $"{music.Title} - {music.Artist}",
                DataContext = music
            };

            // 设置当前播放歌曲的文本颜色为系统强调色
            if (ViewModel.CurrentMusic != null && ViewModel.CurrentMusic.FilePath == music.FilePath)
            {
                try
                {
                    // 尝试获取系统强调色
                    if (Application.Current.Resources.TryGetValue("SystemAccentColor", out object accentColorObj) &&
                        accentColorObj is Windows.UI.Color accentColor)
                    {
                        menuItem.Foreground = new SolidColorBrush(accentColor);
                    }
                    else if (Application.Current.Resources.TryGetValue("SystemControlHighlightAccentBrush", out object accentBrushObj) &&
                             accentBrushObj is SolidColorBrush accentBrush)
                    {
                        menuItem.Foreground = accentBrush;
                    }
                    else
                    {
                        // 如果无法获取系统强调色，使用默认的强调色
                        menuItem.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                    }
                }
                catch
                {
                    // 出现异常时使用默认的强调色
                    menuItem.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                }
            }

            // 添加点击事件
            menuItem.Click += MusicMenuItem_Click;

            // 添加右键菜单
            menuItem.ContextFlyout = CreateMusicContextMenu(music);

            return menuItem;
        }

        private MenuFlyout CreateMusicContextMenu(MusicInfo music)
        {
            var contextMenu = new MenuFlyout();


            // 从播放列表移除
            var removeItem = new MenuFlyoutItem
            {
                Text = "从播放列表移除",
                Icon = new FontIcon { Glyph = "\uE74D" },
                DataContext = music
            };
            removeItem.Click += RemoveMusicMenuItem_Click;
            contextMenu.Items.Add(removeItem);

            // 分隔符
            contextMenu.Items.Add(new MenuFlyoutSeparator());

            // 属性
            var propertiesItem = new MenuFlyoutItem
            {
                Text = "属性",
                Icon = new FontIcon { Glyph = "\uE946" },
                DataContext = music
            };
            propertiesItem.Click += PropertiesMusicMenuItem_Click;
            contextMenu.Items.Add(propertiesItem);

            return contextMenu;
        }

        private void MusicMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null && ViewModel.PlayMusicCommand != null)
                {
                    ViewModel.PlayMusicCommand.Execute(music);
                }
            }
        }

        private void RemoveMusicMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null && ViewModel.RemoveMusicCommand != null)
                {
                    ViewModel.RemoveMusicCommand.Execute(music);
                }
            }
        }

        private async void PropertiesMusicMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null)
                {
                    var dialog = new MusicPropertiesDialog();
                    dialog.SetMusic(music);
                    dialog.XamlRoot = this.XamlRoot;

                    await dialog.ShowAsync();
                }
            }
        }

        private void ViewModel_PlayQueueChanged(object sender, EventArgs e)
        {
            // 当播放队列变化时，更新菜单
            InitializePlayQueueMenu();
        }

        // 在页面卸载时取消事件订阅
        private void PlayerBarPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PlayQueueChanged -= ViewModel_PlayQueueChanged;
                ViewModel.AlbumArtChanged -= OnAlbumArtChanged;
            }
        }
    }
}

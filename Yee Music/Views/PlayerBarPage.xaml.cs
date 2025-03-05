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
            UpdateAlbumArt(music);
        }

        // 添加更新专辑封面的方法
        private async void UpdateAlbumArt(MusicInfo music)
        {
            if (music == null)
            {
                ControllerCover.Source = null;
                return;
            }

            try
            {
                // 获取专辑封面数据
                byte[] albumArtData = music.AlbumArt;

                if (albumArtData != null && albumArtData.Length > 0)
                {
                    // 使用 ImageService 优化图片
                    BitmapImage optimizedImage = await ImageService.GetOptimizedBitmapImageAsync(albumArtData, 100, 100);

                    if (optimizedImage != null)
                    {
                        ControllerCover.Source = optimizedImage;
                        System.Diagnostics.Debug.WriteLine("已加载优化后的专辑封面");
                    }
                    else
                    {
                        // 如果优化失败，尝试直接加载
                        var bitmap = new BitmapImage();
                        using (var stream = new InMemoryRandomAccessStream())
                        {
                            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                            {
                                writer.WriteBytes(albumArtData);
                                await writer.StoreAsync();
                            }
                            await bitmap.SetSourceAsync(stream);
                        }
                        ControllerCover.Source = bitmap;
                        System.Diagnostics.Debug.WriteLine("已加载原始专辑封面");
                    }
                }
                else
                {
                    // 没有专辑封面，显示默认图片
                    ControllerCover.Source = new BitmapImage(new Uri("ms-appx:///Assets/DefaultAlbumArt.png"));
                    System.Diagnostics.Debug.WriteLine("使用默认专辑封面");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新专辑封面时出错: {ex.Message}");
                // 出错时显示默认图片
                ControllerCover.Source = new BitmapImage(new Uri("ms-appx:///Assets/DefaultAlbumArt.png"));
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
                Text = $"{music.Title} - {music.Artist}",
                DataContext = music
            };

            // 设置当前播放歌曲的图标
            if (ViewModel.CurrentMusic != null && ViewModel.CurrentMusic.FilePath == music.FilePath)
            {
                menuItem.Icon = new FontIcon { Glyph = "\uEE4A", FontSize = 9 };
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

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
    }
}

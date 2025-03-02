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
using System.Collections.ObjectModel;
using Yee_Music.Models;
using Yee_Music.Services;
using Microsoft.Extensions.DependencyInjection;
using Yee_Music.ViewModels;
using Yee_Music.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    public sealed partial class PlayQueuePage : Page
    {

        public PlayQueueViewModel ViewModel { get; }

        public PlayQueuePage()
        {
            this.InitializeComponent();
            // 从 DI 容器获取 ViewModel
            ViewModel = App.Services.GetService<PlayQueueViewModel>() ?? new PlayQueueViewModel(App.MusicPlayer);
            // 设置双向绑定的上下文
            this.DataContext = ViewModel;

        }
        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                // 查找播放按钮和音乐图标
                var playButton = grid.FindName("PlayButton") as Button;
                var musicIcon = grid.FindName("MusicFontIcon") as FontIcon;

                if (playButton != null && musicIcon != null)
                {
                    playButton.Visibility = Visibility.Visible;
                    musicIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                // 查找播放按钮和音乐图标
                var playButton = grid.FindName("PlayButton") as Button;
                var musicIcon = grid.FindName("MusicFontIcon") as FontIcon;

                if (playButton != null && musicIcon != null)
                {
                    playButton.Visibility = Visibility.Collapsed;
                    musicIcon.Visibility = Visibility.Visible;
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MusicInfo music)
            {
                ViewModel.PlayMusicCommand.Execute(music);
            }
        }

        private void PlayQueueListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MusicInfo music)
            {
                ViewModel.PlayMusicCommand.Execute(music);
            }
        }
        private async void MusicProperties_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is MusicInfo music)
            {
                var dialog = new MusicPropertiesDialog();
                dialog.SetMusic(music);
                dialog.XamlRoot = this.XamlRoot;

                await dialog.ShowAsync();
            }
        }
    }
}
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
using Yee_Music.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FavoriteSongsPage : Page
    {
        public FavoriteSongsViewModel ViewModel { get; }

        public FavoriteSongsPage()
        {
            this.InitializeComponent();

            // 初始化ViewModel
            ViewModel = new FavoriteSongsViewModel(App.MusicPlayer);
            // 设置DataContext
            this.DataContext = ViewModel;
        }


        private void FavoriteSongsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MusicInfo music)
            {
                ViewModel.PlayMusicCommand.Execute(music);
            }
        }
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is MusicInfo music)
            {
                ViewModel.RemoveFavoriteCommand.Execute(music);
            }
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is MusicInfo music)
            {
                ViewModel.PlayMusicCommand.Execute(music);
            }
        }

        private void FavoriteSongsListView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.FindName("PlayButton") is Button playButton)
            {
                playButton.Visibility = Visibility.Visible;
            }
        }

        private void FavoriteSongsListView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (element.FindName("PlayButton") is Button playButton)
                {
                    playButton.Visibility = Visibility.Collapsed;
                }

                // 同时隐藏移除按钮
                if (element.FindName("RemoveButton") is Button removeButton)
                {
                    removeButton.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}

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
using Yee_Music.Controls;

namespace Yee_Music.Pages
{
    public sealed partial class FavoriteSongsPage : Page
    {
        public FavoriteSongsViewModel ViewModel { get; }

        public FavoriteSongsPage()
        {
            this.InitializeComponent();

            ViewModel = new FavoriteSongsViewModel(App.MusicPlayer);

            this.DataContext = ViewModel;
        }
        private async void MusicList_PropertiesClick(object sender, MusicInfo music)
        {
            try
            {
                if (music != null)
                {
                    var dialog = new MusicPropertiesDialog();
                    dialog.SetMusic(music);
                    dialog.XamlRoot = this.XamlRoot;

                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示属性对话框时出错: {ex.Message}");
            }
        }
    }
}

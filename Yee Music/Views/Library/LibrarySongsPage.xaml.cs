using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yee_Music.Controls;
using Yee_Music.Models;
using Yee_Music.ViewModels;

namespace Yee_Music.Pages
{
    public sealed partial class LibrarySongsPage : Page
    {
        public LibraryViewModel ViewModel => LibraryViewModel.Instance;

        public LibrarySongsPage()
        {
            this.InitializeComponent();
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
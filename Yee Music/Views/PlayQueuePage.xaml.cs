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

            ViewModel = App.Services.GetService<PlayQueueViewModel>() ?? new PlayQueueViewModel(App.MusicPlayer);

            this.DataContext = ViewModel;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
 
            if (ViewModel != null)
            {
                ViewModel.RefreshProperties();
            }
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
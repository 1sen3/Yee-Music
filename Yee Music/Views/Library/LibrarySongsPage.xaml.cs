using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TagLib.Riff;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;
using Yee_Music.Models;
using Yee_Music.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibrarySongsPage : Page
    {
        public LibraryViewModel ViewModel => LibraryViewModel.Instance;

        public LibrarySongsPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LibrarySongsListView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var grid = sender as Grid;
            var playButton = grid?.FindName("PlayButton") as Button;
            if (playButton != null)
            {
                playButton.Visibility = Visibility.Visible;
            }
        }

        private void LibrarySongsListView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var grid = sender as Grid;
            var playButton = grid?.FindName("PlayButton") as Button;
            if (playButton != null)
            {
                playButton.Visibility = Visibility.Collapsed;
            }
        }
    }
}

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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FavoritePage : Page
    {
        public FavoritePage()
        {
            this.InitializeComponent();

            FavoriteSegmented.SelectedIndex = 0;
        }
        private void FavoriteSegmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (FavoriteSegmented.SelectedIndex)
            {
                case 0:
                    Settings_PivotFrame.Navigate(typeof(FavoriteSongsPage));
                    break;
                case 1:
                    Settings_PivotFrame.Navigate(typeof(Tips));
                    break;
                case 2:
                    Settings_PivotFrame.Navigate(typeof(Tips));
                    break;
            }
        }
    }
}

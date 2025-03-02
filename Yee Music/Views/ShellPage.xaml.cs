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
using Windows.UI.WindowManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShellPage : Page
    {
        public ShellPage()
        {
            this.InitializeComponent();
            navFrame.Content = new HomePage();
            nav.SelectedItem = HomePage;
        }
        private void Nav_SelectionChanged(NavigationView sender,NavigationViewSelectionChangedEventArgs args)
        {
            Page curPage = new Page();
            if (args.IsSettingsSelected == true)
            {
                navFrame.Navigate(typeof(SettingsPage));
            }
            string selected = args.SelectedItemContainer.Tag.ToString();
            switch (selected)
            {
                case "Home":
                    navFrame.Navigate(typeof(Tips));
                    break;
                case "Favorite":
                    navFrame.Navigate(typeof(FavoritePage));
                    break;
                case "PlayQueue":
                    navFrame.Navigate(typeof(PlayQueuePage));
                    break;
                case "Library":
                    navFrame.Navigate(typeof(LibraryPage));
                    break;
            }
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            if (nav.IsPaneOpen)
            {
                nav.IsPaneOpen = false;
            }
            else
            {
                nav.IsPaneOpen = true;
            }
        }
    }
}

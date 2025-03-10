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
using FluentIcons.Common;
using FluentIcons.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShellPage : Page
    {
        private Dictionary<string, (FluentIcon icon, TextBlock text)> _navItems;
        public ShellPage()
        {
            this.InitializeComponent();

            InitializeNavItems();

            navFrame.Content = new HomePage();
            nav.SelectedItem = HomePage;
        }
        private void InitializeNavItems()
        {
            _navItems = new Dictionary<string, (FluentIcon icon, TextBlock text)>
            {
                { "Home", (HomeIcon, HomeText) },
                { "Favorite", (FavIcon, FavText) },
                { "Library", (LibIcon, LibText) },
                { "PlayQueue", (QueueIcon, QueueText) },
                { "Settings", (SetIcon, SetText) }
            };
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
                    UpdateNavItemsAppearance(selected);
                    navFrame.Navigate(typeof(Tips));
                    break;
                case "Favorite":
                    UpdateNavItemsAppearance(selected);
                    navFrame.Navigate(typeof(FavoritePage));
                    break;
                case "PlayQueue":
                    UpdateNavItemsAppearance(selected);
                    navFrame.Navigate(typeof(PlayQueuePage));
                    break;
                case "Library":
                    UpdateNavItemsAppearance(selected);
                    navFrame.Navigate(typeof(LibraryPage));
                    break;
                case "Settings":
                    UpdateNavItemsAppearance(selected);
                    navFrame.Navigate(typeof(SettingsPage));
                    break;
            }
        }

        private void UpdateNavItemsAppearance(string selectedTag)
        {
            // 获取系统强调色
            var accentBrush = (SolidColorBrush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];

            // 更新所有导航项的外观
            foreach (var item in _navItems)
            {
                bool isSelected = item.Key == selectedTag;

                // 设置图标样式
                item.Value.icon.IconVariant = isSelected ? IconVariant.Filled : IconVariant.Regular;

                // 设置图标颜色
                item.Value.icon.Foreground = isSelected ? accentBrush :
                    (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"];

                // 设置文本可见性
                item.Value.text.Visibility = isSelected ? Visibility.Collapsed : Visibility.Visible;
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

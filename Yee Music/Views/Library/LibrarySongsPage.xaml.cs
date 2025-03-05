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
            try
            {
                this.InitializeComponent();
                this.DataContext = ViewModel;

                // 初始化右键菜单项 - 使用try-catch包裹可能出错的代码
                try
                {
                    InitializeContextMenu();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"初始化右键菜单出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LibrarySongsPage初始化出错: {ex.Message}");
            }
        }

        private void InitializeContextMenu()
        {
            // 确保MusicList控件已初始化
            if (MusicList == null)
            {
                System.Diagnostics.Debug.WriteLine("MusicList控件未找到");
                return;
            }

            // 确保ViewModel已初始化
            if (ViewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("ViewModel未初始化");
                return;
            }

            try
            {
                // 添加到收藏夹菜单项
                var addToFavoriteItem = new MenuFlyoutItem
                {
                    Text = "添加到收藏夹",
                    Icon = new FontIcon { Glyph = "\uE734" }
                };

                // 检查命令是否存在
                if (ViewModel.AddToFavoriteCommand != null)
                {
                    addToFavoriteItem.Command = ViewModel.AddToFavoriteCommand;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AddToFavoriteCommand未实现");
                }

                // 添加到播放列表菜单项
                var addToPlaylistItem = new MenuFlyoutItem
                {
                    Text = "添加到播放列表",
                    Icon = new FontIcon { Glyph = "\uE8FD" }
                };

                // 检查ContextMenuItems是否存在
                if (MusicList.ContextMenuItems != null)
                {
                    // 添加到控件的右键菜单
                    MusicList.ContextMenuItems.Add(addToFavoriteItem);
                    MusicList.ContextMenuItems.Add(addToPlaylistItem);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ContextMenuItems集合未初始化");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加菜单项时出错: {ex.Message}");
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
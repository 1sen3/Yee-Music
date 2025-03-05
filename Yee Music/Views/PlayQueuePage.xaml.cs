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

            InitializeContextMenu();  
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
                // 确保ContextMenuItems集合已初始化
                if (MusicList.ContextMenuItems == null)
                {
                    MusicList.ContextMenuItems = new ObservableCollection<MenuFlyoutItemBase>();
                }

                // 清空现有菜单项，避免重复添加
                MusicList.ContextMenuItems.Clear();

                // 移出播放列表菜单项
                var removeFromQueueItem = new MenuFlyoutItem
                {
                    Text = "移出播放列表",
                    Icon = new FontIcon { Glyph = "\uE74D" }  // 使用删除图标
                };

                // 检查命令是否存在
                if (ViewModel.RemoveMusicCommand != null)
                {
                    removeFromQueueItem.Command = ViewModel.RemoveMusicCommand;
                    // 直接使用命令参数绑定，不需要设置CommandParameter
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("RemoveMusicCommand未实现");
                }

                // 添加到控件的右键菜单
                MusicList.ContextMenuItems.Add(removeFromQueueItem);

                // 手动调用添加菜单项方法
                MusicList.AddContextMenuItem(removeFromQueueItem);

                System.Diagnostics.Debug.WriteLine("成功添加移出播放列表菜单项");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加菜单项时出错: {ex.Message}");
            }
        }
    }
}
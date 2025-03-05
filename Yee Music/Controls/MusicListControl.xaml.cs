using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Yee_Music.Models;

namespace Yee_Music.Controls
{
    public sealed partial class MusicListControl : UserControl
    {
        public bool ShowRemoveFromQueueMenuItem
        {
            get { return (bool)GetValue(ShowRemoveFromQueueMenuItemProperty); }
            set { SetValue(ShowRemoveFromQueueMenuItemProperty, value); }
        }
        public static readonly DependencyProperty ShowRemoveFromQueueMenuItemProperty = 
            DependencyProperty.Register("ShowRemoveFromQueueMenuItem", typeof(bool), typeof(MusicListControl),
            new PropertyMetadata(false, OnShowRemoveFromQueueMenuItemChanged));
        public ICommand RemoveFromQueueCommand
        {
            get { return (ICommand)GetValue(RemoveFromQueueCommandProperty); }
            set { SetValue(RemoveFromQueueCommandProperty, value); }
        }
        public static readonly DependencyProperty RemoveFromQueueCommandProperty =
            DependencyProperty.Register("RemoveFromQueueCommand", typeof(ICommand), typeof(MusicListControl),
            new PropertyMetadata(null));
        public ICommand AddToFavoriteCommand
        {
            get { return (ICommand)GetValue(AddToFavoriteCommandProperty); }
            set { SetValue(AddToFavoriteCommandProperty, value); }
        }
        public static readonly DependencyProperty AddToFavoriteCommandProperty =
            DependencyProperty.Register("AddToFavoriteCommand", typeof(ICommand), typeof(MusicListControl),
            new PropertyMetadata(null));
        public ICommand AddToPlaylistCommand
        {
            get { return (ICommand)GetValue(AddToPlaylistCommandProperty); }
            set { SetValue(AddToPlaylistCommandProperty, value); }
        }

        public static readonly DependencyProperty AddToPlaylistCommandProperty =
            DependencyProperty.Register("AddToPlaylistCommand", typeof(ICommand), typeof(MusicListControl),
            new PropertyMetadata(null));
        public MusicListControl()
        {
            this.InitializeComponent();

            this.Loaded += MusicListControl_Loaded;
        }
        private void MusicListControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMenuItemsVisibility();
        }
        private void ContextMenuItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (MenuFlyoutItemBase item in e.NewItems)
                {
                    AddContextMenuItem(item);
                }
            }
        }
        private static void OnShowRemoveFromQueueMenuItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MusicListControl;
            if (control != null)
            {
                control.UpdateMenuItemsVisibility();
            }
        }


        #region 依赖属性

        // 音乐列表数据源
        public static readonly DependencyProperty MusicListProperty =
            DependencyProperty.Register(nameof(MusicList), typeof(IEnumerable<MusicInfo>),
                typeof(MusicListControl), new PropertyMetadata(null));

        public IEnumerable<MusicInfo> MusicList
        {
            get { return (IEnumerable<MusicInfo>)GetValue(MusicListProperty); }
            set { SetValue(MusicListProperty, value); }
        }

        // 当前选中的音乐
        public static readonly DependencyProperty SelectedMusicProperty =
            DependencyProperty.Register(nameof(SelectedMusic), typeof(MusicInfo),
                typeof(MusicListControl), new PropertyMetadata(null));

        public MusicInfo SelectedMusic
        {
            get { return (MusicInfo)GetValue(SelectedMusicProperty); }
            set { SetValue(SelectedMusicProperty, value); }
        }

        // 播放命令
        public static readonly DependencyProperty PlayCommandProperty =
            DependencyProperty.Register(nameof(PlayCommand), typeof(ICommand),
                typeof(MusicListControl), new PropertyMetadata(null));

        public ICommand PlayCommand
        {
            get { return (ICommand)GetValue(PlayCommandProperty); }
            set { SetValue(PlayCommandProperty, value); }
        }

        // 是否显示空状态
        public static readonly DependencyProperty ShowEmptyStateProperty =
            DependencyProperty.Register(nameof(ShowEmptyState), typeof(bool),
                typeof(MusicListControl), new PropertyMetadata(false));

        public bool ShowEmptyState
        {
            get { return (bool)GetValue(ShowEmptyStateProperty); }
            set { SetValue(ShowEmptyStateProperty, value); }
        }

        // 空状态图标
        public static readonly DependencyProperty EmptyStateIconProperty =
            DependencyProperty.Register(nameof(EmptyStateIcon), typeof(string),
                typeof(MusicListControl), new PropertyMetadata("\uE8F1"));

        public string EmptyStateIcon
        {
            get { return (string)GetValue(EmptyStateIconProperty); }
            set { SetValue(EmptyStateIconProperty, value); }
        }

        // 空状态标题
        public static readonly DependencyProperty EmptyStateTitleProperty =
            DependencyProperty.Register(nameof(EmptyStateTitle), typeof(string),
                typeof(MusicListControl), new PropertyMetadata("暂无歌曲"));

        public string EmptyStateTitle
        {
            get { return (string)GetValue(EmptyStateTitleProperty); }
            set { SetValue(EmptyStateTitleProperty, value); }
        }

        // 空状态描述
        public static readonly DependencyProperty EmptyStateDescriptionProperty =
            DependencyProperty.Register(nameof(EmptyStateDescription), typeof(string),
                typeof(MusicListControl), new PropertyMetadata(string.Empty));

        public string EmptyStateDescription
        {
            get { return (string)GetValue(EmptyStateDescriptionProperty); }
            set { SetValue(EmptyStateDescriptionProperty, value); }
        }

        // 右键菜单项集合
        public static readonly DependencyProperty ContextMenuItemsProperty =
            DependencyProperty.Register(nameof(ContextMenuItems), typeof(ObservableCollection<MenuFlyoutItemBase>),
                typeof(MusicListControl), new PropertyMetadata(new ObservableCollection<MenuFlyoutItemBase>()));

        public ObservableCollection<MenuFlyoutItemBase> ContextMenuItems
        {
            get { return (ObservableCollection<MenuFlyoutItemBase>)GetValue(ContextMenuItemsProperty); }
            set { SetValue(ContextMenuItemsProperty, value); }
        }

        #endregion

        #region 事件

        // 音乐项点击事件
        public event EventHandler<MusicInfo> MusicItemClick;

        // 属性按钮点击事件
        public event EventHandler<MusicInfo> PropertiesClick;
        // 播放菜单项点击事件
        private void PlayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null && PlayCommand != null && PlayCommand.CanExecute(music))
                {
                    PlayCommand.Execute(music);
                }
            }
        }

        // 添加到收藏夹菜单项点击事件
        private void AddToFavoriteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null && AddToFavoriteCommand != null && AddToFavoriteCommand.CanExecute(music))
                {
                    AddToFavoriteCommand.Execute(music);
                }
            }
        }

        // 添加到播放列表菜单项点击事件
        private void AddToPlaylistMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null && AddToPlaylistCommand != null && AddToPlaylistCommand.CanExecute(music))
                {
                    AddToPlaylistCommand.Execute(music);
                }
            }
        }

        // 属性菜单项点击事件
        private void PropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null)
                {
                    PropertiesClick?.Invoke(this, music);
                }
            }
        }

        #endregion

        private void MusicListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MusicInfo music)
            {
                MusicItemClick?.Invoke(this, music);
            }
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var playButton = grid.FindName("PlayButton") as Button;
                if (playButton != null)
                {
                    playButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var playButton = grid.FindName("PlayButton") as Button;
                if (playButton != null)
                {
                    playButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MusicInfo music && PlayCommand != null)
            {
                PlayCommand.Execute(music);
            }
        }

        private void MusicProperties_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is MusicInfo music)
            {
                PropertiesClick?.Invoke(this, music);
            }
        }
        private void UpdateMenuItemsVisibility()
        {
            // 在ListView的ItemTemplate中找到MenuFlyout并更新菜单项可见性
            var template = MusicListView.ItemTemplate as DataTemplate;
            if (template != null)
            {
                MusicListView.ContainerContentChanging += (sender, args) =>
                {
                    if (args.ItemContainer != null)
                    {
                        var item = args.ItemContainer.ContentTemplateRoot as Grid;
                        if (item != null)
                        {
                            var flyout = item.ContextFlyout as MenuFlyout;
                            if (flyout != null)
                            {
                                foreach (var menuItem in flyout.Items)
                                {
                                    if (menuItem is MenuFlyoutItem mfi)
                                    {
                                        if (mfi.Name == "RemoveFromQueueMenuItem")
                                        {
                                            mfi.Visibility = ShowRemoveFromQueueMenuItem ? Visibility.Visible : Visibility.Collapsed;
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }
        }
        // 处理菜单项点击事件
        private void RemoveFromQueueMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null && RemoveFromQueueCommand != null && RemoveFromQueueCommand.CanExecute(music))
                {
                    RemoveFromQueueCommand.Execute(music);
                }
            }
        }
        // 添加一个方法来添加右键菜单项
        public void AddContextMenuItem(MenuFlyoutItemBase menuItem)
        {
            // 在代码中找到MenuFlyout并添加项
            var template = MusicListView.ItemTemplate as DataTemplate;
            if (template != null)
            {
                // 我们需要在运行时为每个项添加菜单项
                MusicListView.ContainerContentChanging += (sender, args) =>
                {
                    if (args.ItemContainer != null && args.Item is MusicInfo music)
                    {
                        var item = args.ItemContainer.ContentTemplateRoot as Grid;
                        if (item != null)
                        {
                            var flyout = item.ContextFlyout as MenuFlyout;
                            if (flyout != null)
                            {
                                // 找到分隔符的索引
                                int separatorIndex = -1;
                                for (int i = 0; i < flyout.Items.Count; i++)
                                {
                                    if (flyout.Items[i] is MenuFlyoutSeparator)
                                    {
                                        separatorIndex = i;
                                        break;
                                    }
                                }

                                // 在分隔符前插入菜单项
                                if (separatorIndex > 0)
                                {
                                    // 检查是否已添加此菜单项
                                    bool alreadyAdded = false;
                                    foreach (var existingItem in flyout.Items)
                                    {
                                        if (existingItem is MenuFlyoutItem existingMenuItem &&
                                            existingMenuItem.Text == (menuItem as MenuFlyoutItem)?.Text)
                                        {
                                            alreadyAdded = true;
                                            break;
                                        }
                                    }

                                    if (!alreadyAdded)
                                    {
                                        // 创建新的菜单项副本
                                        if (menuItem is MenuFlyoutItem originalItem)
                                        {
                                            var newItem = new MenuFlyoutItem
                                            {
                                                Text = originalItem.Text,
                                                Icon = originalItem.Icon,
                                                Command = originalItem.Command
                                            };

                                            // 设置命令参数为当前音乐项
                                            if (originalItem.CommandParameter is Binding)
                                            {
                                                newItem.CommandParameter = music;
                                            }
                                            else
                                            {
                                                newItem.CommandParameter = originalItem.CommandParameter ?? music;
                                            }

                                            flyout.Items.Insert(separatorIndex, newItem);
                                        }
                                        else
                                        {
                                            flyout.Items.Insert(separatorIndex, menuItem);
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}
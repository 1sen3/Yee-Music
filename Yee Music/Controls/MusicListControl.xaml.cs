using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Yee_Music.Models;
using Yee_Music.Services;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

namespace Yee_Music.Controls
{
    public sealed partial class MusicListControl : UserControl
    {
        public bool HasSelectedItems
        {
            get => (bool)GetValue(HasSelectedItemsProperty);
            private set => SetValue(HasSelectedItemsProperty, value);
        }
        public static readonly DependencyProperty HasSelectedItemsProperty =
            DependencyProperty.Register(
                nameof(HasSelectedItems),
                typeof(bool),
                typeof(MusicListControl),
                new PropertyMetadata(false));
        public static readonly DependencyProperty SelectedItemsCountProperty =
            DependencyProperty.Register(
                nameof(SelectedItemsCount),
                typeof(int),
                typeof(MusicListControl),
                new PropertyMetadata(0));

        public int SelectedItemsCount
        {
            get => (int)GetValue(SelectedItemsCountProperty);
            private set => SetValue(SelectedItemsCountProperty, value);
        }

        private List<MusicInfo> _selectedMusicList = new List<MusicInfo>();

        public List<MusicInfo> SelectedMusicList => _selectedMusicList;
        public bool ShowRemoveFromQueueMenuItem
        {
            get => (bool)GetValue(ShowRemoveFromQueueMenuItemProperty);
            set => SetValue(ShowRemoveFromQueueMenuItemProperty, value);
        }
        public static readonly DependencyProperty ShowRemoveFromQueueMenuItemProperty =
            DependencyProperty.Register(
                nameof(ShowRemoveFromQueueMenuItem),
                typeof(bool),
                typeof(MusicListControl),
                new PropertyMetadata(true, OnShowRemoveFromQueueMenuItemChanged));
        private static void OnShowRemoveFromQueueMenuItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnMenuItemVisibilityChanged(d, e);
        }
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
        public ICommand RemoveFromFavoriteCommand
        {
            get { return (ICommand)GetValue(RemoveFromFavoriteCommandProperty); }
            set { SetValue(RemoveFromFavoriteCommandProperty, value); }
        }
        public static readonly DependencyProperty RemoveFromFavoriteCommandProperty =
            DependencyProperty.Register("RemoveFromFavoriteCommand", typeof(ICommand), typeof(MusicListControl),
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

            ContextMenuItems = new ObservableCollection<MenuFlyoutItemBase>();
            ContextMenuItems.CollectionChanged += ContextMenuItems_CollectionChanged;

            this.Loaded += MusicListControl_Loaded;
            
            this.RegisterPropertyChangedCallback(MusicListProperty, OnMusicListChanged);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            if (MusicListView != null)
            {
                MusicListView.ContainerContentChanging += MusicListView_ContainerContentChanging;
            }
        }

        private void MusicListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase == 0)
            {
                args.RegisterUpdateCallback(ContainerContentChanging_Phase1);
            }
        }

        private void ContainerContentChanging_Phase1(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase == 1)
            {
                DispatcherQueue.GetForCurrentThread().TryEnqueue(
                    DispatcherQueuePriority.Low, 
                    () => UpdateMenuItemsVisibility());
            }
        }

        private void MusicListControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (MusicListView != null && MusicListView.Items != null)
            {
                DispatcherQueue.GetForCurrentThread().TryEnqueue(
                    DispatcherQueuePriority.Low,
                    () => 
                    {
                        UpdateMenuItemsVisibility();
                    });
            }
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

        // 各个菜单项可见性
        public bool ShowPlayMenuItem
        {
            get { return (bool)GetValue(ShowPlayMenuItemProperty); }
            set { SetValue(ShowPlayMenuItemProperty, value); }
        }

        public static readonly DependencyProperty ShowPlayMenuItemProperty =
            DependencyProperty.Register("ShowPlayMenuItem", typeof(bool), typeof(MusicListControl), 
                new PropertyMetadata(true, OnMenuItemVisibilityChanged));

        public bool ShowAddToFavoriteMenuItem
        {
            get { return (bool)GetValue(ShowAddToFavoriteMenuItemProperty); }
            set { SetValue(ShowAddToFavoriteMenuItemProperty, value); }
        }

        public static readonly DependencyProperty ShowAddToFavoriteMenuItemProperty =
            DependencyProperty.Register("ShowAddToFavoriteMenuItem", typeof(bool), typeof(MusicListControl), 
                new PropertyMetadata(true, OnMenuItemVisibilityChanged));
        public bool ShowRemoveFromFavoriteMenuItem
        {
            get { return (bool)GetValue(ShowRemoveFromFavoriteMenuItemProperty); }
            set { SetValue(ShowRemoveFromFavoriteMenuItemProperty, value); }
        }

        public static readonly DependencyProperty ShowRemoveFromFavoriteMenuItemProperty =
            DependencyProperty.Register("ShowRemoveFromFavoriteMenuItem", typeof(bool), typeof(MusicListControl),
                new PropertyMetadata(true, OnMenuItemVisibilityChanged));
        public bool ShowAddToPlaylistMenuItem
        {
            get { return (bool)GetValue(ShowAddToPlaylistMenuItemProperty); }
            set { SetValue(ShowAddToPlaylistMenuItemProperty, value); }
        }

        public static readonly DependencyProperty ShowAddToPlaylistMenuItemProperty =
            DependencyProperty.Register("ShowAddToPlaylistMenuItem", typeof(bool), typeof(MusicListControl), 
                new PropertyMetadata(true, OnMenuItemVisibilityChanged));

        public bool ShowPropertiesMenuItem
        {
            get { return (bool)GetValue(ShowPropertiesMenuItemProperty); }
            set { SetValue(ShowPropertiesMenuItemProperty, value); }
        }

        public static readonly DependencyProperty ShowPropertiesMenuItemProperty =
            DependencyProperty.Register("ShowPropertiesMenuItem", typeof(bool), typeof(MusicListControl), 
                new PropertyMetadata(true, OnMenuItemVisibilityChanged));

        #endregion

        #region 事件

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
        private async void AddToFavoriteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (AddToFavoriteCommand != null && AddToFavoriteCommand.CanExecute(MusicListView.SelectedItem))
            {
                AddToFavoriteCommand.Execute(MusicListView.SelectedItem);
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
        private void MusicListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MusicInfo music)
            {
                MusicItemClick?.Invoke(this, music);

                RefreshMenuItemsVisibility();
            }
        }
        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var playButton = grid.FindName("PlayButton") as Button;
                var checkBox = grid.FindName("MusicListCheckBox") as CheckBox;
                if (playButton != null && checkBox != null)
                {
                    playButton.Visibility = Visibility.Visible;
                    checkBox.Visibility = Visibility.Visible;
                }
            }
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var playButton = grid.FindName("PlayButton") as Button;
                var checkBox = grid.FindName("MusicListCheckBox") as CheckBox;
                if (playButton != null && checkBox != null)
                {
                    playButton.Visibility = Visibility.Collapsed;
                    if (!(bool)checkBox.IsChecked)
                    {
                        checkBox.Visibility = Visibility.Collapsed;
                    }
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

        private void RemoveFromFavoriteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var music = menuItem.DataContext as MusicInfo;
                if (music != null && RemoveFromFavoriteCommand != null && RemoveFromFavoriteCommand.CanExecute(music))
                {
                    RemoveFromFavoriteCommand.Execute(music);
                }
            }
        }

        private void MusicListCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is MusicInfo music)
            {
                if (!_selectedMusicList.Contains(music))
                {
                    checkBox.Visibility = Visibility.Visible;
                    _selectedMusicList.Add(music);
                    UpdateSelectionState(true);
                }
            }
        }

        private void MusicListCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is MusicInfo music)
            {
                checkBox.Visibility = Visibility.Collapsed;
                _selectedMusicList.Remove(music);
                UpdateSelectionState(false);
            }
        }
        #endregion
        private void UpdateSelectionState(bool state)
        {
            if (state)
            {
                SelectedItemsCount +=1;
            }
            else
            {
                SelectedItemsCount -= 1;
            }
            HasSelectedItems = SelectedItemsCount > 0;
        }
        private void OnMusicListChanged(DependencyObject sender, DependencyProperty dp)
        {
            DispatcherQueue.GetForCurrentThread().TryEnqueue(
                DispatcherQueuePriority.Low, 
                () => UpdateMenuItemsVisibility());
        }
        private void UpdateMenuItemsVisibility()
        {
            if (MusicListView != null)
            {
                foreach (var item in MusicListView.Items)
                {
                    if (MusicListView.ContainerFromItem(item) is ListViewItem container)
                    {
                        if (container.ContentTemplateRoot is Grid grid)
                        {
                            if (grid.ContextFlyout is MenuFlyout menuFlyout)
                            {
                                foreach (var menuItem in menuFlyout.Items)
                                {
                                    if (menuItem is MenuFlyoutItem flyoutItem)
                                    {
                                        if (flyoutItem.Name == "PlayMenuItem")
                                        {
                                            flyoutItem.Visibility = ShowPlayMenuItem ? Visibility.Visible : Visibility.Collapsed;
                                        }
                                        else if (flyoutItem.Name == "AddToFavoriteMenuItem")
                                        {
                                            flyoutItem.Visibility = ShowAddToFavoriteMenuItem ? Visibility.Visible : Visibility.Collapsed;
                                        }
                                        else if (flyoutItem.Name == "RemoveFromFavoriteMenuItem")
                                        {
                                            flyoutItem.Visibility = ShowRemoveFromFavoriteMenuItem ? Visibility.Visible : Visibility.Collapsed;
                                        }
                                        else if (flyoutItem.Name == "AddToPlaylistMenuItem")
                                        {
                                            flyoutItem.Visibility = ShowAddToPlaylistMenuItem ? Visibility.Visible : Visibility.Collapsed;
                                        }
                                        else if (flyoutItem.Name == "RemoveFromQueueMenuItem")
                                        {
                                            flyoutItem.Visibility = ShowRemoveFromQueueMenuItem ? Visibility.Visible : Visibility.Collapsed;
                                        }
                                        else if (flyoutItem.Name == "PropertiesMenuItem")
                                        {
                                            flyoutItem.Visibility = ShowPropertiesMenuItem ? Visibility.Visible : Visibility.Collapsed;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddContextMenuItem(MenuFlyoutItemBase menuItem)
        {
            var template = MusicListView.ItemTemplate as DataTemplate;
            if (template != null)
            {
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
                                int separatorIndex = -1;
                                for (int i = 0; i < flyout.Items.Count; i++)
                                {
                                    if (flyout.Items[i] is MenuFlyoutSeparator)
                                    {
                                        separatorIndex = i;
                                        break;
                                    }
                                }

                                if (separatorIndex > 0)
                                {
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
                                        if (menuItem is MenuFlyoutItem originalItem)
                                        {
                                            var newItem = new MenuFlyoutItem
                                            {
                                                Text = originalItem.Text,
                                                Icon = originalItem.Icon,
                                                Command = originalItem.Command
                                            };

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

        private static void OnMenuItemVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MusicListControl control)
            {
                control.UpdateMenuItemsVisibility();
            }
        }

        public void RefreshMenuItemsVisibility()
        {
            UpdateMenuItemsVisibility();
        }

        private void ClearCheckAll_Click(object sender, RoutedEventArgs e)
        {
            ClearAllSelections();
        }
        public void ClearAllSelections()
        {
            _selectedMusicList.Clear();

            foreach (var item in MusicListView.Items )
            {
                if (SelectedItemsCount == 0)
                {
                    return;
                }
                var container = MusicListView.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    var checkBox = FindCheckBoxInContainer(container);
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = false;
                    }
                }
            }
        }
        private void CheckAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach(var item in MusicListView.Items)
            {
                var container = MusicListView.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    var checkBox = FindCheckBoxInContainer(container);
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = true;
                    }
                }
            }
        }
        private CheckBox FindCheckBoxInContainer(ListViewItem container)
        {
            var grid = container.ContentTemplateRoot as Grid;
            if (grid != null)
            {
                foreach (var child in grid.Children)
                {
                    if (child is CheckBox checkBox)
                    {
                        if (checkBox.Name == "MusicListCheckBox")
                        {
                            return checkBox;
                        }
                    }
                }
            }
            return null;
        }
    }
}
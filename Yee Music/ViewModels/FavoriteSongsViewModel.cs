using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yee_Music.Models;
using System.Collections.Generic;
using Yee_Music.Services;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Yee_Music.ViewModels
{
    public enum LoadingState
    {
        Loading,
        Loaded,
        Empty
    }

    public class FavoriteSongsViewModel : ObservableRecipient
    {
        private readonly MusicPlayer _player;
        private readonly DatabaseService _databaseService;
        private ObservableCollection<MusicInfo> _favoriteMusicList = new ObservableCollection<MusicInfo>();
        private bool _isLoading;
        public bool IsFavoriteEmpty => FavoriteMusicList == null || FavoriteMusicList.Count == 0;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<MusicInfo> FavoriteMusicList
        {
            get => _favoriteMusicList;
            set
            {
                if (SetProperty(ref _favoriteMusicList, value))
                {
                    // 当收藏列表变化时，通知 IsFavoriteEmpty 属性也可能变化
                    OnPropertyChanged(nameof(IsFavoriteEmpty));
                }
            }
        }

        private LoadingState _loadingState = LoadingState.Loading;
        public LoadingState LoadingState
        {
            get => _loadingState;
            set => SetProperty(ref _loadingState, value);
        }

        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand<MusicInfo> PlayMusicCommand { get; }
        public IRelayCommand<MusicInfo> RemoveFavoriteCommand { get; }

        public FavoriteSongsViewModel(MusicPlayer player)
        {
            _player = player;
            _databaseService = App.Services.GetService<DatabaseService>();

            // 使用AsyncRelayCommand替代RelayCommand
            RefreshCommand = new AsyncRelayCommand(LoadFavoriteSongsAsync);
            PlayMusicCommand = new RelayCommand<MusicInfo>(PlayMusic);
            RemoveFavoriteCommand = new RelayCommand<MusicInfo>(RemoveFavorite);

            // 初始化时加载喜欢的歌曲
            _ = LoadFavoriteSongsAsync();

            // 订阅播放器当前音乐变化事件，用于更新UI
            _player.CurrentMusicChanged += OnCurrentMusicChanged;

            // 订阅喜欢状态变化事件
            PlayerBarViewModel.FavoriteStatusChanged += OnFavoriteStatusChanged;
        }

        private void OnCurrentMusicChanged(MusicInfo music)
        {
            // 当前音乐变化时，检查是否需要更新喜欢状态
            if (music != null)
            {
                // 刷新列表以反映最新状态
                _ = LoadFavoriteSongsAsync();
            }
        }

        public async Task LoadFavoriteSongsAsync()
        {
            try
            {
                IsLoading = true;

                // 从数据库加载喜欢的歌曲
                if (_databaseService != null)
                {
                    var favorites = await _databaseService.GetFavoriteMusicAsync();

                    // 清空并添加新的收藏歌曲
                    FavoriteMusicList.Clear();
                    foreach (var music in favorites)
                    {
                        // 确保封面图片已加载
                        if (music.AlbumArt == null && !string.IsNullOrEmpty(music.FilePath))
                        {
                            try
                            {
                                // 尝试加载封面
                                music.AlbumArt = music.LoadAlbumArt();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"加载歌曲封面出错: {ex.Message}");
                            }
                        }
                        FavoriteMusicList.Add(music);
                    }

                    Debug.WriteLine($"已从数据库加载 {FavoriteMusicList.Count} 首喜欢的歌曲");
                }
                else
                {
                    Debug.WriteLine("数据库服务未初始化");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载喜欢的歌曲时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;

                OnPropertyChanged(nameof(IsFavoriteEmpty));
            }
        }
        private void PlayMusic(MusicInfo music)
        {
            if (music == null)
                return;

            // 播放选中的歌曲
            _player.PlayAsync(music);

            // 如果歌曲不在播放队列中，添加到播放队列
            if (!PlayQueueService.Instance.PlayQueue.Contains(music))
            {
                PlayQueueService.Instance.AddToQueue(music);
            }

            // 更新播放列表服务中的当前索引
            int index = PlayQueueService.Instance.PlayQueue.IndexOf(music);
            if (index >= 0)
            {
                PlayQueueService.Instance.SetCurrentIndex(index);
            }
        }

        private async void RemoveFavorite(MusicInfo music)
        {
            if (music == null) return;

            try
            {
                // 更新数据库
                if (_databaseService != null)
                {
                    await _databaseService.UpdateFavoriteStatusAsync(music.FilePath, false);
                    Debug.WriteLine($"已将歌曲 {music.Title} 从喜欢列表中移除");
                }

                // 从列表中移除
                FavoriteMusicList.Remove(music);
                OnPropertyChanged(nameof(IsFavoriteEmpty));
                // 如果当前正在播放的是这首歌，也更新其状态
                if (_player.CurrentMusic?.FilePath == music.FilePath)
                {
                    _player.CurrentMusic.IsFavorite = false;
                    // 触发事件通知其他地方更新
                    PlayerBarViewModel.OnFavoriteStatusChanged(_player.CurrentMusic);
                }

                // 更新UI状态
                if (FavoriteMusicList.Count == 0)
                {
                    LoadingState = LoadingState.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"移除喜欢的歌曲时出错: {ex.Message}");
            }
        }

        private void OnFavoriteStatusChanged(object sender, MusicInfo music)
        {
            // 当喜欢状态变化时刷新列表
            _ = LoadFavoriteSongsAsync();
        }

        public void Dispose()
        {
            _player.CurrentMusicChanged -= OnCurrentMusicChanged;
            PlayerBarViewModel.FavoriteStatusChanged -= OnFavoriteStatusChanged;
        }

    }
}
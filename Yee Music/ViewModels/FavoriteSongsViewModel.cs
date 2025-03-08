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

            RefreshCommand = new AsyncRelayCommand(LoadFavoriteSongsAsync);
            PlayMusicCommand = new RelayCommand<MusicInfo>(PlayMusic);
            RemoveFavoriteCommand = new RelayCommand<MusicInfo>(RemoveFavorite);

            _ = LoadFavoriteSongsAsync();

            _player.CurrentMusicChanged += OnCurrentMusicChanged;

            PlayerBarViewModel.FavoriteStatusChanged += OnFavoriteStatusChanged;
        }

        private void OnCurrentMusicChanged(MusicInfo music)
        {
            if (music != null)
            {
                _ = LoadFavoriteSongsAsync();
            }
        }

        public async Task LoadFavoriteSongsAsync()
        {
            try
            {
                IsLoading = true;

                if (_databaseService != null)
                {
                    var favorites = await _databaseService.GetFavoriteMusicAsync();

                    FavoriteMusicList.Clear();
                    foreach (var music in favorites)
                    {
                        if (music.AlbumArt == null && !string.IsNullOrEmpty(music.FilePath))
                        {
                            try
                            {
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
            if (music == null || FavoriteMusicList == null || FavoriteMusicList.Count == 0)
                return;

            PlayQueueService.Instance.SetQueue(FavoriteMusicList, music);
            
            _player.PlayAsync(music);
            
            Debug.WriteLine($"将喜欢列表({FavoriteMusicList.Count}首歌曲)设置为播放队列，开始播放: {music.Title}");
        }

        private async void RemoveFavorite(MusicInfo music)
        {
            if (music == null) return;

            try
            {
                if (_databaseService != null)
                {
                    await _databaseService.UpdateFavoriteStatusAsync(music.FilePath, false);
                    Debug.WriteLine($"已将歌曲 {music.Title} 从喜欢列表中移除");
                }

                FavoriteMusicList.Remove(music);
                OnPropertyChanged(nameof(IsFavoriteEmpty));
                if (_player.CurrentMusic?.FilePath == music.FilePath)
                {
                    _player.CurrentMusic.IsFavorite = false;

                    PlayerBarViewModel.OnFavoriteStatusChanged(_player.CurrentMusic);
                }

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
            _ = LoadFavoriteSongsAsync();

            if (music != null)
            {
                if (music.IsFavorite)
                {
                    if (!FavoriteMusicList.Any(m => m.FilePath == music.FilePath))
                    {
                        FavoriteMusicList.Add(music);
                        OnPropertyChanged(nameof(IsFavoriteEmpty));
                    }
                }
                else
                {
                    var itemToRemove = FavoriteMusicList.FirstOrDefault(m => m.FilePath == music.FilePath);
                    if (itemToRemove != null)
                    {
                        FavoriteMusicList.Remove(itemToRemove);
                        OnPropertyChanged(nameof(IsFavoriteEmpty));
                    }
                }
            }
        }

        public void Dispose()
        {
            _player.CurrentMusicChanged -= OnCurrentMusicChanged;
            PlayerBarViewModel.FavoriteStatusChanged -= OnFavoriteStatusChanged;
        }
    }
}
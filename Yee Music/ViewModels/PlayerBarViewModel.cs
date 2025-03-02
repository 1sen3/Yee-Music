using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yee_Music.Models;
using Microsoft.UI.Dispatching;
using System.Diagnostics;
using Yee_Music.Services;
using System.Windows.Input;

namespace Yee_Music.ViewModels
{
    public delegate void AlbumArtChangedEventHandler(MusicInfo music);
    public class PlayerBarViewModel : ObservableRecipient
    {
        private readonly MusicPlayer _player;
        private MusicInfo _currentMusic;
        private bool _isPlaying;
        private double _progress;
        private double _volume;
        private bool _isMuted;
        private IRelayCommand _toggleMuteCommand;
        public ICommand ToggleFavoriteCommand { get; }
        private TimeSpan _currentTime;
        private string _currentTimeText;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly AppSettings _settings;
        private bool _isRepeatEnabled;
        private PlaybackMode _playMode = PlaybackMode.Sequential;
        public event AlbumArtChangedEventHandler AlbumArtChanged;
        public byte[] AlbumArt => _currentMusic?.AlbumArt;

        public PlaybackMode PlayMode
        {
            get => _playMode;
            set
            {
                if (SetProperty(ref _playMode, value))
                {
                    OnPropertyChanged(nameof(PlayMode));
                    Debug.WriteLine($"ViewModel播放模式已更改为: {value}");
                }
            }
        }
        public IRelayCommand ChangePlayModeCommand { get; }

        private void ChangePlayMode()
        {
            // 循环切换播放模式
            PlayMode = PlayMode switch
            {
                PlaybackMode.Sequential => PlaybackMode.SingleRepeat,
                PlaybackMode.SingleRepeat => PlaybackMode.ListRepeat,
                PlaybackMode.ListRepeat => PlaybackMode.Random,
                PlaybackMode.Random => PlaybackMode.Sequential,
                _ => PlaybackMode.Sequential
            };

            // 将播放模式应用到播放器
            _player.PlayMode = PlayMode;

            // 输出调试信息
            Debug.WriteLine($"播放模式已更改为: {PlayMode}");
        }
        public bool IsRepeatEnabled
        {
            get => _player.PlayMode == PlaybackMode.SingleRepeat;
            set
            {
                // 根据布尔值设置适当的播放模式
                _player.PlayMode = value ? PlaybackMode.SingleRepeat : PlaybackMode.Sequential;
                OnPropertyChanged(nameof(IsRepeatEnabled));
            }
        }
        public IRelayCommand ToggleRepeatCommand { get; }



        public PlayerBarViewModel()
        {
            _settings = AppSettings.Instance; // 使用单例实例

            // 将字符串转换为枚举类型再比较
            _isRepeatEnabled = Enum.TryParse(_settings.PlayMode, out PlaybackMode mode) &&
                               mode == PlaybackMode.SingleRepeat;

            _player = App.MusicPlayer;
            _volume = 100;

            // 初始化当前状态
            _currentMusic = _player.CurrentMusic;  // 直接设置字段

            // 初始化音量相关
            _volume = _player.Volume;
            _isMuted = _player.IsMuted;

            // 初始化播放模式
            PlayMode = _player.PlayMode;
            Debug.WriteLine($"ViewModel初始化播放模式: {PlayMode}");

            // 添加音量命令
            ToggleMuteCommand = new RelayCommand(ToggleMute);

            // 订阅音量变化事件
            _player.VolumeChanged += OnVolumeChanged;

            // 命令初始化
            PlayPauseCommand = new RelayCommand(PlayPause);
            PreviousCommand = new RelayCommand(PlayPrevious);
            NextCommand = new RelayCommand(PlayNext);
            ToggleFavoriteCommand = new RelayCommand(ToggleFavorite);
            // 订阅播放器事件
            _player.CurrentMusicChanged += OnCurrentMusicChanged;
            _player.PlaybackStateChanged += OnPlaybackStateChanged;
            _player.PositionChanged += OnPositionChanged;

            // 初始化当前状态
            CurrentMusic = _player.CurrentMusic;
            IsPlaying = _player.IsPlaying;

            // 如果有当前音乐，手动触发一次位置更新
            if (CurrentMusic != null)
            {
                _player.UpdatePosition();
            }
            else
            {
                CurrentTimeText = "00:00/00:00";
            }

            // 初始化其他状态
            IsPlaying = _player.IsPlaying;
            CurrentTimeText = "00:00/00:00";
            ToggleRepeatCommand = new RelayCommand(ToggleRepeat);
            ChangePlayModeCommand = new RelayCommand(ChangePlayMode);

            // 订阅播放完成事件
            _player.PlaybackCompleted += OnPlaybackCompleted;
        }

        // 在 OnCurrentMusicChanged 方法中添加
        private void OnCurrentMusicChanged(MusicInfo music)
        {
            CurrentMusic = music;  // 使用属性而不是字段
            Debug.WriteLine($"Current music changed to: {music?.Title ?? "null"}");  // 添加日志
                                                                                     // 确保播放模式与播放器同步
            PlayMode = _player.PlayMode;

            // 触发专辑封面更新事件
            AlbumArtChanged?.Invoke(music);
        }
        private void UpdateTimeText(TimeSpan position)
        {
            try
            {
                if (CurrentMusic?.Duration != null)
                {
                    CurrentTimeText = $"{position:mm\\:ss}/{CurrentMusic.Duration:mm\\:ss}";
                    OnPropertyChanged(nameof(CurrentTimeText));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新时间文本出错: {ex.Message}");
            }
        }
        public MusicInfo CurrentMusic
        {
            get => _currentMusic;
            set
            {
                if (SetProperty(ref _currentMusic, value))
                {
                    // 触发相关属性更新
                    OnPropertyChanged(nameof(CurrentMusic));
                    UpdateTimeText(_currentTime);

                    // 触发专辑封面更新事件
                    AlbumArtChanged?.Invoke(value);
                }
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public double Progress
        {
            get => _progress;
            set
            {
                if (SetProperty(ref _progress, value))
                {
                    _player.SetPosition(TimeSpan.FromSeconds(value));
                }
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    _player.Volume = value;
                    IsMuted = (value <= 0);
                }
            }
        }
        public bool IsMuted
        {
            get => _isMuted;
            set => SetProperty(ref _isMuted, value);
        }
        public IRelayCommand ToggleMuteCommand
        {
            get => _toggleMuteCommand;
            set => _toggleMuteCommand = value;
        }
        private void ToggleMute()
        {
            _player.IsMuted = !_player.IsMuted;
        }
        private void OnVolumeChanged(double volume, bool isMuted)
        {
            Volume = volume;
            IsMuted = isMuted;
        }

        public string CurrentTimeText
        {
            get => _currentTimeText;
            set => SetProperty(ref _currentTimeText, value);  // 移除 private 修饰符
        }

        public IRelayCommand PlayPauseCommand { get; }

        public IRelayCommand PreviousCommand { get; }
        public IRelayCommand NextCommand { get; }

        private void PlayPause()
        {
            if (_player.IsPlaying)
            {
                _player.Pause();
            }
            else
            {
                _player.Resume();
            }
        }

        private void PlayNext()
        {
            var nextMusic = PlayQueueService.Instance.GetNext();
            if (nextMusic != null)
            {
                _player.PlayAsync(nextMusic);
            }
        }

        private void PlayPrevious()
        {
            var previousMusic = PlayQueueService.Instance.GetPrevious();
            if (previousMusic != null)
            {
                _player.PlayAsync(previousMusic);
            }
        }

        private void OnPlaybackStateChanged(bool isPlaying)
        {
            try
            {
                IsPlaying = isPlaying;
                // 确保属性变更通知被触发
                OnPropertyChanged(nameof(IsPlaying));
                Debug.WriteLine($"播放状态已更改为: {isPlaying}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新播放状态出错: {ex.Message}");
            }
        }

        private void OnPositionChanged(TimeSpan position)
        {
            try
            {
                _currentTime = position;
                Progress = position.TotalSeconds;
                UpdateTimeText(position);

                // 确保属性变更通知被触发
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(CurrentTimeText));

                Debug.WriteLine($"进度更新: {position}, Progress={Progress}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新进度出错: {ex.Message}");
            }
        }
        private void ToggleRepeat()
        {
            IsRepeatEnabled = !IsRepeatEnabled;
        }
        private async void OnPlaybackCompleted()
        {
            if (IsRepeatEnabled && CurrentMusic != null)
            {
                await _player.PlayAsync(CurrentMusic);
            }
        }
        private async void ToggleFavorite()
        {
            if (CurrentMusic == null)
            {
                return;
            }

            // 切换喜欢状态
            CurrentMusic.IsFavorite = !CurrentMusic.IsFavorite;

            // 通知UI更新
            OnPropertyChanged(nameof(CurrentMusic));

            // 保存喜欢状态到数据库
            var dbService = App.Services.GetService(typeof(DatabaseService)) as DatabaseService;
            if (dbService != null)
            {
                await dbService.UpdateFavoriteStatusAsync(CurrentMusic.FilePath, CurrentMusic.IsFavorite);
                Debug.WriteLine($"{(CurrentMusic.IsFavorite ? "添加到" : "从")}喜欢列表{(CurrentMusic.IsFavorite ? "" : "移除")}: {CurrentMusic.Title}");
            }

            // 触发事件通知其他ViewModel
            FavoriteStatusChanged?.Invoke(this, CurrentMusic);
        }
        private async void SaveFavoriteStatus(MusicInfo music)
        {
            try
            {
                if (music == null) return;

                var dbService = App.Services.GetService(typeof(DatabaseService)) as DatabaseService;
                if (dbService != null)
                {
                    await dbService.UpdateFavoriteStatusAsync(music.FilePath, music.IsFavorite);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存喜欢状态时出错: {ex.Message}");
            }
        }
        // 加载音乐时检查喜欢状态
        private async void LoadFavoriteStatus(MusicInfo music)
        {
            if (music == null)
                return;

            try
            {
                var dbService = App.Services.GetService(typeof(DatabaseService)) as DatabaseService;
                if (dbService != null)
                {
                    var allMusic = await dbService.GetAllMusicAsync();
                    var dbMusic = allMusic.FirstOrDefault(m => m.FilePath == music.FilePath);
                    if (dbMusic != null)
                    {
                        music.IsFavorite = dbMusic.IsFavorite;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载喜欢状态时出错: {ex.Message}");
            }
        }
        public static event EventHandler<MusicInfo> FavoriteStatusChanged;
        public static void OnFavoriteStatusChanged(MusicInfo music)
        {
            // 触发FavoriteStatusChanged事件
            FavoriteStatusChanged?.Invoke(null, music);
            Debug.WriteLine($"触发喜欢状态变更事件: {music.Title}, 状态: {music.IsFavorite}");
        }
    }
}

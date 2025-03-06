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
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace Yee_Music.ViewModels
{
    public delegate void AlbumArtChangedEventHandler(MusicInfo music);
    public class PlayerBarViewModel : ObservableRecipient
    {
        private readonly MusicPlayer _player;
        private MusicInfo _currentMusic;
        private PlayQueueService _playQueueService;
        //private readonly SystemMediaTransportControlsService _smtcService;
        private bool _isPlaying;
        private double _progress;
        private double _volume;
        private bool _isMuted;
        private IRelayCommand _toggleMuteCommand;
        private bool _isShuffleEnabled;
        public IRelayCommand ToggleShuffleCommand { get; }
        private IRelayCommand<MusicInfo> _toggleFavoriteCommand;
        public IRelayCommand<MusicInfo> ToggleFavoriteCommand => _toggleFavoriteCommand ??= new RelayCommand<MusicInfo>(ToggleFavorite);
        private TimeSpan _currentTime;
        private string _currentTimeText;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly AppSettings _settings;
        private bool _isRepeatEnabled;
        private PlaybackMode _playMode = PlaybackMode.Sequential;
        public event AlbumArtChangedEventHandler AlbumArtChanged;
        public byte[] AlbumArt => _currentMusic?.AlbumArt;
        public ObservableCollection<MusicInfo> PlayQueue => _playQueueService.PlayQueue;
        // 队列是否为空
        public bool IsQueueEmpty => PlayQueue == null || PlayQueue.Count == 0;
        public event EventHandler PlayQueueChanged;
        // 播放特定音乐命令
        private IRelayCommand<MusicInfo> _playMusicCommand;
        public IRelayCommand<MusicInfo> PlayMusicCommand => _playMusicCommand ??= new RelayCommand<MusicInfo>(PlayMusic);
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
        public bool IsShuffleEnabled
        {
            get => _isShuffleEnabled;
            set
            {
                if (SetProperty(ref _isShuffleEnabled, value))
                {
                    // 将随机播放状态应用到播放器
                    _player.IsShuffleEnabled = value;

                    // 如果开启随机播放，但当前是循环模式，则不允许
                    if (value && (_playMode == PlaybackMode.SingleRepeat || _playMode == PlaybackMode.ListRepeat))
                    {
                        // 循环模式优先级高于随机播放
                        _isShuffleEnabled = false;
                        _player.IsShuffleEnabled = false;
                        Debug.WriteLine("循环模式下不能开启随机播放");
                    }
                    else
                    {
                        Debug.WriteLine($"随机播放状态已更改为: {value}");
                    }

                    // 保存设置
                    _settings.IsShuffleEnabled = _isShuffleEnabled;
                    _settings.Save();

                    OnPropertyChanged(nameof(IsShuffleEnabled));
                }
            }
        }
        public IRelayCommand ChangePlayModeCommand { get; }

        private void ChangePlayMode()
        {
            // 循环切换播放模式
            PlayMode = PlayMode switch
            {
                PlaybackMode.Sequential => PlaybackMode.ListRepeat,
                PlaybackMode.ListRepeat => PlaybackMode.SingleRepeat,
                PlaybackMode.SingleRepeat => PlaybackMode.Sequential,
                PlaybackMode.Random => PlaybackMode.ListRepeat, // 从随机模式切换到列表循环
                _ => PlaybackMode.Sequential
            };

            // 将播放模式应用到播放器
            _player.PlayMode = PlayMode;

            // 如果切换到循环模式，则关闭随机播放
            if (PlayMode == PlaybackMode.ListRepeat || PlayMode == PlaybackMode.SingleRepeat)
            {
                IsShuffleEnabled = false;
            }

            // 输出调试信息
            Debug.WriteLine($"播放模式已更改为: {PlayMode}, 随机播放: {IsShuffleEnabled}");
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

            // 初始化 DispatcherQueue
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();


            // 将字符串转换为枚举类型再比较
            _isRepeatEnabled = Enum.TryParse(_settings.PlayMode, out PlaybackMode mode) &&
                               mode == PlaybackMode.SingleRepeat;

            _player = App.MusicPlayer;
            _volume = 100;

            // 初始化 PlayQueueService
            _playQueueService = PlayQueueService.Instance;

            // 获取 SMTC 服务实例
            //_smtcService = SystemMediaTransportControlsService.Instance;

            // 初始化当前状态
            _currentMusic = _player.CurrentMusic;  // 直接设置字段

            _isShuffleEnabled = _settings.IsShuffleEnabled;

            // 初始化音量相关
            _volume = _player.Volume;
            _isMuted = _player.IsMuted;

            // 初始化播放模式
            PlayMode = _player.PlayMode;
            Debug.WriteLine($"ViewModel初始化播放模式: {PlayMode}");
            // 初始化随机播放命令
            ToggleShuffleCommand = new RelayCommand(ToggleShuffle);

            // 添加音量命令
            ToggleMuteCommand = new RelayCommand(ToggleMute);

            // 订阅音量变化事件
            _player.VolumeChanged += OnVolumeChanged;

            // 订阅播放模式变更事件
            _player.PlayModeChanged += OnPlayModeChanged;

            // 订阅播放队列变化事件
            if (_playQueueService != null)
            {
                _playQueueService.QueueChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(IsQueueEmpty));
                    PlayQueueChanged?.Invoke(this, EventArgs.Empty);
                };
            }

            // 命令初始化
            PlayPauseCommand = new RelayCommand(PlayPause);
            PreviousCommand = new RelayCommand(PlayPrevious);
            NextCommand = new RelayCommand(PlayNext);

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

        private void OnCurrentMusicChanged(MusicInfo music)
        {
            // 使用DispatcherQueue确保在UI线程上更新UI
            _dispatcherQueue.TryEnqueue(() => 
            {
                try
                {
                    Debug.WriteLine($"在UI线程上更新CurrentMusic: {music?.Title ?? "null"}");
                    
                    // 设置当前音乐属性
                    if (music != _currentMusic)  // 只在实际变化时更新，避免循环引用
                    {
                        CurrentMusic = music;  // 使用属性而不是字段
                    }
                    
                    // 确保播放模式与播放器同步
                    PlayMode = _player.PlayMode;

                    // 不要在这里触发AlbumArtChanged，因为CurrentMusic的setter已经会触发
                    // AlbumArtChanged?.Invoke(music);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"更新CurrentMusic时出错: {ex.Message}");
                }
            });
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
                try
                {
                    // 检查是否在UI线程上
                    if (DispatcherQueue.GetForCurrentThread() != _dispatcherQueue)
                    {
                        // 如果不在UI线程上，则切换到UI线程
                        Debug.WriteLine("警告：在非UI线程上尝试设置CurrentMusic，正在切换到UI线程");
                        _dispatcherQueue.TryEnqueue(() => CurrentMusic = value);
                        return;
                    }
                    
                    // 如果在UI线程上，正常处理
                    if (SetProperty(ref _currentMusic, value))
                    {
                        try
                        {
                            // 触发相关属性更新
                            OnPropertyChanged(nameof(CurrentMusic));
                            UpdateTimeText(_currentTime);

                            // 触发专辑封面更新事件（确保value不为null时才调用）
                            if (value != null)
                            {
                                AlbumArtChanged?.Invoke(value);
                            }
                            else
                            {
                                Debug.WriteLine("警告: 尝试设置CurrentMusic为null");
                                // 可以选择在这里触发一个特殊的事件来清除专辑封面
                                AlbumArtChanged?.Invoke(null); // 传递null以清除专辑封面
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"更新CurrentMusic相关属性时出错: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"设置CurrentMusic时发生异常: {ex.Message}");
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
                try
                {
                    // 检查是否在UI线程上
                    if (DispatcherQueue.GetForCurrentThread() != _dispatcherQueue)
                    {
                        // 如果不在UI线程上，则切换到UI线程
                        Debug.WriteLine("警告：在非UI线程上尝试设置Progress，正在切换到UI线程");
                        _dispatcherQueue.TryEnqueue(() => Progress = value);
                        return;
                    }
                    
                    // 在UI线程上执行
                    if (SetProperty(ref _progress, value))
                    {
                        _player.SetPosition(TimeSpan.FromSeconds(value));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"设置Progress时发生异常: {ex.Message}");
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

        private async void PlayNext()
        {
            try
            {
                bool success = await _player.PlayNextAsync();
                
                if (success)
                {
                    // 手动更新播放状态为播放
                    IsPlaying = true;
                    OnPropertyChanged(nameof(IsPlaying));
                    Debug.WriteLine("播放状态已更新为播放");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放下一首歌曲时出错: {ex.Message}");
            }
        }

        private async void PlayPrevious()
        {
            try
            {
                bool success = await _player.PlayPreviousAsync();
                
                if (success)
                {
                    // 手动更新播放状态为播放
                    IsPlaying = true;
                    OnPropertyChanged(nameof(IsPlaying));
                    Debug.WriteLine("播放状态已更新为播放");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放上一首歌曲时出错: {ex.Message}");
            }
        }

        private void OnPlaybackStateChanged(bool isPlaying)
        {
            try
            {
                // 使用 DispatcherQueue 确保在 UI 线程上更新属性
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsPlaying = isPlaying;
                    OnPropertyChanged(nameof(IsPlaying));
                    Debug.WriteLine($"播放状态已更新为: {(isPlaying ? "播放" : "暂停")}");
                });
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
                // 使用DispatcherQueue确保在UI线程上更新UI
                _dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        _currentTime = position;
                        Progress = position.TotalSeconds;
                        UpdateTimeText(position);

                        // 确保属性变更通知被触发
                        OnPropertyChanged(nameof(Progress));
                        OnPropertyChanged(nameof(CurrentTimeText));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"在UI线程上更新进度时出错: {ex.Message}");
                    }
                });
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

        // 添加随机播放切换方法
        private void ToggleShuffle()
        {
            // 切换随机播放状态
            IsShuffleEnabled = !IsShuffleEnabled;

            // 如果开启随机播放，则设置播放模式为随机
            if (IsShuffleEnabled)
            {
                // 只有在非循环模式下才能设置为随机播放
                if (_playMode != PlaybackMode.SingleRepeat && _playMode != PlaybackMode.ListRepeat)
                {
                    PlayMode = PlaybackMode.Random;
                    _player.PlayMode = PlayMode;
                }
                else
                {
                    // 如果是循环模式，则不允许开启随机播放
                    IsShuffleEnabled = false;
                }
            }
            else if (_playMode == PlaybackMode.Random)
            {
                // 如果关闭随机播放且当前是随机模式，则恢复为顺序播放
                PlayMode = PlaybackMode.Sequential;
                _player.PlayMode = PlayMode;
            }

            Debug.WriteLine($"切换随机播放: {IsShuffleEnabled}, 播放模式: {PlayMode}");
        }
        // 添加播放模式变更事件处理方法
        private void OnPlayModeChanged(PlaybackMode mode, bool isShuffleEnabled)
        {
            // 更新 ViewModel 中的状态
            PlayMode = mode;

            // 只有当模式不是循环模式时，才更新随机播放状态
            if (mode != PlaybackMode.SingleRepeat && mode != PlaybackMode.ListRepeat)
            {
                _isShuffleEnabled = isShuffleEnabled;
                OnPropertyChanged(nameof(IsShuffleEnabled));
            }
            else
            {
                // 循环模式下，关闭随机播放
                _isShuffleEnabled = false;
                OnPropertyChanged(nameof(IsShuffleEnabled));
            }

            Debug.WriteLine($"播放模式已更改为: {mode}, 随机播放: {_isShuffleEnabled}");
        }
        private void OnPlaybackCompleted()
        {
            try
            {
                // 在UI线程上检查状态
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        if (IsRepeatEnabled && CurrentMusic != null)
                        {
                            Debug.WriteLine("播放完成，由于启用了重复播放，正在重新播放当前歌曲");
                            await _player.PlayAsync(CurrentMusic);
                        }
                        else
                        {
                            Debug.WriteLine("播放完成，继续播放队列中的下一首");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"在UI线程上处理播放完成时出错: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理播放完成事件时出错: {ex.Message}");
            }
        }
        private async void ToggleFavorite(MusicInfo music)
        {
            if (music != null)
            {
                try
                {
                    // 切换收藏状态
                    music.IsFavorite = !music.IsFavorite;

                    // 更新数据库
                    var databaseService = App.Services.GetService<DatabaseService>();
                    if (databaseService != null)
                    {
                        await databaseService.UpdateMusicFavoriteStatusAsync(music.FilePath, music.IsFavorite);
                        System.Diagnostics.Debug.WriteLine($"更新音乐收藏状态: {music.Title}, 收藏: {music.IsFavorite}");
                    }

                    // 通知 UI 更新
                    OnPropertyChanged(nameof(CurrentMusic));

                    // 触发事件通知其他地方更新
                    OnFavoriteStatusChanged(music);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"切换收藏状态出错: {ex.Message}");
                }
            }
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
            FavoriteStatusChanged?.Invoke(null, music);
        }
        private void PlayMusic(MusicInfo music)
        {
            if (music != null && _playQueueService != null)
            {
                int index = -1;

                // 在播放队列中查找音乐索引
                if (_playQueueService.PlayQueue != null)
                {
                    for (int i = 0; i < _playQueueService.PlayQueue.Count; i++)
                    {
                        if (_playQueueService.PlayQueue[i].FilePath == music.FilePath)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                if (index >= 0)
                {
                    // 直接使用 _player 播放指定索引的音乐
                    _player.PlayAsync(_playQueueService.PlayQueue[index]);
                }
            }
        }

        // 从播放列表移除音乐命令
        private IRelayCommand<MusicInfo> _removeMusicCommand;
        public IRelayCommand<MusicInfo> RemoveMusicCommand => _removeMusicCommand ??= new RelayCommand<MusicInfo>(RemoveMusic);
        private void RemoveMusic(MusicInfo music)
        {
            if (music != null && _playQueueService != null && _playQueueService.PlayQueue != null)
            {
                // 从播放队列中移除音乐
                var itemToRemove = _playQueueService.PlayQueue.FirstOrDefault(m => m.FilePath == music.FilePath);
                if (itemToRemove != null)
                {
                    _playQueueService.PlayQueue.Remove(itemToRemove);
                    OnPropertyChanged(nameof(IsQueueEmpty));
                    PlayQueueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}

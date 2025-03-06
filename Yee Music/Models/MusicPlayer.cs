using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using System.IO;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using System.Diagnostics;
using FFmpeg;
using NAudio.Wave;
using Windows.Storage;
using System.Timers;
using Yee_Music.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.Media;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
namespace Yee_Music.Models
{
    public enum PlaybackMode
    {
        Sequential,    // 顺序播放
        SingleRepeat,  // 单曲循环
        ListRepeat,    // 列表循环
        Random         // 随机播放
    }
    public class MusicPlayer
    {
        private MediaPlayer _mediaPlayer;
        private MusicInfo _currentMusic;
        private bool _isPlaying;
        private double _progress;
        private double _volume = 100;
        private double _lastVolume = 100;
        private bool _isMuted = false;
        private TimeSpan _currentTime;
        private readonly DispatcherTimer _positionTimer;
        private PlaybackMode _playMode = PlaybackMode.Sequential;
        private readonly AppSettings _settings;
        private PlayQueueService _playQueueService;
        
        // SMTC相关字段
        private SystemMediaTransportControls _systemMediaControls;
        private SystemMediaTransportControlsDisplayUpdater _displayUpdater;
        
        // 防止重复触发MediaEnded的标记
        private bool _isHandlingMediaEnded = false;
        private DateTime _lastPositionCheckTime = DateTime.MinValue;

        public event Action PlaybackCompleted;
        public event Action<bool> PlaybackStateChanged;
        public event Action<TimeSpan> PositionChanged;
        public event Action<MusicInfo> CurrentMusicChanged;
        public event Action<double, bool> VolumeChanged;

        private bool _isShuffleEnabled = false;
        // 添加随机播放属性
        public bool IsShuffleEnabled
        {
            get => _isShuffleEnabled;
            set
            {
                // 如果循环模式为列表循环或单曲循环，则不允许开启随机播放
                if (value && (_playMode == PlaybackMode.ListRepeat || _playMode == PlaybackMode.SingleRepeat))
                {
                    _isShuffleEnabled = false;
                    return;
                }

                _isShuffleEnabled = value;

                // 保存设置
                _settings.IsShuffleEnabled = value;
                _settings.Save();

                // 如果开启随机播放，则设置播放模式为随机
                if (value)
                {
                    _playMode = PlaybackMode.Random;
                }
                else if (_playMode == PlaybackMode.Random)
                {
                    // 如果关闭随机播放且当前是随机模式，则恢复为顺序播放
                    _playMode = PlaybackMode.Sequential;
                }

                // 触发播放模式变更事件
                PlayModeChanged?.Invoke(_playMode, _isShuffleEnabled);

                Debug.WriteLine($"随机播放状态: {value}, 播放模式: {_playMode}");
            }
        }
        // 播放模式变更事件
        public event Action<PlaybackMode, bool> PlayModeChanged;
        public bool IsPlaying => _isPlaying;
        public double Progress => _progress;
        public double Volume
        {
            get => _volume;
            set
            {
                if (value < 0) value = 0;
                if (value > 100) value = 100;

                _volume = value;
                _mediaPlayer.Volume = value / 100.0;

                if (value > 0)
                    _lastVolume = value;

                _isMuted = (value <= 0);

                // 保存设置
                _settings.Volume = value;
                _settings.Save();

                // 触发音量变化事件
                VolumeChanged?.Invoke(value, _isMuted);
            }
        }
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (value)
                {
                    // 静音
                    _lastVolume = _volume > 0 ? _volume : 100;
                    Volume = 0;
                }
                else
                {
                    // 取消静音
                    Volume = _lastVolume;
                }

                _isMuted = value;
            }
        }
        public PlaybackMode PlayMode
        {
            get => _playMode;
            set
            {
                _playMode = value;

                // 如果设置为列表循环或单曲循环，则关闭随机播放
                if (value == PlaybackMode.ListRepeat || value == PlaybackMode.SingleRepeat)
                {
                    _isShuffleEnabled = false;
                }
                // 如果设置为随机播放，则开启随机播放
                else if (value == PlaybackMode.Random)
                {
                    _isShuffleEnabled = true;
                }

                UpdatePlayMode();

                // 保存设置 - 将枚举转换为字符串
                _settings.PlayMode = value.ToString();
                _settings.IsShuffleEnabled = _isShuffleEnabled;
                _settings.Save();
                Debug.WriteLine($"保存播放模式: {value}, 随机播放: {_isShuffleEnabled}");
            }
        }
        // 切换随机播放状态
        public void ToggleShuffle()
        {
            IsShuffleEnabled = !IsShuffleEnabled;
        }
        // 循环切换播放模式
        public void CyclePlayMode()
        {
            // 循环切换播放模式：顺序播放 -> 列表循环 -> 单曲循环 -> 顺序播放
            PlayMode = PlayMode switch
            {
                PlaybackMode.Sequential => PlaybackMode.ListRepeat,
                PlaybackMode.ListRepeat => PlaybackMode.SingleRepeat,
                PlaybackMode.SingleRepeat => PlaybackMode.Sequential,
                PlaybackMode.Random => PlaybackMode.ListRepeat, // 从随机模式切换到列表循环
                _ => PlaybackMode.Sequential
            };
        }

        private void UpdatePlayMode()
        {
            try
            {
                switch (_playMode)
                {
                    case PlaybackMode.Sequential:
                        _mediaPlayer.IsLoopingEnabled = false;
                        _isShuffleEnabled = false;
                        Debug.WriteLine("设置为顺序播放模式");
                        break;
                    case PlaybackMode.SingleRepeat:
                        _mediaPlayer.IsLoopingEnabled = true;
                        _isShuffleEnabled = false;
                        Debug.WriteLine("设置为单曲循环模式");
                        break;
                    case PlaybackMode.ListRepeat:
                        _mediaPlayer.IsLoopingEnabled = false;
                        _isShuffleEnabled = false;
                        Debug.WriteLine("设置为列表循环模式");
                        break;
                    case PlaybackMode.Random:
                        _mediaPlayer.IsLoopingEnabled = false;
                        _isShuffleEnabled = true;
                        Debug.WriteLine("设置为随机播放模式");
                        break;
                }

                // 触发播放模式变更事件
                PlayModeChanged?.Invoke(_playMode, _isShuffleEnabled);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新播放模式出错: {ex.Message}");
            }
        }
        private async Task LoadLastPlayedMusicAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_settings.LastPlayedMusicPath) &&
                    File.Exists(_settings.LastPlayedMusicPath))
                {
                    var music = await MusicInfo.CreateFromFileAsync(_settings.LastPlayedMusicPath);

                    // 只加载音乐信息，但不自动播放
                    _currentMusic = music;
                    CurrentMusicChanged?.Invoke(music);

                    // 准备媒体源但不播放
                    var storageFile = await StorageFile.GetFileFromPathAsync(music.FilePath);
                    var mediaSource = MediaSource.CreateFromStorageFile(storageFile);
                    _mediaPlayer.Source = mediaSource;

                    // 设置上次的播放位置
                    TimeSpan position = TimeSpan.Zero;
                    if (_settings.LastPlaybackPosition > 0)
                    {
                        position = TimeSpan.FromSeconds(_settings.LastPlaybackPosition);
                        _mediaPlayer.PlaybackSession.Position = position;
                        Debug.WriteLine($"设置播放位置: {position}");
                    }
                    // 从数据库获取最新的收藏状态
                    UpdateMusicFavoriteStatus(music);

                    _currentMusic = music;
                    CurrentMusicChanged?.Invoke(music);
                    // 不调用 _mediaPlayer.Play()
                    _isPlaying = false;
                    PlaybackStateChanged?.Invoke(false);

                    // 手动触发位置更新事件，确保UI显示正确的时间
                    PositionChanged?.Invoke(position);
                    
                    // 更新SMTC信息
                    UpdateSystemMediaTransportControls(music, false);

                    Debug.WriteLine($"已加载上次播放的音乐: {music.Title}，位置: {position}，播放模式: {_playMode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载上次播放的音乐失败: {ex.Message}");
            }
        }


        public TimeSpan CurrentTime => _currentTime;
        public MusicInfo CurrentMusic => _currentMusic;
        public double Position
        {
            get
            {
                try
                {
                    if (_mediaPlayer?.PlaybackSession != null)
                    {
                        return _mediaPlayer.PlaybackSession.Position.TotalSeconds;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"获取播放位置时出错: {ex.Message}");
                }
                return 0;
            }
        }
        public MusicPlayer()
        {
            _settings = AppSettings.Load();

            // 创建MediaPlayer实例
            _mediaPlayer = new MediaPlayer();
            
            // 获取播放队列服务
            _playQueueService = PlayQueueService.Instance;
            // 监听队列加载完成事件
            _playQueueService.QueueLoaded += (s, e) =>
            {
                // 如果有当前播放的歌曲，恢复播放
                var currentMusic = _playQueueService.GetCurrent();
                if (currentMusic != null)
                {
                    // 可以选择自动播放或只更新当前歌曲信息
                    _currentMusic = currentMusic;
                    // 如果需要自动播放，取消下面的注释
                };
            };
            // 从设置中加载播放模式 - 将字符串转换为枚举
            if (Enum.TryParse(_settings.PlayMode, out PlaybackMode mode))
            {
                _playMode = mode;
            }
            else
            {
                _playMode = PlaybackMode.Sequential; // 默认值
            }

            // 从设置中加载随机播放状态
            _isShuffleEnabled = _settings.IsShuffleEnabled;

            // 确保播放模式和随机播放状态一致
            if (_isShuffleEnabled && _playMode != PlaybackMode.Random)
            {
                _playMode = PlaybackMode.Random;
            }
            else if (!_isShuffleEnabled && _playMode == PlaybackMode.Random)
            {
                _playMode = PlaybackMode.Sequential;
            }

            // 从设置中加载音量
            _volume = _settings.Volume;
            _lastVolume = _volume;
            _mediaPlayer.Volume = _volume / 100.0;

            // 立即应用播放模式设置
            UpdatePlayMode();
            Debug.WriteLine($"从设置加载播放模式: {_playMode}");

            // 设置音量
            _mediaPlayer.Volume = _settings.Volume / 100.0;

            // 初始化定时器
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)  // 更频繁地更新
            };

            _positionTimer.Tick += (s, e) =>
            {
                try
                {
                    if (_mediaPlayer.PlaybackSession != null && _isPlaying)
                    {
                        var position = _mediaPlayer.PlaybackSession.Position;
                        var duration = _currentMusic?.Duration ?? TimeSpan.Zero;
                        
                        // 触发位置更新事件
                        PositionChanged?.Invoke(position);
                        
                        // 如果当前正在处理MediaEnded，跳过额外检查
                        if (_isHandlingMediaEnded)
                            return;
                        
                        // 降低检查频率：每秒最多检查一次结束状态
                        var now = DateTime.Now;
                        if ((now - _lastPositionCheckTime).TotalSeconds >= 1)
                        {
                            _lastPositionCheckTime = now;
                            
                            // 仅在必要时执行结束检查
                            if (duration.TotalSeconds > 0 && position.TotalSeconds > 0)
                            {
                                // 特殊保护：对于短歌（小于10秒），如果播放位置已经接近结束，提前触发结束处理
                                if (duration.TotalSeconds < 10 && position.TotalSeconds >= duration.TotalSeconds - 0.5)
                                {
                                    Debug.WriteLine($"短歌特殊保护：检测到短歌即将结束：{position.TotalSeconds}/{duration.TotalSeconds}");
                                    
                                    // 设置标记，避免重复触发
                                    _isHandlingMediaEnded = true;
                                    
                                    // 主动处理为结束
                                    HandleMediaEnded();
                                    return;
                                }
                                
                                // 检查是否播放到了歌曲末尾但没有触发MediaEnded事件
                                if (position.TotalSeconds >= duration.TotalSeconds - 1.5)
                                {
                                    Debug.WriteLine($"检测到歌曲接近结束：{position.TotalSeconds}/{duration.TotalSeconds}");
                                    
                                    // 设置标记，避免重复触发
                                    _isHandlingMediaEnded = true;
                                    
                                    // 如果接近歌曲末尾，主动处理为结束
                                    HandleMediaEnded();
                                    return; // 提前返回，避免执行下面的代码
                                }
                                
                                // 检查播放位置是否异常（超过歌曲时长）
                                if (position.TotalSeconds > duration.TotalSeconds + 2)
                                {
                                    Debug.WriteLine($"检测到异常播放位置：{position.TotalSeconds}秒，超过歌曲时长{duration.TotalSeconds}秒");
                                    
                                    // 强制结束当前播放并切换到下一首
                                    _isHandlingMediaEnded = true;
                                    HandleMediaEnded();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"更新播放位置出错: {ex.Message}");
                }
            };

            // 设置播放状态变化事件
            _mediaPlayer.PlaybackSession.PlaybackStateChanged += (sender, args) =>
            {
                _isPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
                PlaybackStateChanged?.Invoke(_isPlaying);

                if (_isPlaying)
                {
                    _positionTimer.Start();
                }
                else
                {
                    _positionTimer.Stop();
                }
                
                // 更新SMTC播放状态
                UpdateSystemMediaPlaybackStatus(_isPlaying);
            };
            
            // 设置媒体结束事件
            _mediaPlayer.MediaEnded += async (sender, args) =>
            {
                try
                {
                    Debug.WriteLine("系统MediaEnded事件触发");
                    // 使用统一的处理方法
                    HandleMediaEnded();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"处理系统媒体结束事件时出错: {ex.Message}");
                }
            };
            
            // 初始化SMTC
            InitializeSystemMediaTransportControls();
            
            // 尝试加载上次播放的音乐
            _ = LoadLastPlayedMusicAsync();
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender,object args)
        {
            _currentTime = sender.Position;
            _progress = _currentTime.TotalSeconds;
            PositionChanged?.Invoke(_currentTime);
        }
        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            var isPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
            PlaybackStateChanged?.Invoke(isPlaying);
        }
        public async Task PlayAsync(MusicInfo music)
        {
            try
            {
                if (music == null)
                {
                    Debug.WriteLine("无法播放：音乐信息为空");
                    return;
                }

                // 保存当前播放位置
                if (_currentMusic != null && _mediaPlayer?.PlaybackSession != null)
                {
                    _settings.LastPlaybackPosition = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
                    _settings.Save();
                }

                // 设置新的音乐
                _currentMusic = music;

                // 保存最后播放的音乐路径
                _settings.LastPlayedMusicPath = music.FilePath;
                _settings.Save();

                // 从数据库获取最新的收藏状态
                UpdateMusicFavoriteStatus(music);

                try
                {
                    // 步骤1：停止当前播放并清除媒体源
                    _mediaPlayer.Pause();
                    _mediaPlayer.Source = null;

                    // 步骤2：等待一小段时间确保清除生效
                    await Task.Delay(50);

                    // 步骤3：准备媒体源
                    var storageFile = await StorageFile.GetFileFromPathAsync(music.FilePath);
                    var mediaSource = MediaSource.CreateFromStorageFile(storageFile);

                    // 步骤4：设置新媒体源
                    _mediaPlayer.Source = mediaSource;

                    // 步骤5：确保从头开始播放 - 非常重要
                    // 由于Source变更会重置PlaybackSession，我们等待新的PlaybackSession初始化
                    await Task.Delay(100);
                    if (_mediaPlayer.PlaybackSession != null)
                    {
                        _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                        Debug.WriteLine("确保从头开始播放");
                    }

                    // 步骤6：开始播放
                    _mediaPlayer.Play();

                    // 步骤7：再次检查播放位置，确保从头开始播放
                    await Task.Delay(200);
                    if (_mediaPlayer.PlaybackSession != null &&
                        _mediaPlayer.PlaybackSession.Position.TotalSeconds > 0.5)
                    {
                        _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                        Debug.WriteLine("播放开始后再次重置位置");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"设置媒体源或播放时出错: {ex.Message}");
                    // 尝试恢复播放
                    try { _mediaPlayer.Play(); } catch { }
                }

                // 确保设置播放状态为播放
                _isPlaying = true;
                PlaybackStateChanged?.Invoke(true);

                // 通知当前音乐已更改
                CurrentMusicChanged?.Invoke(music);

                // 更新SMTC信息
                UpdateSystemMediaTransportControls(music, true);

                // 更新播放队列中的当前索引
                int index = _playQueueService.GetIndexOf(music);
                if (index >= 0)
                {
                    _playQueueService.SetCurrentIndex(index);
                }

                Debug.WriteLine($"开始播放: {music.Title}, 时长: {music.Duration.TotalSeconds}秒");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放音乐时出错: {ex.Message}");
            }
        }
        private void SavePlaybackPosition()
        {
            if (_mediaPlayer?.PlaybackSession != null && _currentMusic != null)
            {
                _settings.LastPlaybackPosition = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
                _settings.LastPlayedMusicPath = _currentMusic.FilePath;
                _settings.Save();

                Debug.WriteLine($"保存播放位置: {_settings.LastPlaybackPosition}秒");
            }
        }
        public void UpdatePosition()
        {
            if (_mediaPlayer?.PlaybackSession != null)
            {
                var position = _mediaPlayer.PlaybackSession.Position;
                PositionChanged?.Invoke(position);
                Debug.WriteLine($"手动更新位置: {position}");
            }
        }
        public void SetPosition(TimeSpan position)
        {
            _mediaPlayer.PlaybackSession.Position = position;
        }
        public void Pause()
        {
            SavePlaybackPosition();
            _mediaPlayer.Pause();
            _isPlaying = false;
            PlaybackStateChanged?.Invoke(false);
            
            // 更新SMTC播放状态
            UpdateSystemMediaPlaybackStatus(false);

            // 停止定时器
            _positionTimer.Stop();
            Debug.WriteLine("暂停播放，停止定时器");
        }

        public void Resume()
        {
            _mediaPlayer?.Play();
            _isPlaying = true;
            PlaybackStateChanged?.Invoke(true);
            
            // 更新SMTC播放状态
            UpdateSystemMediaPlaybackStatus(true);

            // 确保定时器启动
            if (!_positionTimer.IsEnabled)
            {
                _positionTimer.Start();
                Debug.WriteLine("恢复播放，启动定时器");
            }
        }

        public void Stop()
        {
            _mediaPlayer?.Pause();
            _mediaPlayer.Source = null;
            _currentMusic = null;
            CurrentMusicChanged?.Invoke(null);
            // 触发状态变更事件
            PlaybackStateChanged?.Invoke(false);
            
            // 清除SMTC信息
            ClearSystemMediaTransportControls();
        }
        public void SetVolume(double volume)
        {
            if (volume < 0) volume = 0;
            if (volume > 1) volume = 1;

            _mediaPlayer.Volume = volume;
        }
        // 确保在应用关闭时保存状态
        public void Shutdown()
        {
            SavePlaybackPosition();
            _settings.PlayMode = _playMode.ToString(); // 将枚举转换为字符串
            _settings.Save();

            Debug.WriteLine($"应用关闭时保存状态：位置={_settings.LastPlaybackPosition}秒，模式={_settings.PlayMode}");

            _positionTimer?.Stop();
            
            // 清除SMTC信息
            ClearSystemMediaTransportControls();
        }
        // 更新歌曲的喜欢状态
        public void UpdateCurrentMusicFavoriteState()
        {
            if (_currentMusic != null)
            {
                UpdateMusicFavoriteStatus(_currentMusic);
            }
        }
        // 从数据库获取音乐的最新收藏状态
        private async void UpdateMusicFavoriteStatus(MusicInfo music)
        {
            try
            {
                var databaseService = App.Services.GetService<DatabaseService>();
                if (databaseService != null)
                {
                    // 获取最新的收藏状态
                    bool isFavorite = await databaseService.IsMusicFavoriteAsync(music.FilePath);
                    music.IsFavorite = isFavorite;
                    System.Diagnostics.Debug.WriteLine($"更新音乐收藏状态: {music.Title}, 收藏: {isFavorite}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取音乐收藏状态出错: {ex.Message}");
            }
        }
        
        #region SMTC相关方法
        
        // 初始化系统媒体传输控件
        private void InitializeSystemMediaTransportControls()
        {
            try
            {
                // 获取SMTC实例
                _systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
                if (_systemMediaControls == null)
                {
                    Debug.WriteLine("无法获取当前视图的SMTC实例");
                    return;
                }
                
                _displayUpdater = _systemMediaControls.DisplayUpdater;
                
                // 启用SMTC功能
                _systemMediaControls.IsEnabled = true;
                _systemMediaControls.IsPlayEnabled = true;
                _systemMediaControls.IsPauseEnabled = true;
                _systemMediaControls.IsNextEnabled = true;
                _systemMediaControls.IsPreviousEnabled = true;
                
                // 设置媒体类型
                _displayUpdater.Type = MediaPlaybackType.Music;
                
                // 注册按钮点击事件
                _systemMediaControls.ButtonPressed += SystemMediaControls_ButtonPressed;
                
                Debug.WriteLine("系统媒体传输控件初始化完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化系统媒体传输控件出错: {ex.Message}");
            }
        }
        
        // 处理SMTC按钮点击事件
        private void SystemMediaControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            try
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        Resume();
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        Pause();
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        _ = PlayNextAsync();
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        _ = PlayPreviousAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理系统媒体控制按钮点击事件出错: {ex.Message}");
            }
        }

        // 播放下一首
        public async Task<bool> PlayNextAsync(bool loopIfEnd = false)
        {
            try
            {
                if (_playQueueService.PlayQueue == null || _playQueueService.PlayQueue.Count == 0)
                {
                    Debug.WriteLine("播放队列为空，无法播放下一首");
                    return false;
                }

                // 关键修复：在获取下一首歌之前，先强制重置当前播放位置和媒体源
                if (_mediaPlayer != null)
                {
                    try
                    {
                        // 1. 暂停当前播放
                        _mediaPlayer.Pause();

                        // 2. 最彻底的方法是直接清除媒体源
                        _mediaPlayer.Source = null;

                        // 3. 等待一小段时间确保清除生效
                        await Task.Delay(50);

                        Debug.WriteLine("切换歌曲前完全重置播放器状态");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"重置播放器状态时出错: {ex.Message}");
                    }
                }

                // 获取下一首歌曲
                MusicInfo nextMusic = null;

                if (_isShuffleEnabled)
                {
                    nextMusic = _playQueueService.GetRandom();
                }
                else
                {
                    nextMusic = _playQueueService.GetNext(loopIfEnd);
                }

                if (nextMusic == null)
                {
                    Debug.WriteLine("没有下一首歌曲可播放");
                    return false;
                }

                // 播放下一首歌曲，确保从头开始播放
                await PlayAsync(nextMusic);

                // 额外检查：确保播放位置被重置
                if (_mediaPlayer?.PlaybackSession != null)
                {
                    _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放下一首歌曲时出错: {ex.Message}");
                return false;
            }
        }

        // 播放上一首
        public async Task<bool> PlayPreviousAsync()
        {
            try
            {
                // 先重置播放器状态
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Pause();
                    _mediaPlayer.Source = null;
                    await Task.Delay(50);
                }

                // 获取上一首歌曲
                MusicInfo prevMusic = _isShuffleEnabled ?
                    _playQueueService.GetRandom() :
                    _playQueueService.GetPrevious();

                if (prevMusic == null)
                {
                    Debug.WriteLine("没有上一首歌曲可播放");
                    return false;
                }

                // 播放上一首歌曲
                await PlayAsync(prevMusic);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放上一首歌曲时出错: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> PlayRandomAsync()
        {
            try
            {
                // 先重置播放器状态
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Pause();
                    _mediaPlayer.Source = null;
                    await Task.Delay(50);
                }

                // 获取随机歌曲
                MusicInfo randomMusic = _playQueueService.GetRandom();

                if (randomMusic == null)
                {
                    Debug.WriteLine("没有随机歌曲可播放");
                    return false;
                }

                // 播放随机歌曲
                await PlayAsync(randomMusic);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放随机歌曲时出错: {ex.Message}");
                return false;
            }
        }
        // 更新SMTC信息
        private async void UpdateSystemMediaTransportControls(MusicInfo music, bool isPlaying)
        {
            if (music == null || _systemMediaControls == null || _displayUpdater == null)
                return;
                
            try
            {
                // 清除之前的信息
                _displayUpdater.ClearAll();
                
                // 设置音乐信息
                _displayUpdater.MusicProperties.Title = music.Title;
                _displayUpdater.MusicProperties.Artist = music.Artist;
                _displayUpdater.MusicProperties.AlbumTitle = music.Album;
                
                // 设置专辑封面
                if (music.AlbumArt != null && music.AlbumArt.Length > 0)
                {
                    try
                    {
                        // 创建内存流
                        var stream = new InMemoryRandomAccessStream();
                        // 使用WindowsRuntimeBufferExtensions将byte[]转换为IBuffer
                        await stream.WriteAsync(music.AlbumArt.AsBuffer());
                        stream.Seek(0);
                        
                        // 设置缩略图
                        _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromStream(stream);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"设置SMTC专辑封面出错: {ex.Message}");
                    }
                }
                
                // 更新显示
                _displayUpdater.Update();
                
                // 更新播放状态
                UpdateSystemMediaPlaybackStatus(isPlaying);
                
                Debug.WriteLine($"已更新系统媒体传输控件信息: {music.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新系统媒体传输控件信息出错: {ex.Message}");
            }
        }
        
        // 更新SMTC播放状态
        private void UpdateSystemMediaPlaybackStatus(bool isPlaying)
        {
            if (_systemMediaControls == null)
                return;
                
            try
            {
                // 设置播放状态
                _systemMediaControls.PlaybackStatus = isPlaying 
                    ? MediaPlaybackStatus.Playing 
                    : MediaPlaybackStatus.Paused;
                    
                Debug.WriteLine($"已更新系统媒体传输控件播放状态: {(isPlaying ? "播放中" : "已暂停")}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新系统媒体传输控件播放状态出错: {ex.Message}");
            }
        }
        
        // 清除SMTC信息
        private void ClearSystemMediaTransportControls()
        {
            if (_systemMediaControls == null || _displayUpdater == null)
                return;
                
            try
            {
                // 清除所有信息
                _displayUpdater.ClearAll();
                _displayUpdater.Update();
                
                // 设置为已停止状态
                _systemMediaControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                
                Debug.WriteLine("已清除系统媒体传输控件信息");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清除系统媒体传输控件信息出错: {ex.Message}");
            }
        }

        #endregion

        // 处理媒体结束
        private void HandleMediaEnded()
        {
            try
            {
                // 设置标记，防止重复触发
                _isHandlingMediaEnded = true;

                Debug.WriteLine("处理媒体结束事件");

                // 触发播放完成事件
                PlaybackCompleted?.Invoke();

                // 根据播放模式处理
                switch (_playMode)
                {
                    case PlaybackMode.Sequential:
                        // 顺序播放，播放下一首
                        _ = PlayNextAsync();
                        break;
                    case PlaybackMode.SingleRepeat:
                        // 单曲循环，重新播放当前歌曲
                        if (_currentMusic != null)
                        {
                            // 重置播放位置并重新播放
                            if (_mediaPlayer?.PlaybackSession != null)
                            {
                                _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                            }
                            _mediaPlayer.Play();
                        }
                        break;
                    case PlaybackMode.ListRepeat:
                        // 列表循环，播放下一首，如果是最后一首则从头开始
                        _ = PlayNextAsync(true);
                        break;
                    case PlaybackMode.Random:
                        // 随机播放
                        _ = PlayRandomAsync();
                        break;
                }

                // 延迟重置标记，确保处理完成
                Task.Delay(500).ContinueWith(_ => _isHandlingMediaEnded = false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理媒体结束事件时出错: {ex.Message}");
                _isHandlingMediaEnded = false;
            }
        }
    }
}

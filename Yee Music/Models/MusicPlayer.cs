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
                }
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
                    if (_mediaPlayer.PlaybackSession != null)
                    {
                        var position = _mediaPlayer.PlaybackSession.Position;
                        PositionChanged?.Invoke(position);
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
            };
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
        public async Task PlayAsync(MusicInfo music, double startPosition = 0)
        {
            if (music == null) return;

            try
            {
                _currentMusic = music;
                CurrentMusicChanged?.Invoke(music);

                var storageFile = await StorageFile.GetFileFromPathAsync(music.FilePath);
                var mediaSource = MediaSource.CreateFromStorageFile(storageFile);
                _mediaPlayer.Source = mediaSource;

                // 设置开始位置
                if (startPosition > 0)
                {
                    _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(startPosition);
                }

                _mediaPlayer.Play();
                // 确保触发播放状态变化事件
                _isPlaying = true;
                PlaybackStateChanged?.Invoke(true);

                // 确保定时器启动
                if (!_positionTimer.IsEnabled)
                {
                    _positionTimer.Start();
                    Debug.WriteLine("开始播放，启动定时器");
                }

                // 保存当前播放的音乐路径
                _settings.LastPlayedMusicPath = music.FilePath;
                _settings.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放出错: {ex.Message}");
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

            // 停止定时器
            _positionTimer.Stop();
            Debug.WriteLine("暂停播放，停止定时器");
        }

        public void Resume()
        {
            _mediaPlayer?.Play();
            _isPlaying = true;
            PlaybackStateChanged?.Invoke(true);

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
    }
}

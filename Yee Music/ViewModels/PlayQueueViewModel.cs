using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yee_Music.Models;
using Yee_Music.Services;

namespace Yee_Music.ViewModels
{
    public class PlayQueueViewModel : ObservableRecipient
    {
        private readonly MusicPlayer _player;
        private PlayQueueService _playQueueService;
        private MusicInfo _currentPlayingMusic;

        public ObservableCollection<MusicInfo> PlayQueue => PlayQueueService.Instance.PlayQueue;
        public bool IsQueueEmpty => PlayQueue.Count == 0;

        public MusicInfo CurrentPlayingMusic
        {
            get => _currentPlayingMusic;
            set => SetProperty(ref _currentPlayingMusic, value);
        }
        private IRelayCommand<MusicInfo> _playMusicCommand;
        public IRelayCommand<MusicInfo> PlayMusicCommand { get; }
        public IRelayCommand ClearQueueCommand { get; }
        public IRelayCommand RemoveMusicCommand { get; }
        public void RefreshProperties()
        {
            OnPropertyChanged(nameof(PlayQueue));
            OnPropertyChanged(nameof(IsQueueEmpty));
        }

        public PlayQueueViewModel(MusicPlayer player)
        {
            _player = player;
            _playQueueService = PlayQueueService.Instance;

            // 监听播放队列变化
            _playQueueService.PlayQueue.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(PlayQueue));
                OnPropertyChanged(nameof(IsQueueEmpty));
            };

            // 监听队列加载完成事件
            _playQueueService.QueueLoaded += (s, e) =>
            {
                OnPropertyChanged(nameof(PlayQueue));
                OnPropertyChanged(nameof(IsQueueEmpty));
            };

            // 初始化命令
            PlayMusicCommand = new RelayCommand<MusicInfo>(PlayMusic);
            ClearQueueCommand = new RelayCommand(ClearQueue);
            RemoveMusicCommand = new RelayCommand<MusicInfo>(RemoveMusic);

            // 订阅播放器事件
            _player.CurrentMusicChanged += OnCurrentMusicChanged;

            // 初始化当前播放的音乐
            CurrentPlayingMusic = PlayQueueService.Instance.GetCurrent();
        }

        private void OnCurrentMusicChanged(MusicInfo music)
        {
            CurrentPlayingMusic = music;
            Debug.WriteLine($"播放列表视图模型：当前播放音乐已更改为 {music?.Title ?? "无"}");
        }

        private void PlayMusic(MusicInfo music)
        {
            if (music != null)
            {
                _player.PlayAsync(music);
                // 更新播放列表服务中的当前索引
                int index = PlayQueue.IndexOf(music);
                if (index >= 0)
                {
                    PlayQueueService.Instance.SetCurrentIndex(index);
                }
            }
        }

        private void ClearQueue()
        {
            _playQueueService.ClearQueue();
            OnPropertyChanged(nameof(IsQueueEmpty));
        }

        private void RemoveMusic(MusicInfo music)
        {
            if (music != null)
            {
                _playQueueService.RemoveFromQueue(music);
                OnPropertyChanged(nameof(IsQueueEmpty));
            }
        }
    }
}

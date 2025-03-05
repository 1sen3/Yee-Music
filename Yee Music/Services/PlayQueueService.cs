using System;
using System.Collections.ObjectModel;
using System.Linq;
using Yee_Music.Models;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Yee_Music.Services
{
    public class PlayQueueService
    {
        private static PlayQueueService _instance;
        public static PlayQueueService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PlayQueueService();
                }
                return _instance;
            }
        }
        public ObservableCollection<MusicInfo> PlayQueue { get; private set; }
        public int CurrentIndex { get; private set; } = -1;
        private readonly DatabaseService _databaseService;
        private bool _isInitialized = false;
        public event EventHandler QueueLoaded;
        private PlayQueueService()
        {
            Debug.WriteLine("创建 PlayQueueService 实例");
            PlayQueue = new ObservableCollection<MusicInfo>();
            _databaseService = App.Services.GetService<DatabaseService>();
        }
        public async Task InitializeAsync()
        {
            // 防止重复初始化
            if (_isInitialized)
            {
                Debug.WriteLine("PlayQueueService 已经初始化，跳过");
                return;
            }

            Debug.WriteLine("PlayQueueService 开始初始化...");

            if (_databaseService != null)
            {
                Debug.WriteLine("数据库服务已获取，开始加载播放队列...");
                await LoadQueueFromDatabaseAsync();
                _isInitialized = true;

                // 通知队列已加载完成
                Debug.WriteLine($"播放队列加载完成，共 {PlayQueue.Count} 首歌曲，当前索引: {CurrentIndex}");
                QueueLoaded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Debug.WriteLine("警告: 无法获取数据库服务，播放队列将不会持久化");
                _isInitialized = true;
            }
        }
        private async Task LoadQueueFromDatabaseAsync()
        {
            try
            {
                var (queue, index) = await _databaseService.LoadPlayQueueAsync();

                PlayQueue.Clear();
                foreach (var music in queue)
                {
                    PlayQueue.Add(music);
                }

                CurrentIndex = index;
                Debug.WriteLine($"从数据库加载播放队列成功，共 {PlayQueue.Count} 首歌曲");

                // 如果有当前播放的音乐，确保更新其收藏状态
                var currentMusic = GetCurrent();
                if (currentMusic != null && App.MusicPlayer != null)
                {
                    try
                    {
                        // 使用正确的方法名称
                        App.MusicPlayer.UpdateCurrentMusicFavoriteState();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"更新收藏状态出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"从数据库加载播放队列失败: {ex.Message}");
            }
        }
        private async Task SaveQueueToDatabaseAsync()
        {
            if (_databaseService != null && _isInitialized)
            {
                try
                {
                    await _databaseService.SavePlayQueueAsync(PlayQueue.ToList(), CurrentIndex);
                    Debug.WriteLine("播放队列已保存到数据库");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"保存播放队列到数据库失败: {ex.Message}");
                }
            }
        }
        public void SetCurrentIndex(int index)
        {
            if (index >= 0 && index < PlayQueue.Count)
            {
                CurrentIndex = index;
                Debug.WriteLine($"设置当前播放索引为 {index}");

                // 保存到数据库
                SaveQueueToDatabaseAsync().ConfigureAwait(false);
            }
        }
        // 添加单首歌曲到播放列表
        public void AddToQueue(MusicInfo music)
        {
            PlayQueue.Add(music);
            Debug.WriteLine($"添加歌曲到播放列表: {music.Title}");

            SaveQueueToDatabaseAsync().ConfigureAwait(false);
        }
        // 清空当前播放列表并添加新的音乐列表
        public void SetQueue(IEnumerable<MusicInfo> musicList, MusicInfo currentMusic = null)
        {
            PlayQueue.Clear();
            foreach (var music in musicList)
            {
                PlayQueue.Add(music);
            }

            if (currentMusic != null)
            {
                CurrentIndex = PlayQueue.IndexOf(currentMusic);
            }
            else
            {
                CurrentIndex = PlayQueue.Count > 0 ? 0 : -1;
            }

            Debug.WriteLine($"设置新的播放列表，共 {PlayQueue.Count} 首歌曲");
            // 保存到数据库
            SaveQueueToDatabaseAsync().ConfigureAwait(false);
        }
        // 获取下一首歌
        public MusicInfo GetNext()
        {
            if (PlayQueue.Count == 0) return null;

            CurrentIndex++;
            if (CurrentIndex >= PlayQueue.Count)
            {
                CurrentIndex = 0;
            }

            Debug.WriteLine($"切换到下一首: {PlayQueue[CurrentIndex].Title}");

            // 保存到数据库
            SaveQueueToDatabaseAsync().ConfigureAwait(false);

            return PlayQueue[CurrentIndex];
        }

        // 获取上一首歌
        public MusicInfo GetPrevious()
        {
            if (PlayQueue.Count == 0) return null;

            CurrentIndex--;
            if (CurrentIndex < 0)
            {
                CurrentIndex = PlayQueue.Count - 1;
            }

            Debug.WriteLine($"切换到上一首: {PlayQueue[CurrentIndex].Title}");

            // 保存到数据库
            SaveQueueToDatabaseAsync().ConfigureAwait(false);

            return PlayQueue[CurrentIndex];
        }

        // 获取当前播放的歌曲
        public MusicInfo GetCurrent()
        {
            if (CurrentIndex >= 0 && CurrentIndex < PlayQueue.Count)
            {
                return PlayQueue[CurrentIndex];
            }
            return null;
        }
        // 从播放列表中移除音乐
        public void RemoveFromQueue(MusicInfo music)
        {
            int index = PlayQueue.IndexOf(music);
            if (index >= 0)
            {
                // 如果要移除的是当前播放的音乐
                if (index == CurrentIndex)
                {
                    // 如果是最后一首，则索引减一
                    if (index == PlayQueue.Count - 1)
                    {
                        CurrentIndex--;
                    }
                    // 否则保持索引不变，下一首会自动前移
                }
                // 如果要移除的音乐在当前播放的音乐之前，需要调整当前索引
                else if (index < CurrentIndex)
                {
                    CurrentIndex--;
                }

                PlayQueue.Remove(music);
                Debug.WriteLine($"从播放列表中移除音乐: {music.Title}");

                // 保存到数据库
                SaveQueueToDatabaseAsync().ConfigureAwait(false);
            }
        }

        // 清空播放列表
        public void ClearQueue()
        {
            PlayQueue.Clear();
            CurrentIndex = -1;
            Debug.WriteLine("清空播放列表");

            SaveQueueToDatabaseAsync().ConfigureAwait(false);
        }
    }
}

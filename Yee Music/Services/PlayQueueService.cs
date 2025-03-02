using System;
using System.Collections.ObjectModel;
using System.Linq;
using Yee_Music.Models;
using System.Diagnostics;
using System.Collections.Generic;

namespace Yee_Music.Services
{
    public class PlayQueueService
    {
        private static PlayQueueService _instance;
        public static PlayQueueService Instance => _instance ??= new PlayQueueService();
        public ObservableCollection<MusicInfo> PlayQueue { get; private set; }
        public int CurrentIndex { get; private set; } = -1;
        private PlayQueueService()
        {
            PlayQueue = new ObservableCollection<MusicInfo>();
        }
        public void SetCurrentIndex(int index)
        {
            if (index >= 0 && index < PlayQueue.Count)
            {
                CurrentIndex = index;
                Debug.WriteLine($"设置当前播放索引为 {index}");
            }
        }
        // 添加单首歌曲到播放列表
        public void AddToQueue(MusicInfo music)
        {
            PlayQueue.Add(music);
            Debug.WriteLine($"添加歌曲到播放列表: {music.Title}");
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
                CurrentIndex = 0;
            }

            Debug.WriteLine($"设置新的播放列表，共 {PlayQueue.Count} 首歌曲");
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
        // 添加从播放列表中移除音乐的方法
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
            }
        }

        // 添加清空播放列表的方法
        public void ClearQueue()
        {
            PlayQueue.Clear();
            CurrentIndex = -1;
            Debug.WriteLine("清空播放列表");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using TagLib;
using System.IO;
using Windows.Storage;
using System.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Formats.Tar;
using Yee_Music.Services;

namespace Yee_Music.Models
{
    public class MusicInfo:ObservableObject
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string FilePath { get; set; }
        public TimeSpan Duration { get; set; }
        // 延迟加载的属性
        private byte[] _albumArt;
        private bool _albumArtLoaded = false;
        public byte[] AlbumArt
        {
            get
            {
                if (!_albumArtLoaded && !string.IsNullOrEmpty(FilePath))
                {
                    _albumArt = LoadAlbumArt();
                    _albumArtLoaded = true;
                }
                return _albumArt;
            }
            set
            {
                _albumArt = value;
                _albumArtLoaded = true;
            }
        }
        private BitmapImage _coverImage;
        public BitmapImage CoverImage
        {
            get => _coverImage;
            set => SetProperty(ref _coverImage, value);
        }
        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged(nameof(IsFavorite));
                }
            }
        }
        public static async Task<MusicInfo> CreateFromFileAsync(string filePath)
        {
            var file = new MusicInfo { FilePath = filePath };

            try
            {
                TagLib.File tagFile = null;
                // 使用 TagLib 读取音频文件元数据
                using (tagFile = TagLib.File.Create(filePath))
                {
                    file.Title = string.IsNullOrEmpty(tagFile.Tag.Title)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : tagFile.Tag.Title;
                    file.Artist = string.Join(", ", tagFile.Tag.Performers);
                    file.Album = tagFile.Tag.Album;
                    file.Duration = tagFile.Properties.Duration;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading file metadata: {ex.Message}");
                // 如果无法读取元数据，至少显示文件名
                file.Title = Path.GetFileNameWithoutExtension(filePath);
            }

            return file;
        }
        // 确保 LoadAlbumArt 方法正确实现
        public byte[] LoadAlbumArt()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"加载专辑封面: {FilePath}");
                using (var file = TagLib.File.Create(FilePath))
                {
                    if (file.Tag.Pictures.Length > 0)
                    {
                        return file.Tag.Pictures[0].Data.Data;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载专辑封面出错: {ex.Message}");
            }
            return null;
        }
    }
}

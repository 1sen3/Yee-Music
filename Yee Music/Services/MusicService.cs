using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.WebUI;
using Yee_Music.Models;

namespace Yee_Music.Services
{
    public class MusicService
    {
        public List<MusicInfo>LoadMusicFile(string path)
        {
            var musicFiles= new List<MusicInfo>();
            var supportedExtensions = new[] { ".mp3", ".wav", ".flac", ".m4a" };
            foreach(var file in Directory.GetFiles(path))
            {
                if (supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                {
                    MusicInfo musicInfo = new MusicInfo()
                    {
                        FilePath = file,
                    };
                    musicFiles.Add(musicInfo);
                }
            }
            return musicFiles;
        }
        public void SaveMusicLibrary(string path, List<MusicInfo> musicFiles)
        {
            var json=System.Text.Json.JsonSerializer.Serialize(musicFiles);
            File.WriteAllText(path, json);
        }
        public List<MusicInfo> LoadMusicLibrary(string path)
        {
            if (File.Exists(path))
            {
                var json=File.ReadAllText(path);
                return System.Text.Json.JsonSerializer.Deserialize<List<MusicInfo>>(json);
            }
            return new List<MusicInfo>();   
        }
    }
}

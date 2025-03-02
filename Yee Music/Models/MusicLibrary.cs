using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using System.IO;
using Yee_Music.Models;
using Yee_Music.Helpers;
using TagLib;
using System.Threading;
using System.Diagnostics;

namespace Yee_Music.Models;

public class MusicLibrary : ObservableObject
{
    private readonly string[] SupportedExtensions = { ".mp3", ".flac", ".m4a", ".wav", ".wma", ".aac", ".ogg" };
    public async Task<List<MusicInfo>> LoadMusicFileAysnc(string libraryPath)
    {
        var musicFiles = new List<MusicInfo>();
        if(string.IsNullOrEmpty(libraryPath) || !Directory.Exists(libraryPath))
        {
            return musicFiles;
        }
        var files = Directory.GetFiles(libraryPath, "*.*",SearchOption.AllDirectories).Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLower()));
        foreach(var file in files)
        {
            var musicInfo=await MusicInfo.CreateFromFileAsync(file);
            musicFiles.Add(musicInfo);
        }
        return musicFiles;
    }
}

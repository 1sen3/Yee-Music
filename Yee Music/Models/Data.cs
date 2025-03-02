using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using Yee_Music.Helpers;
using Yee_Music.ViewModels;
using Yee_Music.Pages;

namespace Yee_Music.Models;

public class Data
{
    public static bool NotFirstUsed { get; set; } = false;
    public static string? SelectedAlbum
    {
        get; set;
    }
    public static string? SelectedArtist
    {
        get; set;
    }
    /// <summary>
    /// 软件显示名称
    /// </summary>
    public static readonly string AppDisplayName = "Yee Music";
    /// <summary>
    /// 软件语言
    /// </summary>
    public static readonly string Language = "zh-cn";

    /// <summary>
    /// 播放器支持的音频文件类型
    /// </summary>
    public static readonly string[] SupportedAudioTypes = [".flac", ".wav", ".m4a", ".aac", ".mp3", ".wma", ".ogg", ".oga", ".opus"];
    public static MusicPlayer MusicPlayer { get; set; } = new();
    public static MusicLibrary MusicLibrary { get; set; } = new();
    public static bool HasMusicLibraryLoaded { get; set; } = false;
    public static MainWindow? MainWindow
    {
        get; set;
    }
    public static ShellPage? ShellPage
    {
        get; set;
    }
}
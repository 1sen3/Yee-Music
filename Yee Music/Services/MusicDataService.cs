using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TagLib;
using Yee_Music.Models;

public class MusicDataService
{
    private const string DataFilePath = "music_data.json";

    public async Task SaveMusicDataAsync(List<MusicInfo> musicInfos)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonData = JsonSerializer.Serialize(musicInfos, options);
        await System.IO.File.WriteAllTextAsync(DataFilePath, jsonData);
    }

    public async Task<List<MusicInfo>> LoadMusicDataAsync()
    {
        if (!System.IO.File.Exists(DataFilePath))
        {
            return new List<MusicInfo>();
        }

        var jsonData = await System.IO.File.ReadAllTextAsync(DataFilePath);
        return JsonSerializer.Deserialize<List<MusicInfo>>(jsonData);
    }
}
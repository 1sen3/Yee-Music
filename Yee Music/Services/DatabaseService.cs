using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Yee_Music.Models;
using System.Diagnostics;
using System.Xml.Schema;
using System.Linq;

namespace Yee_Music.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        public DatabaseService()
        {
            // 获取应用程序本地文件夹路径
            _dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "yeemusic.db");
            _connectionString = $"Data Source={_dbPath}";

            // 初始化数据库
            InitializeDatabase();
        }
        private async void InitializeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 创建音乐库路径表
                var command = connection.CreateCommand();
                command.CommandText = 
                @"
                    CREATE TABLE IF NOT EXISTS MusicLibraryPaths (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Path TEXT NOT NULL UNIQUE
                )";
                command.ExecuteNonQuery();

                // 创建音乐表 - 添加LibraryPathId外键
                command = connection.CreateCommand();
                command.CommandText = 
                @"
                        CREATE TABLE IF NOT EXISTS Music (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FilePath TEXT NOT NULL UNIQUE,
                        Title TEXT,
                        Artist TEXT,
                        Album TEXT,
                        Duration TEXT,
                        IsFavorite INTEGER DEFAULT 0,
                        LastPlayed TEXT,
                        PlayCount INTEGER DEFAULT 0,
                        LibraryPathId INTEGER,
                        FOREIGN KEY (LibraryPathId) REFERENCES MusicLibraryPaths(Id) ON DELETE CASCADE
                )";
                command.ExecuteNonQuery();
                // 创建设置表
                var settingsCommand = connection.CreateCommand();
                settingsCommand.CommandText = 
                @"
                        CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                )";
                await settingsCommand.ExecuteNonQueryAsync();
                //创建播放列表
                command = connection.CreateCommand();
                command.CommandText=
                @"
                    CREATE TABLE IF NOT EXISTS PlaylistMusic (
                    PlaylistId INTEGER,
                    MusicId INTEGER,
                    SortOrder INTEGER,
                    PRIMARY KEY (PlaylistId, MusicId),
                    FOREIGN KEY (PlaylistId) REFERENCES Playlist(Id) ON DELETE CASCADE,
                    FOREIGN KEY (MusicId) REFERENCES Music(Id) ON DELETE CASCADE
                )";
                command.ExecuteNonQuery();

                // 检查是否需要迁移数据（为现有音乐添加LibraryPathId）
                command = connection.CreateCommand();
                command.CommandText = "PRAGMA table_info(Music)";
                using var reader = await command.ExecuteReaderAsync();
                bool hasLibraryPathId = false;
                while (await reader.ReadAsync())
                {
                    if (reader.GetString(1) == "LibraryPathId")
                    {
                        hasLibraryPathId = true;
                        break;
                    }
                }

                // 如果需要迁移数据
                if (!hasLibraryPathId)
                {
                    Debug.WriteLine("需要迁移音乐数据，添加LibraryPathId字段");
                    await MigrateMusic();
                }

                // 创建播放队列表
                await CreatePlayQueueTableAsync(connection);

                Debug.WriteLine("数据库初始化成功");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化数据库出错: {ex.Message}");
            }
        }
        // 迁移现有音乐数据，为其添加LibraryPathId
        private async Task MigrateMusic()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 开始事务
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. 添加LibraryPathId列（如果不存在）
                    var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = @"
                        ALTER TABLE Music ADD COLUMN LibraryPathId INTEGER 
                        REFERENCES MusicLibraryPaths(Id) ON DELETE CASCADE";
                    alterCommand.Transaction = transaction;

                    try
                    {
                        await alterCommand.ExecuteNonQueryAsync();
                        Debug.WriteLine("已添加LibraryPathId列");
                    }
                    catch (SqliteException ex)
                    {
                        // 如果列已存在，忽略错误
                        Debug.WriteLine($"添加列时出现异常（可能列已存在）: {ex.Message}");
                    }

                    // 2. 获取所有音乐库路径
                    var pathsCommand = connection.CreateCommand();
                    pathsCommand.CommandText = "SELECT Id, Path FROM MusicLibraryPaths";
                    pathsCommand.Transaction = transaction;

                    var paths = new Dictionary<int, string>();
                    using (var reader = await pathsCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            paths.Add(reader.GetInt32(0), reader.GetString(1));
                        }
                    }

                    // 3. 获取所有音乐
                    var musicCommand = connection.CreateCommand();
                    musicCommand.CommandText = "SELECT Id, FilePath FROM Music WHERE LibraryPathId IS NULL";
                    musicCommand.Transaction = transaction;

                    var musicToUpdate = new List<(int id, string path)>();
                    using (var reader = await musicCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            musicToUpdate.Add((reader.GetInt32(0), reader.GetString(1)));
                        }
                    }

                    // 4. 更新每个音乐的LibraryPathId
                    int updatedCount = 0;
                    foreach (var (musicId, musicPath) in musicToUpdate)
                    {
                        // 找到包含此音乐的音乐库路径
                        foreach (var (pathId, libraryPath) in paths)
                        {
                            if (musicPath.StartsWith(libraryPath, StringComparison.OrdinalIgnoreCase))
                            {
                                var updateCommand = connection.CreateCommand();
                                updateCommand.CommandText = "UPDATE Music SET LibraryPathId = @PathId WHERE Id = @MusicId";
                                updateCommand.Parameters.AddWithValue("@PathId", pathId);
                                updateCommand.Parameters.AddWithValue("@MusicId", musicId);
                                updateCommand.Transaction = transaction;
                                await updateCommand.ExecuteNonQueryAsync();
                                updatedCount++;
                                break;
                            }
                        }
                    }

                    // 提交事务
                    transaction.Commit();
                    Debug.WriteLine($"音乐数据迁移完成，更新了 {updatedCount}/{musicToUpdate.Count} 首歌曲");
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    transaction.Rollback();
                    Debug.WriteLine($"迁移音乐数据出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"迁移音乐数据时出错: {ex.Message}");
            }
        }
        // 修改添加或更新音乐的方法，加入LibraryPathId
        public async Task<bool> AddOrUpdateMusicAsync(MusicInfo music)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 首先找到对应的音乐库路径ID
                int? libraryPathId = await GetLibraryPathIdForMusicAsync(connection, music.FilePath);

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO Music (FilePath, Title, Artist, Album, Duration, IsFavorite, LibraryPathId)
                    VALUES (@FilePath, @Title, @Artist, @Album, @Duration, @IsFavorite, @LibraryPathId)";

                command.Parameters.AddWithValue("@FilePath", music.FilePath);
                command.Parameters.AddWithValue("@Title", music.Title ?? string.Empty);
                command.Parameters.AddWithValue("@Artist", music.Artist ?? string.Empty);
                command.Parameters.AddWithValue("@Album", music.Album ?? string.Empty);
                command.Parameters.AddWithValue("@Duration", music.Duration.ToString());
                command.Parameters.AddWithValue("@IsFavorite", music.IsFavorite ? 1 : 0);
                command.Parameters.AddWithValue("@LibraryPathId", libraryPathId as object ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加或更新音乐出错: {ex.Message}");
                return false;
            }
        }
        // 根据音乐文件路径获取对应的音乐库路径ID
        private async Task<int?> GetLibraryPathIdForMusicAsync(SqliteConnection connection, string musicFilePath)
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Path FROM MusicLibraryPaths";

            using var reader = await command.ExecuteReaderAsync();

            int? bestMatchPathId = null;
            int bestMatchLength = 0;

            while (await reader.ReadAsync())
            {
                int pathId = reader.GetInt32(0);
                string libraryPath = reader.GetString(1);

                // 检查音乐文件是否在这个音乐库路径下
                if (musicFilePath.StartsWith(libraryPath, StringComparison.OrdinalIgnoreCase))
                {
                    // 选择最长匹配的路径（最具体的路径）
                    if (libraryPath.Length > bestMatchLength)
                    {
                        bestMatchPathId = pathId;
                        bestMatchLength = libraryPath.Length;
                    }
                }
            }

            return bestMatchPathId;
        }
        // 添加获取所有音乐库路径的方法
        public async Task<List<string>> GetAllMusicLibraryPathsAsync()
        {
            var result = new List<string>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT Path FROM MusicLibraryPaths";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(reader.GetString(0));
                }

                Debug.WriteLine($"从数据库加载了 {result.Count} 个音乐库路径");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取音乐库路径出错: {ex.Message}");
            }

            return result;
        }
        // 添加保存音乐库路径的方法
        public async Task<bool> AddMusicLibraryPathAsync(string path)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            INSERT OR IGNORE INTO MusicLibraryPaths (Path)
            VALUES (@Path)";
                command.Parameters.AddWithValue("@Path", path);

                int result = await command.ExecuteNonQueryAsync();
                Debug.WriteLine($"添加音乐库路径结果: {result > 0}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加音乐库路径出错: {ex.Message}");
                return false;
            }
        }
        // 修改批量添加音乐的方法，加入LibraryPathId关联
        public async Task<int> AddMusicBatchAsync(List<MusicInfo> musicList)
        {
            int successCount = 0;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 开始事务
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var music in musicList)
                    {
                        // 获取对应的音乐库路径ID
                        int? libraryPathId = await GetLibraryPathIdForMusicAsync(connection, music.FilePath);

                        var command = connection.CreateCommand();
                        command.CommandText = @"
                            INSERT OR REPLACE INTO Music (FilePath, Title, Artist, Album, Duration, IsFavorite, LibraryPathId)
                            VALUES (@FilePath, @Title, @Artist, @Album, @Duration, @IsFavorite, @LibraryPathId)";

                        command.Parameters.AddWithValue("@FilePath", music.FilePath);
                        command.Parameters.AddWithValue("@Title", music.Title ?? string.Empty);
                        command.Parameters.AddWithValue("@Artist", music.Artist ?? string.Empty);
                        command.Parameters.AddWithValue("@Album", music.Album ?? string.Empty);
                        command.Parameters.AddWithValue("@Duration", music.Duration.ToString());
                        command.Parameters.AddWithValue("@IsFavorite", music.IsFavorite ? 1 : 0);
                        command.Parameters.AddWithValue("@LibraryPathId", libraryPathId as object ?? DBNull.Value);
                        command.Transaction = transaction;

                        await command.ExecuteNonQueryAsync();
                        successCount++;
                    }

                    // 提交事务
                    transaction.Commit();
                    Debug.WriteLine($"成功批量添加 {successCount}/{musicList.Count} 首歌曲");
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    transaction.Rollback();
                    Debug.WriteLine($"批量添加音乐事务出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"批量添加音乐出错: {ex.Message}");
            }

            return successCount;
        }
        // 添加一个方法，用于更新所有音乐的音乐库关联
        public async Task UpdateAllMusicLibraryAssociationsAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 获取所有音乐库路径
                var pathsCommand = connection.CreateCommand();
                pathsCommand.CommandText = "SELECT Id, Path FROM MusicLibraryPaths";

                var paths = new Dictionary<int, string>();
                using (var reader = await pathsCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        paths.Add(reader.GetInt32(0), reader.GetString(1));
                    }
                }

                // 获取所有音乐
                var musicCommand = connection.CreateCommand();
                musicCommand.CommandText = "SELECT Id, FilePath FROM Music";

                var musicToUpdate = new List<(int id, string path)>();
                using (var reader = await musicCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        musicToUpdate.Add((reader.GetInt32(0), reader.GetString(1)));
                    }
                }

                // 开始事务
                using var transaction = connection.BeginTransaction();

                try
                {
                    int updatedCount = 0;

                    // 更新每个音乐的LibraryPathId
                    foreach (var (musicId, musicPath) in musicToUpdate)
                    {
                        // 找到包含此音乐的音乐库路径
                        int? bestMatchPathId = null;
                        int bestMatchLength = 0;

                        foreach (var (pathId, libraryPath) in paths)
                        {
                            if (musicPath.StartsWith(libraryPath, StringComparison.OrdinalIgnoreCase))
                            {
                                // 选择最长匹配的路径（最具体的路径）
                                if (libraryPath.Length > bestMatchLength)
                                {
                                    bestMatchPathId = pathId;
                                    bestMatchLength = libraryPath.Length;
                                }
                            }
                        }

                        // 更新音乐的LibraryPathId
                        var updateCommand = connection.CreateCommand();
                        updateCommand.CommandText = "UPDATE Music SET LibraryPathId = @PathId WHERE Id = @MusicId";
                        updateCommand.Parameters.AddWithValue("@PathId", bestMatchPathId as object ?? DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@MusicId", musicId);
                        updateCommand.Transaction = transaction;

                        await updateCommand.ExecuteNonQueryAsync();
                        updatedCount++;
                    }

                    // 提交事务
                    transaction.Commit();
                    Debug.WriteLine($"已更新 {updatedCount}/{musicToUpdate.Count} 首歌曲的音乐库关联");
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    transaction.Rollback();
                    Debug.WriteLine($"更新音乐库关联事务出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新音乐库关联出错: {ex.Message}");
            }
        }
        // 添加删除音乐库路径的方法
        public async Task<bool> RemoveMusicLibraryPathAsync(string path)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 开始事务
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. 获取要删除的音乐库路径ID
                    var getIdCommand = connection.CreateCommand();
                    getIdCommand.CommandText = "SELECT Id FROM MusicLibraryPaths WHERE Path = @Path";
                    getIdCommand.Parameters.AddWithValue("@Path", path);
                    getIdCommand.Transaction = transaction;

                    var pathId = await getIdCommand.ExecuteScalarAsync();

                    if (pathId != null)
                    {
                        // 2. 删除与此音乐库关联的所有音乐
                        // 注意：如果设置了外键级联删除，这一步可能不需要
                        var deleteMusicCommand = connection.CreateCommand();
                        deleteMusicCommand.CommandText = "DELETE FROM Music WHERE LibraryPathId = @PathId";
                        deleteMusicCommand.Parameters.AddWithValue("@PathId", pathId);
                        deleteMusicCommand.Transaction = transaction;

                        int deletedCount = await deleteMusicCommand.ExecuteNonQueryAsync();
                        Debug.WriteLine($"已删除 {deletedCount} 首与音乐库 {path} 关联的歌曲");
                    }

                    // 3. 删除音乐库路径
                    var deletePathCommand = connection.CreateCommand();
                    deletePathCommand.CommandText = "DELETE FROM MusicLibraryPaths WHERE Path = @Path";
                    deletePathCommand.Parameters.AddWithValue("@Path", path);
                    deletePathCommand.Transaction = transaction;

                    await deletePathCommand.ExecuteNonQueryAsync();

                    // 提交事务
                    transaction.Commit();
                    Debug.WriteLine($"已成功删除音乐库路径: {path}");
                    return true;
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    transaction.Rollback();
                    Debug.WriteLine($"删除音乐库路径事务出错: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除音乐库路径出错: {ex.Message}");
                return false;
            }
        }
        // 添加保存所有音乐库路径的方法
        public async Task<bool> SaveAllMusicLibraryPathsAsync(IEnumerable<string> paths)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 开始事务
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 清空现有路径
                    var clearCommand = connection.CreateCommand();
                    clearCommand.CommandText = "DELETE FROM MusicLibraryPaths";
                    clearCommand.Transaction = transaction;
                    await clearCommand.ExecuteNonQueryAsync();

                    // 添加新路径
                    foreach (var path in paths)
                    {
                        var insertCommand = connection.CreateCommand();
                        insertCommand.CommandText = "INSERT INTO MusicLibraryPaths (Path) VALUES (@Path)";
                        insertCommand.Parameters.AddWithValue("@Path", path);
                        insertCommand.Transaction = transaction;
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    // 提交事务
                    transaction.Commit();
                    Debug.WriteLine($"成功保存 {paths.Count()} 个音乐库路径到数据库");
                    return true;
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    transaction.Rollback();
                    Debug.WriteLine($"保存音乐库路径事务出错: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存音乐库路径出错: {ex.Message}");
                return false;
            }
        }
        // 获取所有音乐
        public async Task<List<MusicInfo>> GetAllMusicAsync()
        {
            var result = new List<MusicInfo>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Music";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var music = new MusicInfo
                    {
                        FilePath = reader.GetString(1),
                        Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Artist = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Album = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        IsFavorite = reader.GetInt32(6) == 1
                    };

                    // 解析Duration
                    if (!reader.IsDBNull(5))
                    {
                        if (TimeSpan.TryParse(reader.GetString(5), out var duration))
                        {
                            music.Duration = duration;
                        }
                    }

                    // 检查文件是否存在
                    if (File.Exists(music.FilePath))
                    {
                        result.Add(music);
                    }
                    else
                    {
                        Debug.WriteLine($"音乐文件不存在，将从数据库中删除: {music.FilePath}");
                        await RemoveMusicAsync(music.FilePath);
                    }
                }

                Debug.WriteLine($"从数据库加载了 {result.Count} 首歌曲");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取所有音乐出错: {ex.Message}");
            }

            return result;
        }
        // 添加一个方法用于从数据库中删除音乐
        public async Task<bool> RemoveMusicAsync(string filePath)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Music WHERE FilePath = @FilePath";
                command.Parameters.AddWithValue("@FilePath", filePath);

                int result = await command.ExecuteNonQueryAsync();
                Debug.WriteLine($"已从数据库中删除不存在的音乐文件: {filePath}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除音乐文件记录时出错: {ex.Message}");
                return false;
            }
        }
        // 获取喜欢的音乐
        public async Task<List<MusicInfo>> GetFavoriteMusicAsync()
        {
            var result = new List<MusicInfo>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Music WHERE IsFavorite = 1";

                Debug.WriteLine("执行SQL查询获取喜欢的音乐");

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var music = new MusicInfo
                    {
                        FilePath = reader.GetString(1),
                        Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Artist = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Album = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        IsFavorite = true
                    };

                    // 解析Duration
                    if (!reader.IsDBNull(5))
                    {
                        if (TimeSpan.TryParse(reader.GetString(5), out var duration))
                        {
                            music.Duration = duration;
                        }
                    }

                    result.Add(music);
                    Debug.WriteLine($"加载喜欢的歌曲: {music.Title} - {music.Artist}");
                }

                Debug.WriteLine($"成功从数据库加载了 {result.Count} 首喜欢的歌曲");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取喜欢的音乐出错: {ex.Message}");
            }

            return result;
        }
        // 更新喜欢状态
        public async Task<bool> UpdateFavoriteStatusAsync(string filePath, bool isFavorite)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Music SET IsFavorite = @IsFavorite WHERE FilePath = @FilePath";
                command.Parameters.AddWithValue("@IsFavorite", isFavorite ? 1 : 0);
                command.Parameters.AddWithValue("@FilePath", filePath);

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新喜欢状态出错: {ex.Message}");
                return false;
            }
        }

        // 更新播放次数和最后播放时间
        public async Task UpdatePlayStatisticsAsync(string filePath)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Music 
                    SET PlayCount = PlayCount + 1, LastPlayed = @LastPlayed 
                    WHERE FilePath = @FilePath";
                command.Parameters.AddWithValue("@LastPlayed", DateTime.Now.ToString("o"));
                command.Parameters.AddWithValue("@FilePath", filePath);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新播放统计出错: {ex.Message}");
            }
        }
        public async Task ImportMusicFromPathsAsync(IEnumerable<string> folderPaths)
        {
            if (folderPaths == null || !folderPaths.Any())
                return;

            try
            {
                // 获取已有的音乐文件路径
                var existingPaths = new HashSet<string>();
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT FilePath FROM Music";

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        existingPaths.Add(reader.GetString(0));
                    }
                }

                int newFilesCount = 0;
                // 从音乐库路径扫描音乐文件
                foreach (var folderPath in folderPaths)
                {
                    if (Directory.Exists(folderPath))
                    {
                        var musicFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => IsAudioFile(file))
                            .ToList();

                        Debug.WriteLine($"在路径 {folderPath} 中找到 {musicFiles.Count} 个音频文件");

                        foreach (var filePath in musicFiles)
                        {
                            if (!existingPaths.Contains(filePath))
                            {
                                // 创建MusicInfo对象
                                var musicInfo = await CreateMusicInfoFromFileAsync(filePath);
                                if (musicInfo != null)
                                {
                                    bool success = await AddOrUpdateMusicAsync(musicInfo);
                                    if (success)
                                        newFilesCount++;
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"路径不存在: {folderPath}");
                    }
                }

                Debug.WriteLine($"导入音乐完成，新增 {newFilesCount} 个文件");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"导入音乐出错: {ex.Message}");
            }
        }

        public async Task ScanAndImportAllMusicAsync()
        {
            try
            {
                // 获取所有音乐库路径
                var paths = await GetAllMusicLibraryPathsAsync();

                if (paths.Count == 0)
                {
                    Debug.WriteLine("没有找到音乐库路径，无法扫描音乐");
                    return;
                }

                // 导入所有路径中的音乐
                await ImportMusicFromPathsAsync(paths);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"扫描并导入音乐出错: {ex.Message}");
            }
        }
        // 添加辅助方法判断是否为音频文件
        private bool IsAudioFile(string filePath)
        {
            var supportedExtensions = new[] { ".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg", ".wma" };
            return supportedExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant());
        }

        // 添加辅助方法从文件创建MusicInfo对象
        private async Task<MusicInfo> CreateMusicInfoFromFileAsync(string filePath)
        {
            try
            {
                // 使用TagLib读取音乐文件元数据
                using var file = TagLib.File.Create(filePath);

                var musicInfo = new MusicInfo
                {
                    FilePath = filePath,
                    Title = string.IsNullOrEmpty(file.Tag.Title) ? Path.GetFileNameWithoutExtension(filePath) : file.Tag.Title,
                    Artist = string.Join(", ", file.Tag.Performers),
                    Album = file.Tag.Album ?? string.Empty,
                    Duration = file.Properties.Duration,
                    IsFavorite = false // 默认不是收藏的
                };

                return musicInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"读取音乐文件元数据出错: {ex.Message}, 文件: {filePath}");

                // 如果读取元数据失败，仍然返回基本信息
                return new MusicInfo
                {
                    FilePath = filePath,
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    Artist = string.Empty,
                    Album = string.Empty,
                    Duration = TimeSpan.Zero,
                    IsFavorite = false
                };
            }
        }
        // 添加获取特定音乐库下所有音乐的方法
        public async Task<List<MusicInfo>> GetMusicByLibraryPathAsync(string libraryPath)
        {
            var result = new List<MusicInfo>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 首先获取音乐库路径ID
                var pathCommand = connection.CreateCommand();
                pathCommand.CommandText = "SELECT Id FROM MusicLibraryPaths WHERE Path = @Path";
                pathCommand.Parameters.AddWithValue("@Path", libraryPath);

                var pathId = await pathCommand.ExecuteScalarAsync();

                if (pathId != null)
                {
                    // 获取该音乐库下的所有音乐
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Music WHERE LibraryPathId = @PathId";
                    command.Parameters.AddWithValue("@PathId", pathId);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var music = new MusicInfo
                        {
                            FilePath = reader.GetString(1),
                            Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Artist = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Album = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            IsFavorite = reader.GetInt32(6) == 1
                        };

                        // 解析Duration
                        if (!reader.IsDBNull(5))
                        {
                            if (TimeSpan.TryParse(reader.GetString(5), out var duration))
                            {
                                music.Duration = duration;
                            }
                        }

                        // 检查文件是否存在
                        if (File.Exists(music.FilePath))
                        {
                            result.Add(music);
                        }
                    }

                    Debug.WriteLine($"从音乐库 {libraryPath} 加载了 {result.Count} 首歌曲");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取音乐库下的音乐出错: {ex.Message}");
            }

            return result;
        }
        public async Task CreatePlayQueueTableAsync(SqliteConnection connection)
        {
            try
            {
                var command = connection.CreateCommand();
                command.CommandText= 
                @"
                    CREATE TABLE IF NOT EXISTS PlayQueue (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MusicId INTEGER,
                    SortOrder INTEGER,
                    FOREIGN KEY (MusicId) REFERENCES Music(Id) ON DELETE CASCADE
                )";
                await command.ExecuteNonQueryAsync();
                Debug.WriteLine("创建播放队列表成功");
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"创建播放队列表出错: {ex.Message}");
            }
        }
        public async Task SavePlayQueueAsync(List<MusicInfo> queue,int currentIndex)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 开始事务
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 清空现有播放队列
                    var clearCommand = connection.CreateCommand();
                    clearCommand.CommandText = "DELETE FROM PlayQueue";
                    clearCommand.Transaction = transaction;
                    await clearCommand.ExecuteNonQueryAsync();

                    // 保存当前索引到设置表
                    var indexCommand = connection.CreateCommand();
                    indexCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    )";
                    indexCommand.Transaction = transaction;
                    await indexCommand.ExecuteNonQueryAsync();

                    var saveIndexCommand = connection.CreateCommand();
                    saveIndexCommand.CommandText = @"
                    INSERT OR REPLACE INTO Settings (Key, Value)
                    VALUES ('CurrentPlayIndex', @Value)";
                    saveIndexCommand.Parameters.AddWithValue("@Value", currentIndex.ToString());
                    saveIndexCommand.Transaction = transaction;
                    await saveIndexCommand.ExecuteNonQueryAsync();

                    // 保存播放队列
                    for (int i = 0; i < queue.Count; i++)
                    {
                        var music = queue[i];

                        // 获取音乐ID
                        var getMusicIdCommand = connection.CreateCommand();
                        getMusicIdCommand.CommandText = "SELECT Id FROM Music WHERE FilePath = @FilePath";
                        getMusicIdCommand.Parameters.AddWithValue("@FilePath", music.FilePath);
                        getMusicIdCommand.Transaction = transaction;

                        var musicId = await getMusicIdCommand.ExecuteScalarAsync();

                        if (musicId != null)
                        {
                            var insertCommand = connection.CreateCommand();
                            insertCommand.CommandText = @"
                            INSERT INTO PlayQueue (MusicId, SortOrder)
                            VALUES (@MusicId, @SortOrder)";
                            insertCommand.Parameters.AddWithValue("@MusicId", musicId);
                            insertCommand.Parameters.AddWithValue("@SortOrder", i);
                            insertCommand.Transaction = transaction;

                            await insertCommand.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            Debug.WriteLine($"无法找到音乐ID: {music.FilePath}");
                        }
                    }

                    // 提交事务
                    transaction.Commit();
                    Debug.WriteLine($"成功保存播放队列，共 {queue.Count} 首歌曲");
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    transaction.Rollback();
                    Debug.WriteLine($"保存播放队列事务出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存播放队列出错: {ex.Message}");
            }
        }
        // 加载播放队列
        public async Task<(List<MusicInfo> Queue, int CurrentIndex)> LoadPlayQueueAsync()
        {
            var queue = new List<MusicInfo>();
            int currentIndex = -1;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // 获取当前索引
                var indexCommand = connection.CreateCommand();
                indexCommand.CommandText = "SELECT Value FROM Settings WHERE Key = 'CurrentPlayIndex'";

                var indexResult = await indexCommand.ExecuteScalarAsync();
                if (indexResult != null)
                {
                    int.TryParse(indexResult.ToString(), out currentIndex);
                }

                // 获取播放队列
                var queueCommand = connection.CreateCommand();
                queueCommand.CommandText = @"
                SELECT m.FilePath, m.Title, m.Artist, m.Album, m.Duration, m.IsFavorite
                FROM PlayQueue pq
                JOIN Music m ON pq.MusicId = m.Id
                ORDER BY pq.SortOrder";

                using var reader = await queueCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var music = new MusicInfo
                    {
                        FilePath = reader.GetString(0),
                        Title = reader.GetString(1),
                        Artist = reader.GetString(2),
                        Album = reader.GetString(3),
                        IsFavorite = reader.GetInt32(5) == 1
                    };

                    // 解析时长
                    if (TimeSpan.TryParse(reader.GetString(4), out TimeSpan duration))
                    {
                        music.Duration = duration;
                    }

                    queue.Add(music);
                }

                Debug.WriteLine($"从数据库加载了 {queue.Count} 首歌曲的播放队列，当前索引: {currentIndex}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载播放队列出错: {ex.Message}");
            }

            return (queue, currentIndex);
        }
        // 检查音乐是否被收藏
        public async Task<bool> IsMusicFavoriteAsync(string filePath)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            SELECT IsFavorite FROM Music 
            WHERE FilePath = @FilePath";
                command.Parameters.AddWithValue("@FilePath", filePath);

                var result = await command.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) == 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查音乐收藏状态出错: {ex.Message}");
            }

            return false;
        }
        // 更新音乐收藏状态
        public async Task UpdateMusicFavoriteStatusAsync(string filePath, bool isFavorite)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            UPDATE Music 
            SET IsFavorite = @IsFavorite 
            WHERE FilePath = @FilePath";
                command.Parameters.AddWithValue("@FilePath", filePath);
                command.Parameters.AddWithValue("@IsFavorite", isFavorite ? 1 : 0);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新音乐收藏状态出错: {ex.Message}");
            }
        }
    }
}

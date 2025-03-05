using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Schema;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using TagLib.Matroska;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Yee_Music.Helpers;
using Yee_Music.Models;
using Yee_Music.Services;

namespace Yee_Music.ViewModels
{
    public class LibraryViewModel : ObservableRecipient
    {
        // 单例实例
        private static LibraryViewModel _instance;

        // 单例访问器
        public static LibraryViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LibraryViewModel();
                }
                return _instance;
            }
        }
        private readonly DatabaseService _databaseService;
        private string _musicLibraryPath;
        private readonly MusicLibrary _musicLibrary;
        private ObservableCollection<MusicInfo> _musicInfos;
        private ObservableCollection<MusicInfo> _musicList = new ObservableCollection<MusicInfo>();
        private bool _isLoading;
        private MusicInfo _selectedMusic;
        private ObservableCollection<string> _musicLibraryPaths = new ObservableCollection<string>();
        public ObservableCollection<string> MusicLibraryPaths => _musicLibraryPaths;
        public bool IsLibraryEmpty
        {
            get => MusicList == null || MusicList.Count == 0;
        }

        public Visibility IsEmptyLibrary
        {
            get
            {
                var isEmpty = _musicLibraryPaths == null || _musicLibraryPaths.Count == 0;
                Debug.WriteLine($"IsEmptyLibrary被调用: {isEmpty}, 路径数量: {_musicLibraryPaths?.Count ?? 0}");
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public ObservableCollection<MusicInfo> MusicList
        {
            get => _musicList;
            private set
            {
                SetProperty(ref _musicList, value);
                OnPropertyChanged(nameof(IsLibraryEmpty));
                Debug.WriteLine($"MusicList 已更新，包含 {value?.Count ?? 0} 首歌曲");
            }
        }

        // 属性定义
        public ObservableCollection<MusicInfo> MusicInfos
        {
            get => _musicInfos;
            set => SetProperty(ref _musicInfos, value);
        }

        public string MusicLibraryPath
        {
            get => _musicLibraryPath;
            set
            {
                if (SetProperty(ref _musicLibraryPath, value))
                {
                    // 将新路径保存到数据库
                    if (_databaseService != null && !string.IsNullOrEmpty(value))
                    {
                        _ = _databaseService.AddMusicLibraryPathAsync(value);
                        Debug.WriteLine($"已将路径 {value} 添加到数据库");
                    }

                    _ = LoadMusicLibrary();  // 路径改变时自动加载
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // 命令定义
        public IAsyncRelayCommand SelectFolderCommand { get; }
        public IAsyncRelayCommand LoadMusicLibraryCommand { get; }
        public IAsyncRelayCommand<MusicInfo> PlayCommand { get; }
        public void InitializeMusicLibraryPaths(List<string> paths)
        {
            _musicLibraryPaths.Clear();
            foreach (var path in paths)
            {
                _musicLibraryPaths.Add(path);
            }
            OnPropertyChanged(nameof(MusicLibraryPaths));
            OnPropertyChanged(nameof(IsEmptyLibrary));
        }
        // 将构造函数改为私有，防止外部创建实例
        private LibraryViewModel()
        {
            try
            {
                _musicLibrary = new MusicLibrary();
                _musicInfos = new ObservableCollection<MusicInfo>();

                // 正确获取DatabaseService实例
                _databaseService = App.Services?.GetService<DatabaseService>();

                if (_databaseService == null)
                {
                    Debug.WriteLine("警告：DatabaseService未能正确初始化");
                }
                else
                {
                    Debug.WriteLine("DatabaseService初始化成功");
                }

                // 初始化音乐库路径集合
                _musicLibraryPaths = new ObservableCollection<string>();

                // 初始化命令
                SelectFolderCommand = new AsyncRelayCommand(SelectFolder);
                LoadMusicLibraryCommand = new AsyncRelayCommand(LoadMusicLibrary);
                PlayCommand = new AsyncRelayCommand<MusicInfo>(PlayMusic);
                AddMusicLibraryCommand = new AsyncRelayCommand(AddMusicLibraryAsync);
                RemoveMusicLibraryCommand = new RelayCommand<string>(RemoveMusicLibrary);
                RefreshMusicListCommand = new AsyncRelayCommand(LoadAllMusicLibrariesAsync);

                // 从数据库加载音乐库路径和音乐
                _ = LoadMusicLibraryPathsFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ViewModel 初始化错误: {ex}");
            }
        }
        // 添加从数据库加载音乐库路径的方法
        private async Task LoadMusicLibraryPathsFromDatabaseAsync()
        {
            try
            {
                if (_databaseService == null)
                {
                    Debug.WriteLine("DatabaseService未初始化，无法加载音乐库路径");
                    return;
                }

                // 从数据库加载路径
                var paths = await _databaseService.GetAllMusicLibraryPathsAsync();

                // 更新UI集合
                _musicLibraryPaths.Clear();
                foreach (var path in paths)
                {
                    _musicLibraryPaths.Add(path);
                }

                Debug.WriteLine($"从数据库加载了 {_musicLibraryPaths.Count} 个音乐库路径");

                // 通知UI更新
                OnPropertyChanged(nameof(MusicLibraryPaths));
                OnPropertyChanged(nameof(IsEmptyLibrary));

                // 加载音乐
                await LoadAllMusicFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"从数据库加载音乐库路径出错: {ex.Message}");
            }
        }
        private async Task LoadAllMusicFromDatabaseAsync()
        {
            try
            {
                Debug.WriteLine("开始从数据库加载音乐...");

                if (_databaseService == null)
                {
                    Debug.WriteLine("DatabaseService未初始化，无法加载音乐");
                    return;
                }

                // 从数据库获取所有音乐
                var musicList = await _databaseService.GetAllMusicAsync();

                // 如果数据库中没有音乐，则从文件系统加载
                if (musicList.Count == 0)
                {
                    Debug.WriteLine("数据库中没有音乐，从文件系统加载");
                    await LoadAllMusicLibrariesAsync();

                    // 导入音乐到数据库
                    await ImportMusicToDatabase();
                }
                else
                {
                    // 更新UI集合
                    MusicList = new ObservableCollection<MusicInfo>(musicList);
                    Debug.WriteLine($"从数据库加载了 {MusicList.Count} 首歌曲");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"从数据库加载音乐出错: {ex.Message}");

                // 出错时尝试从文件系统加载
                await LoadAllMusicLibrariesAsync();
            }
        }
        private async Task ImportMusicToDatabase()
        {
            try
            {
                if (_databaseService == null || MusicList.Count == 0)
                    return;

                Debug.WriteLine($"开始将 {MusicList.Count} 首歌曲导入数据库...");

                int successCount = 0;
                foreach (var music in MusicList)
                {
                    bool success = await _databaseService.AddOrUpdateMusicAsync(music);
                    if (success)
                        successCount++;
                }

                Debug.WriteLine($"成功导入 {successCount} 首歌曲到数据库");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"导入音乐到数据库出错: {ex.Message}");
            }
        }
        public IAsyncRelayCommand AddMusicLibraryCommand { get; }
        public IRelayCommand<string> RemoveMusicLibraryCommand { get; }
        // 修改加载音乐库的方法，确保正确处理异常并通知 UI 更新
        private async Task LoadAllMusicLibrariesAsync()
        {
            try
            {
                Debug.WriteLine("开始加载所有音乐库...");

                // 创建一个新的集合
                var newMusicList = new ObservableCollection<MusicInfo>();

                // 从数据库获取音乐库路径
                var paths = await _databaseService.GetAllMusicLibraryPathsAsync();

                // 确保音乐库路径列表有效
                if (paths == null || !paths.Any())
                {
                    Debug.WriteLine("没有找到音乐库路径");
                    MusicList = newMusicList;
                    return;
                }

                // 加载所有音乐库中的音乐
                foreach (var path in paths)
                {
                    if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    {
                        Debug.WriteLine($"跳过无效路径: {path}");
                        continue;
                    }

                    Debug.WriteLine($"加载音乐库: {path}");
                    await LoadMusicLibraryAsync(path, newMusicList);

                    // 每处理完一个路径就进行一次小型GC回收
                    GC.Collect(0, GCCollectionMode.Optimized);
                }

                // 替换整个集合
                MusicList = newMusicList;

                // 确保通知UI更新
                OnPropertyChanged(nameof(MusicList));

                Debug.WriteLine($"所有音乐库加载完成，共 {MusicList.Count} 首歌曲");

                // 将加载的音乐保存到数据库
                await ImportMusicToDatabase();

                // 扫描完成后强制进行完整的垃圾回收
                Debug.WriteLine("扫描完成，开始释放内存...");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Debug.WriteLine("内存释放完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载音乐库时出错: {ex.Message}");
            }
        }
        public IRelayCommand RefreshMusicListCommand { get; }

        private async Task LoadMusicLibraryAsync(string path, ObservableCollection<MusicInfo> targetList)
        {
            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                var files = await folder.GetFilesAsync();

                Debug.WriteLine($"在 {path} 中找到 {files.Count} 个文件");

                // 分批处理文件，每批100个
                const int batchSize = 100;
                for (int i = 0; i < files.Count; i += batchSize)
                {
                    var batch = files.Skip(i).Take(batchSize);

                    foreach (var file in batch)
                    {
                        try
                        {
                            string fileType = file.FileType.ToLower();
                            if (fileType == ".mp3" || fileType == ".wav" || fileType == ".flac" || fileType == ".m4a")
                            {
                                var music = await MusicInfo.CreateFromFileAsync(file.Path);
                                targetList.Add(music);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"处理音乐文件出错: {file.Path}, 错误: {ex.Message}");
                            continue;
                        }
                    }

                    // 每处理完一批就释放一次内存
                    GC.Collect(0, GCCollectionMode.Optimized);
                }

                // 处理完一个文件夹后进行一次小型GC
                GC.Collect(1, GCCollectionMode.Optimized);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载音乐库出错: {path}, 错误: {ex.Message}");
            }
        }
        private async Task AddMusicLibraryAsync()
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                folderPicker.FileTypeFilter.Add("*");

                // 初始化 FolderPicker
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    var path = folder.Path;

                    // 检查是否已存在该路径
                    if (!_musicLibraryPaths.Contains(path))
                    {
                        // 添加到集合
                        _musicLibraryPaths.Add(path);

                        // 保存到数据库
                        if (_databaseService != null)
                        {
                            await _databaseService.AddMusicLibraryPathAsync(path);
                            Debug.WriteLine($"已将路径 {path} 添加到数据库");
                        }
                        else
                        {
                            Debug.WriteLine("警告：DatabaseService未初始化，无法保存路径到数据库");
                        }

                        Debug.WriteLine($"添加新音乐库: {path}");
                        Debug.WriteLine($"当前音乐库路径数量: {_musicLibraryPaths.Count}");

                        // 强制通知UI更新
                        OnPropertyChanged(nameof(MusicLibraryPaths));
                        OnPropertyChanged(nameof(IsEmptyLibrary));

                        // 加载新添加的音乐库
                        var newMusicList = new ObservableCollection<MusicInfo>();
                        await LoadMusicLibraryAsync(path, newMusicList);

                        // 将新音乐添加到现有列表
                        foreach (var music in newMusicList)
                        {
                            MusicList.Add(music);

                            // 同时添加到数据库
                            if (_databaseService != null)
                            {
                                await _databaseService.AddOrUpdateMusicAsync(music);
                            }
                        }

                        // 通知UI更新
                        OnPropertyChanged(nameof(MusicList));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加音乐库时出错: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
        // 添加一个公共方法来保存音乐库路径
        public async Task SaveMusicLibraryPathsAsync()
        {
            try
            {
                // 保存到数据库
                if (_databaseService != null)
                {
                    await _databaseService.SaveAllMusicLibraryPathsAsync(_musicLibraryPaths);
                    Debug.WriteLine($"已保存 {_musicLibraryPaths.Count} 个音乐库路径到数据库");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存音乐库路径时出错: {ex.Message}");
            }
        }
        private async void RemoveMusicLibrary(string path)
        {
            try
            {
                if (_musicLibraryPaths.Contains(path))
                {
                    // 从集合中移除
                    _musicLibraryPaths.Remove(path);

                    // 从数据库中移除
                    if (_databaseService != null)
                    {
                        await _databaseService.RemoveMusicLibraryPathAsync(path);
                    }

                    Debug.WriteLine($"移除音乐库: {path}");
                    Debug.WriteLine($"当前音乐库路径数量: {_musicLibraryPaths.Count}");

                    // 通知UI更新
                    OnPropertyChanged(nameof(MusicLibraryPaths));
                    OnPropertyChanged(nameof(IsEmptyLibrary));

                    // 重新从数据库加载所有音乐
                    await LoadAllMusicFromDatabaseAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"移除音乐库时出错: {ex.Message}");
            }
        }

        private async Task LoadMusicLibrary()
        {
            if (string.IsNullOrEmpty(_musicLibraryPath))
                return;

            IsLoading = true;
            try
            {
                var files = Directory.GetFiles(_musicLibraryPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => new[] { ".mp3", ".wav", ".flac", ".m4a","aif"}.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                Debug.WriteLine($"找到 {files.Count} 个音乐文件");

                var musicFiles = new ObservableCollection<MusicInfo>();
                foreach (var file in files)
                {
                    try
                    {
                        var musicInfo = await MusicInfo.CreateFromFileAsync(file);
                        musicFiles.Add(musicInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"处理文件失败: {file}, 错误: {ex.Message}");
                    }
                }

                MusicInfos = musicFiles;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载音乐库失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SelectFolder()
        {
            try
            {
                var folderPicker = new FolderPicker();
                var window = App.MainWindow;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                folderPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    MusicLibraryPath = folder.Path;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"选择文件夹失败: {ex.Message}");
            }
        }

        private async Task PlayMusic(MusicInfo music)
        {
            try
            {
                if (music == null) return;

                // 使用当前实例的 MusicList 属性，而不是 ViewModel.MusicList
                PlayQueueService.Instance.SetQueue(MusicList, music);
                await App.MusicPlayer.PlayAsync(music);

                // 更新播放统计
                if (_databaseService != null)
                {
                    await _databaseService.UpdatePlayStatisticsAsync(music.FilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放音乐失败: {ex.Message}");
            }
        }
        public void AddMusicLibraryPathWithoutPicker(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.WriteLine($"无效的音乐库路径: {path}");
                return;
            }

            // 检查路径是否已存在
            if (MusicLibraryPaths.Contains(path))
            {
                Debug.WriteLine($"音乐库路径已存在: {path}");
                return;
            }

            // 添加路径
            MusicLibraryPaths.Add(path);

            // 保存设置
            SaveMusicLibraryPathsAsync();

            Debug.WriteLine($"已添加音乐库路径: {path}");
        }
        private ICommand _addToFavoriteCommand;
        public ICommand AddToFavoriteCommand => _addToFavoriteCommand ??= new RelayCommand<MusicInfo>(AddToFavorite);
        private async void AddToFavorite(MusicInfo music)
        {
            if (music != null)
            {
                var dbService = App.Services.GetService(typeof(DatabaseService)) as DatabaseService;
                if (dbService != null)
                {
                    await dbService.UpdateFavoriteStatusAsync(music.FilePath, music.IsFavorite);
                    Debug.WriteLine($"{(music.IsFavorite ? "添加到" : "从")}喜欢列表{(music.IsFavorite ? "" : "移除")}: {music.Title}");
                }
            }
        }
        private ObservableCollection<MusicInfo> _favoriteMusicList = new ObservableCollection<MusicInfo>();
        public ObservableCollection<MusicInfo> FavoriteMusicList
        {
            get => _favoriteMusicList;
            set => SetProperty(ref _favoriteMusicList, value);
        }

        // 收藏列表是否为空
        public bool IsFavoriteEmpty => FavoriteMusicList == null || FavoriteMusicList.Count == 0;
    }
}

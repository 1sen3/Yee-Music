using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;

namespace Yee_Music.Services
{
    public class CoverImageCacheService
    {
        private static readonly string CacheFolderName = "CoverCache";
        private static string _cacheFolderPath;
        public static async Task InitializeAsync()
        {
            try
            {
                // 获取应用程序本地缓存文件夹
                var localFolder = ApplicationData.Current.LocalFolder;
                var cacheFolder = await localFolder.CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);
                _cacheFolderPath = cacheFolder.Path;

                Debug.WriteLine($"封面缓存文件夹初始化: {_cacheFolderPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化封面缓存文件夹失败: {ex.Message}");
                // 使用备用路径
                _cacheFolderPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, CacheFolderName);
                Directory.CreateDirectory(_cacheFolderPath);
            }
        }
        // 生成文件路径的哈希值作为缓存文件名
        private static string GetHashString(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        // 保存封面图片到缓存
        public static async Task<string> SaveCoverImageAsync(byte[] imageData, string filePath)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            try
            {
                // 确保缓存文件夹已初始化
                if (string.IsNullOrEmpty(_cacheFolderPath))
                {
                    await InitializeAsync();
                }

                // 生成缓存文件名
                string fileName = GetHashString(filePath) + ".jpg";
                string cachePath = Path.Combine(_cacheFolderPath, fileName);

                // 保存图片到缓存
                await File.WriteAllBytesAsync(cachePath, imageData);

                Debug.WriteLine($"封面图片已缓存: {cachePath}");
                return cachePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存封面图片到缓存失败: {ex.Message}");
                return null;
            }
        }

        // 从缓存加载封面图片
        public static async Task<BitmapImage> LoadCoverImageAsync(string filePath)
        {
            try
            {
                // 确保缓存文件夹已初始化
                if (string.IsNullOrEmpty(_cacheFolderPath))
                {
                    await InitializeAsync();
                }

                // 生成缓存文件名
                string fileName = GetHashString(filePath) + ".jpg";
                string cachePath = Path.Combine(_cacheFolderPath, fileName);

                // 检查缓存文件是否存在
                if (File.Exists(cachePath))
                {
                    var bitmap = new BitmapImage();
                    using (var stream = File.OpenRead(cachePath))
                    {
                        var memStream = new MemoryStream();
                        await stream.CopyToAsync(memStream);
                        memStream.Position = 0;

                        await bitmap.SetSourceAsync(memStream.AsRandomAccessStream());
                    }

                    Debug.WriteLine($"从缓存加载封面图片: {cachePath}");
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"从缓存加载封面图片失败: {ex.Message}");
            }

            return null;
        }

        // 检查封面图片是否已缓存
        public static bool IsCoverImageCached(string filePath)
        {
            if (string.IsNullOrEmpty(_cacheFolderPath))
                return false;

            string fileName = GetHashString(filePath) + ".jpg";
            string cachePath = Path.Combine(_cacheFolderPath, fileName);

            return File.Exists(cachePath);
        }

        // 清理缓存
        public static async Task ClearCacheAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_cacheFolderPath) || !Directory.Exists(_cacheFolderPath))
                    return;

                var directory = new DirectoryInfo(_cacheFolderPath);
                foreach (var file in directory.GetFiles())
                {
                    file.Delete();
                }

                Debug.WriteLine("封面缓存已清理");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理封面缓存失败: {ex.Message}");
            }
        }
    }
}

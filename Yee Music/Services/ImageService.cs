using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace Yee_Music.Services
{
    public static class ImageService
    {
        public static async Task<BitmapImage> GetOptimizedBitmapImageAsync(byte[] imageData, int maxWidth = 200, int maxHeight = 200)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            try
            {
                // 创建内存流
                using (var stream = new InMemoryRandomAccessStream())
                {
                    // 写入图片数据
                    using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(imageData);
                        await writer.StoreAsync();
                    }

                    // 解码图片
                    var decoder = await BitmapDecoder.CreateAsync(stream);

                    // 计算缩放尺寸
                    double scale = Math.Min(
                        maxWidth / (double)decoder.PixelWidth,
                        maxHeight / (double)decoder.PixelHeight);

                    if (scale > 1) scale = 1; // 不放大图片

                    uint scaledWidth = (uint)(decoder.PixelWidth * scale);
                    uint scaledHeight = (uint)(decoder.PixelHeight * scale);

                    // 创建缩放后的图片
                    using (var resizedStream = new InMemoryRandomAccessStream())
                    {
                        var transform = new BitmapTransform
                        {
                            ScaledWidth = scaledWidth,
                            ScaledHeight = scaledHeight,
                            InterpolationMode = BitmapInterpolationMode.Linear
                        };

                        var pixelData = await decoder.GetPixelDataAsync(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied,
                            transform,
                            ExifOrientationMode.RespectExifOrientation,
                            ColorManagementMode.DoNotColorManage);

                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, resizedStream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied,
                            scaledWidth,
                            scaledHeight,
                            decoder.DpiX,
                            decoder.DpiY,
                            pixelData.DetachPixelData());

                        await encoder.FlushAsync();

                        // 创建 BitmapImage
                        var bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(resizedStream);
                        return bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"优化图片时出错: {ex.Message}");
                return null;
            }
        }
    }
}
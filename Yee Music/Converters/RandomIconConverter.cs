using Microsoft.UI.Xaml.Data;
using System;
using Yee_Music.ViewModels;
using Yee_Music.Models;
using static Yee_Music.Models.MusicPlayer;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Yee_Music.Helpers
{
    public class RandomIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // 修改为接收布尔值
            if (value is bool isShuffleEnabled)
            {
                // 根据随机播放状态返回不同图标
                // 使用相同的随机播放图标，但在不同状态下使用不同的颜色
                return isShuffleEnabled ? "\uE8B1" : "\uE8B1"; // 两种状态都使用随机播放图标
            }
            return "\uE8B1"; // 默认图标为随机播放
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
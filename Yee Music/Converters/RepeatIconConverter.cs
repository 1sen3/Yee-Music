using Microsoft.UI.Xaml.Data;
using System;
using Yee_Music.ViewModels;
using Yee_Music.Models;
using static Yee_Music.Models.MusicPlayer;

namespace Yee_Music.Converters
{
    public class PlayModeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PlaybackMode playMode)
            {
                return playMode switch
                {
                    PlaybackMode.Sequential => "\uF5E7",  // 不循环图标
                    PlaybackMode.SingleRepeat => "\uE8ED", // 循环图标
                    PlaybackMode.ListRepeat => "\uE8EE",   // 列表循环图标
                    _ => "\uF5E7"
                };
            }
            return "\uF5E7"; // 默认图标
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
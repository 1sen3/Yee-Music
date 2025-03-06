using Microsoft.UI.Xaml.Data;
using System;

namespace Yee_Music.Converters;
public class PlayPauseIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isPlaying)
        {
            // 如果正在播放，显示暂停图标；否则显示播放图标
            return isPlaying ? "\uF8AE" : "\uF5B0";
        }
        // 默认返回播放图标
        return "\uE768";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace Yee_Music.Converters
{
    public class RandomColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isShuffleEnabled)
            {
                if (isShuffleEnabled)
                {
                    // 随机播放启用时，使用默认前景色（不修改颜色）
                    return Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush
                        ?? new SolidColorBrush(Colors.White);
                }
                else
                {
                    // 随机播放禁用时，使用灰色
                    return Application.Current.Resources["TextFillColorSecondaryBrush"] as SolidColorBrush
                        ?? new SolidColorBrush(Colors.Gray);
                }
            }

            // 默认返回默认前景色
            return Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush
                ?? new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
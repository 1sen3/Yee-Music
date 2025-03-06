using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace Yee_Music.Converters
{
    public class VolumeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double volume)
            {
                if (volume <= 0)
                    return "\uE74F"; // 静音图标
                else if (volume < 33)
                    return "\uE992"; // 低音量图标
                else if (volume < 66)
                    return "\uE993"; // 中音量图标
                else
                    return "\uE994"; // 高音量图标
            }
            return "\uE994"; // 默认图标
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using Microsoft.UI.Xaml.Data;
using System;

namespace Yee_Music.Helpers
{
    public class FavoriteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isFavorite)
            {
                // 使用实心心形表示喜欢，空心心形表示未喜欢
                return isFavorite ? "\uEB52" : "\uEB51";
            }
            return "\uE734"; // 默认返回空心心形
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
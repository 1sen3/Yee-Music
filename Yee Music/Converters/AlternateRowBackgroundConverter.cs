using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;

namespace Yee_Music.Converters
{
    public class AlternateRowBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ListViewItem item)
            {
                var listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
                if (listView != null)
                {
                    var index = listView.IndexFromContainer(item);

                    if (index % 2 == 0)
                    {
                        var brush = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as SolidColorBrush;
                        if (brush == null)
                        {
                            brush = Application.Current.Resources["CardBackgroundFillColorDefault"] as SolidColorBrush;
                        }

                        if (brush == null)
                        {
                            Debug.WriteLine("无法获取主题资源，使用硬编码颜色");
                            return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 128, 128, 128));
                        }

                        return brush;
                    }
                }
            }

            return null; // 透明背景
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
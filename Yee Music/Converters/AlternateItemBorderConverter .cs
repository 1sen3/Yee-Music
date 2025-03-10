using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Yee_Music.Converters
{
    public class AlternateItemBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ListViewItem item)
            {
                var listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
                if (listView != null)
                {
                    var index = listView.IndexFromContainer(item);

                    // 偶数行有背景色，所以也应该有描边
                    if (index % 2 == 0)
                    {
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(48, 128, 128, 128)); // 半透明灰色描边
                    }
                }
            }

            return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)); // 透明描边
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
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
                            return GetAdaptiveBackgroundBrush();
                        }

                        return brush;
                    }
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private SolidColorBrush GetAdaptiveBackgroundBrush()
        {
            var requestedTheme = Application.Current.RequestedTheme;

            if (requestedTheme == ApplicationTheme.Dark)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 200, 200, 200));
            }
            else
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(20, 100, 100, 100));
            }
        }
    }
}
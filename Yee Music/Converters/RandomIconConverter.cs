﻿using Microsoft.UI.Xaml.Data;
using System;
using Yee_Music.ViewModels;
using Yee_Music.Models;
using static Yee_Music.Models.MusicPlayer;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Yee_Music.Converters
{
    public class RandomIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isShuffleEnabled)
            {
                return isShuffleEnabled ? "\uE8B1" : "\uE8B1";
            }
            return "\uE8B1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
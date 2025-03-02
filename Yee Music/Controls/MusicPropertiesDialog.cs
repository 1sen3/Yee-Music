using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using Yee_Music.Models;

namespace Yee_Music.Controls
{
    public class MusicPropertiesDialog : ContentDialog
    {
        private MusicInfo _music;
        public MusicPropertiesDialog()
        {
            this.DefaultStyleKey = typeof(MusicPropertiesDialog);
            this.Title = "属性";
            this.PrimaryButtonText = "确定";
            this.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        }
        public void SetMusic(MusicInfo music)
        {
            _music = music;
            UpdateContent();
        }
        public void UpdateContent()
        {
            if (_music == null)
            {
                return;
            }
            var fileInfo = new FileInfo(_music.FilePath);
            bool fileExists = fileInfo.Exists;
            string fileSize = fileExists ? FormatFileSize(fileInfo.Length) : "文件不存在";
            string lastModified = fileExists ? fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") : "未知";

            var panel = new StackPanel()
            {
                Spacing = 12
            };

            // 添加基本信息
            panel.Children.Add(CreatePropertyGroup("基本信息", new[]
            {
                ("歌曲名称", _music.Title ?? "未知"),
                ("艺术家", _music.Artist ?? "未知"),
                ("专辑", _music.Album ?? "未知"),
                ("时长", _music.Duration.ToString(@"hh\:mm\:ss") ?? "未知")
            }));

            // 添加文件信息
            panel.Children.Add(CreatePropertyGroup("文件信息", new[]
            {
                ("文件路径", _music.FilePath),
                ("文件大小", fileSize),
                ("文件类型", Path.GetExtension(_music.FilePath).ToUpper()),
                ("修改日期", lastModified)
            }));

            this.Content = panel;

        }
        private UIElement CreatePropertyGroup(string groupTitle, (string Label, string Value)[] properties)
        {
            var panel = new StackPanel
            {
                Spacing = 8
            };

            // 添加组标题
            panel.Children.Add(new TextBlock
            {
                Text = groupTitle,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // 添加属性
            foreach (var (label, value) in properties)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var labelBlock = new TextBlock
                {
                    Text = label + ":",
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 8, 0),
                    Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
                };
                Grid.SetColumn(labelBlock, 0);

                var valueBlock = new TextBlock
                {
                    Text = value,
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping = TextWrapping.Wrap
                };
                valueBlock.IsTextSelectionEnabled = true;
                Grid.SetColumn(valueBlock, 1);

                grid.Children.Add(labelBlock);
                grid.Children.Add(valueBlock);
                panel.Children.Add(grid);
            }

            return panel;
        }
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n2}{suffixes[counter]}";
        }
    }
}

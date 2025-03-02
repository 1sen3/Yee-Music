using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT;
using Yee_Music.Models;
using Windows.UI;
using System.Globalization;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yee_Music.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PersonalizeSettingsPage : Page
    {
        private AppSettings _settings;
        private bool _isInitializing = false;
        public PersonalizeSettingsPage()
        {
            this.InitializeComponent();

            try
            {
                // 使用单例实例而不是直接加载
                _settings = AppSettings.Instance;
                InitializeUIState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化设置页面时出错: {ex.Message}");
                // 不需要创建新实例，因为单例模式会确保有一个实例
            }
        }
        private void InitializeUIState()
        {
            _isInitializing = true; // 标记正在初始化

            try
            {
                // 使用 ThemeService 获取当前主题设置
                string currentTheme = App.ThemeService.GetCurrentTheme();

                // 设置主题单选按钮状态
                switch (currentTheme)
                {
                    case "Light":
                        Settings_Theme_Light.IsChecked = true;
                        break;
                    case "Dark":
                        Settings_Theme_Dark.IsChecked = true;
                        break;
                    case "Default":
                    default:
                        Settings_Theme_System.IsChecked = true;
                        break;
                }

                // 使用 ThemeService 获取当前材质设置
                string currentMaterial = App.ThemeService.GetCurrentMaterial();

                // 设置材质下拉框
                int materialIndex = 0;
                switch (currentMaterial)
                {
                    case "Mica":
                        materialIndex = 0;
                        break;
                    case "MicaAlt":
                        materialIndex = 1;
                        break;
                    case "Acrylic":
                        materialIndex = 2;
                        break;
                    case "None":
                        materialIndex = 3;
                        break;
                }
                MaterialComboBox.SelectedIndex = materialIndex;
            }
            finally
            {
                _isInitializing = false; // 初始化完成
            }
        }

        private void Settings_Theme_Light_Checked(object sender, RoutedEventArgs e)
        {
            // 使用 ThemeService 应用浅色主题
            App.ThemeService.ApplyTheme("Light");
        }

        private void Settings_Theme_Dark_Checked(object sender, RoutedEventArgs e)
        {
            // 使用 ThemeService 应用深色主题
            App.ThemeService.ApplyTheme("Dark");
        }

        private void Settings_Theme_System_Checked(object sender, RoutedEventArgs e)
        {
            // 使用 ThemeService 应用系统主题
            App.ThemeService.ApplyTheme("Default");
        }

        private void MaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如果正在初始化，不执行应用材质的操作
            if (_isInitializing)
                return;

            if (MaterialComboBox.SelectedItem is ComboBoxItem item && item.Tag is string materialType)
            {
                // 使用 ThemeService 应用窗口材质
                App.ThemeService.ApplyWindowMaterial(materialType);
            }
        }

        private void ResetMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            // 重置为默认设置
            MaterialComboBox.SelectedIndex = 0; // Mica

            // 使用 ThemeService 应用默认材质
            App.ThemeService.ApplyWindowMaterial("Mica");
        }
    }
}

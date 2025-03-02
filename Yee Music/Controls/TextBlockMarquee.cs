using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Diagnostics;
using Windows.Foundation;

namespace Yee_Music.Controls
{
    // 使用UserControl而不是继承TextBlock
    public sealed class TextBlockMarquee : UserControl
    {
        private TextBlock _textBlock;
        private Storyboard _storyboard;
        private DoubleAnimation _animation;
        private bool _isInitialized = false;
        private bool _isTextTrimmed = false;

        // 定义依赖属性
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBlockMarquee),
                new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(TextBlockMarquee),
                new PropertyMetadata(12.0, OnVisualPropertyChanged));

        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register(nameof(FontWeight), typeof(Windows.UI.Text.FontWeight), typeof(TextBlockMarquee),
                new PropertyMetadata(Microsoft.UI.Text.FontWeights.Normal, OnVisualPropertyChanged));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(TextBlockMarquee),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        public static readonly DependencyProperty MarqueeDelayProperty =
            DependencyProperty.Register(nameof(MarqueeDelay), typeof(double), typeof(TextBlockMarquee),
                new PropertyMetadata(1000.0, OnMarqueePropertyChanged));

        public static readonly DependencyProperty MarqueeSpeedProperty =
            DependencyProperty.Register(nameof(MarqueeSpeed), typeof(double), typeof(TextBlockMarquee),
                new PropertyMetadata(50.0, OnMarqueePropertyChanged));

        // 属性
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public Windows.UI.Text.FontWeight FontWeight
        {
            get { return (Windows.UI.Text.FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }


        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public double MarqueeDelay
        {
            get { return (double)GetValue(MarqueeDelayProperty); }
            set { SetValue(MarqueeDelayProperty, value); }
        }

        public double MarqueeSpeed
        {
            get { return (double)GetValue(MarqueeSpeedProperty); }
            set { SetValue(MarqueeSpeedProperty, value); }
        }

        public bool IsTextTrimmed
        {
            get { return _isTextTrimmed; }
            private set
            {
                if (_isTextTrimmed != value)
                {
                    _isTextTrimmed = value;
                    IsTextTrimmedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler IsTextTrimmedChanged;

        // 构造函数
        public TextBlockMarquee()
        {
            this.DefaultStyleKey = typeof(TextBlockMarquee);

            // 创建TextBlock
            _textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 设置内容
            this.Content = _textBlock;

            // 注册事件
            this.Loaded += TextBlockMarquee_Loaded;
            this.SizeChanged += TextBlockMarquee_SizeChanged;

            // 应用初始属性
            UpdateTextBlockProperties();
        }

        // 事件处理
        private void TextBlockMarquee_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeMarquee();
        }

        private void TextBlockMarquee_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CheckIfTextIsTrimmed();
            UpdateMarqueeAnimation();
        }

        // 属性变更处理
        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextBlockMarquee)d;
            control.UpdateTextBlockProperties();
            control.CheckIfTextIsTrimmed();
            control.UpdateMarqueeAnimation();
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextBlockMarquee)d;
            control.UpdateTextBlockProperties();
        }

        private static void OnMarqueePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextBlockMarquee)d;
            control.UpdateMarqueeAnimation();
        }

        // 更新TextBlock属性
        private void UpdateTextBlockProperties()
        {
            if (_textBlock != null)
            {
                _textBlock.Text = Text;
                _textBlock.FontSize = FontSize;
                _textBlock.FontWeight = FontWeight;
                _textBlock.Foreground = Foreground;
            }
        }

        // 检查文本是否被截断
        private void CheckIfTextIsTrimmed()
        {
            if (_textBlock == null || this.ActualWidth <= 0)
                return;

            // 测量文本宽度
            _textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = _textBlock.DesiredSize.Width;

            IsTextTrimmed = textWidth > this.ActualWidth;
            Debug.WriteLine($"文本宽度: {textWidth}, 控件宽度: {this.ActualWidth}, 是否截断: {IsTextTrimmed}");
        }

        // 初始化滚动动画
        private void InitializeMarquee()
        {
            if (_isInitialized || _textBlock == null)
                return;

            _storyboard = new Storyboard();
            _animation = new DoubleAnimation();

            Storyboard.SetTarget(_animation, _textBlock);
            Storyboard.SetTargetProperty(_animation, "(UIElement.RenderTransform).(TranslateTransform.X)");

            _storyboard.Children.Add(_animation);
            _textBlock.RenderTransform = new TranslateTransform();

            _isInitialized = true;
            UpdateMarqueeAnimation();
        }

        // 更新滚动动画
        private void UpdateMarqueeAnimation()
        {
            if (!_isInitialized || _textBlock == null)
                return;

            _storyboard.Stop();

            if (IsTextTrimmed)
            {
                // 测量文本宽度
                _textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double textWidth = _textBlock.DesiredSize.Width;

                // 设置动画参数
                _animation.From = 0;
                _animation.To = -(textWidth + 20); // 额外空间确保完全滚动
                _animation.Duration = new Duration(TimeSpan.FromMilliseconds(textWidth * MarqueeSpeed));
                _animation.RepeatBehavior = RepeatBehavior.Forever;
                _animation.BeginTime = TimeSpan.FromMilliseconds(MarqueeDelay);

                // 开始动画
                _storyboard.Begin();
                Debug.WriteLine("开始文本滚动动画");
            }
        }
    }
}
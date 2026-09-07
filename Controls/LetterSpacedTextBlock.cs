using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;

namespace SecSeeTime.Controls
{
    public sealed class LetterSpacedTextBlock : System.Windows.Controls.UserControl
    {
        private readonly System.Windows.Controls.StackPanel _panel = new() { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(LetterSpacedTextBlock), new PropertyMetadata("", (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static readonly DependencyProperty LetterSpacingProperty = DependencyProperty.Register(nameof(LetterSpacing), typeof(double), typeof(LetterSpacedTextBlock), new PropertyMetadata(0d, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static new readonly DependencyProperty FontFamilyProperty = TextBlock.FontFamilyProperty.AddOwner(typeof(LetterSpacedTextBlock), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static new readonly DependencyProperty FontSizeProperty = TextBlock.FontSizeProperty.AddOwner(typeof(LetterSpacedTextBlock), new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static new readonly DependencyProperty FontWeightProperty = TextBlock.FontWeightProperty.AddOwner(typeof(LetterSpacedTextBlock), new FrameworkPropertyMetadata(FontWeights.Normal, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static new readonly DependencyProperty FontStyleProperty = TextBlock.FontStyleProperty.AddOwner(typeof(LetterSpacedTextBlock), new FrameworkPropertyMetadata(FontStyles.Normal, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static new readonly DependencyProperty ForegroundProperty = TextBlock.ForegroundProperty.AddOwner(typeof(LetterSpacedTextBlock), new FrameworkPropertyMetadata(Brushes.White, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static readonly DependencyProperty OutlineEnabledProperty = DependencyProperty.Register(nameof(OutlineEnabled), typeof(bool), typeof(LetterSpacedTextBlock), new PropertyMetadata(false, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static readonly DependencyProperty OutlineThicknessProperty = DependencyProperty.Register(nameof(OutlineThickness), typeof(double), typeof(LetterSpacedTextBlock), new PropertyMetadata(1d, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));
        public static readonly DependencyProperty OutlineColorProperty = DependencyProperty.Register(nameof(OutlineColor), typeof(System.Windows.Media.Brush), typeof(LetterSpacedTextBlock), new PropertyMetadata(Brushes.Black, (_,__) => ((LetterSpacedTextBlock)_).Rebuild()));

        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public double LetterSpacing { get => (double)GetValue(LetterSpacingProperty); set => SetValue(LetterSpacingProperty, value); }
        public new System.Windows.Media.FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
        public new double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
        public new System.Windows.FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
        public new System.Windows.FontStyle FontStyle { get => (FontStyle)GetValue(FontStyleProperty); set => SetValue(FontStyleProperty, value); }
        public new System.Windows.Media.Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
        public bool OutlineEnabled { get => (bool)GetValue(OutlineEnabledProperty); set => SetValue(OutlineEnabledProperty, value); }
        public double OutlineThickness { get => (double)GetValue(OutlineThicknessProperty); set => SetValue(OutlineThicknessProperty, value); }
        public System.Windows.Media.Brush OutlineColor { get => (Brush)GetValue(OutlineColorProperty); set => SetValue(OutlineColorProperty, value); }

        public LetterSpacedTextBlock() => Content = _panel;

        private void Rebuild()
        {
            if (_panel == null) return;
            _panel.Children.Clear();
            string text = Text ?? "";
            for (int i = 0; i < text.Length; i++)
            {
                var tb = new TextBlock
                {
                    Text = text[i].ToString(), FontFamily = FontFamily, FontSize = FontSize,
                    FontWeight = FontWeight, FontStyle = FontStyle, Foreground = Foreground,
                    VerticalAlignment = VerticalAlignment.Center
                };
                if (OutlineEnabled)
                {
                    double d = Math.Clamp(OutlineThickness, 0.5, 6);
                    tb.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = OutlineColor is SolidColorBrush sb ? sb.Color : Colors.Black,
                        BlurRadius = 0, ShadowDepth = d, Direction = 0, Opacity = 0.9
                    };
                }
                if (i < text.Length - 1) tb.Margin = new Thickness(0, 0, LetterSpacing, 0);
                _panel.Children.Add(tb);
            }
        }
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimpleSpoutOverlay.UI.Dialogs;

public partial class ColorPickerWindow
{
    private bool _isUpdatingHexInput;
    private bool _isUpdatingFromPicker;
    private double _currentHue;
    private double _currentSaturation;
    private double _currentValue;

    public Color SelectedColor { get; private set; }

    public ColorPickerWindow(Color initialColor)
    {
        InitializeComponent();

        // Convert RGB to HSV
        RgbToHsv(initialColor.R, initialColor.G, initialColor.B, out _currentHue, out _currentSaturation, out _currentValue);
        
        HueSlider.Value = _currentHue;
        AlphaSlider.Value = initialColor.A;

        UpdatePreview();
        
        // Defer gradient update until after layout is complete
        Loaded += (_, _) => UpdateSvGradient();
    }

    private void OnSVPickerMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Single click to lock - update position and don't drag
        UpdateSvFromMouse(e);
        e.Handled = true;
    }

    private void OnSVPickerMouseMove(object sender, MouseEventArgs e)
    {
        // Only update if we're explicitly dragging with button held
        if (e is { LeftButton: MouseButtonState.Pressed, RightButton: MouseButtonState.Released })
        {
            UpdateSvFromMouse(e);
        }
    }

    private void OnSVPickerMouseUp(object sender, MouseButtonEventArgs e)
    {
        // Nothing needed here
    }

    private void UpdateSvFromMouse(MouseEventArgs e)
    {
        Point pos = e.GetPosition(SvPickerCanvas);
        double width = SvPickerCanvas.ActualWidth;
        double height = SvPickerCanvas.ActualHeight;

        _currentSaturation = Math.Max(0, Math.Min(1, pos.X / width));
        _currentValue = Math.Max(0, Math.Min(1, 1 - (pos.Y / height))); // Inverted Y

        UpdatePreview();
    }

    private void OnHueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingFromPicker)
            return;

        _currentHue = HueSlider.Value;
        UpdateSvGradient();
        UpdatePreview();
    }

    private void OnAlphaChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingFromPicker)
            return;

        UpdatePreview();
    }

    private void UpdateSvGradient()
    {
        // Draw SV gradient based on current hue
        double width = SvPickerCanvas.ActualWidth;
        double height = SvPickerCanvas.ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        WriteableBitmap bitmap = new((int)width, (int)height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.Lock();

        unsafe
        {
            IntPtr backBuffer = bitmap.BackBuffer;
            int stride = bitmap.BackBufferStride;

            for (int y = 0; y < (int)height; y++)
            {
                for (int x = 0; x < (int)width; x++)
                {
                    double s = x / width;
                    double v = 1 - (y / height);

                    Color color = HsvToRgb(_currentHue, s, v);

                    byte* pixel = (byte*)backBuffer + y * stride + x * 4;
                    pixel[0] = color.B;
                    pixel[1] = color.G;
                    pixel[2] = color.R;
                    pixel[3] = 255;
                }
            }
        }

        bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)width, (int)height));
        bitmap.Unlock();

        SvPickerCanvas.Background = new ImageBrush(bitmap);

        // Draw selection indicator
        DrawSelectionIndicator();
    }

    private void DrawSelectionIndicator()
    {
        SvPickerCanvas.Children.Clear();

        double width = SvPickerCanvas.ActualWidth;
        double height = SvPickerCanvas.ActualHeight;
        double x = _currentSaturation * width;
        double y = (1 - _currentValue) * height;

        // Simple circle indicator
        Ellipse circle = new Ellipse
        {
            Width = 10,
            Height = 10,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(circle, x - 5);
        Canvas.SetTop(circle, y - 5);
        SvPickerCanvas.Children.Add(circle);
    }

    private void UpdatePreview()
    {
        _isUpdatingFromPicker = true;

        byte a = (byte)AlphaSlider.Value;
        Color color = HsvToRgb(_currentHue, _currentSaturation, _currentValue);
        SelectedColor = Color.FromArgb(a, color.R, color.G, color.B);

        PreviewRect.Fill = new SolidColorBrush(SelectedColor);
        AlphaValueText.Text = a.ToString();

        UpdateHexInputText(color.R, color.G, color.B);
        DrawSelectionIndicator();

        _isUpdatingFromPicker = false;
    }

    private void OnHexInputChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingHexInput || _isUpdatingFromPicker)
            return;

        if (!TryParseRgbHex(HexInput.Text, out byte r, out byte g, out byte b)) return;
        
        RgbToHsv(r, g, b, out _currentHue, out _currentSaturation, out _currentValue);
        
        _isUpdatingFromPicker = true;
        HueSlider.Value = _currentHue;
        _isUpdatingFromPicker = false;
        
        UpdateSvGradient();
        UpdatePreview();
    }

    private void OnHexInputLostFocus(object sender, RoutedEventArgs e)
    {
        NormalizeHexInputText();
    }

    private void OnHexInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        NormalizeHexInputText();
        e.Handled = true;
    }

    private void NormalizeHexInputText()
    {
        if (TryParseRgbHex(HexInput.Text, out var r, out var g, out var b))
        {
            RgbToHsv(r, g, b, out _currentHue, out _currentSaturation, out _currentValue);
            _isUpdatingFromPicker = true;
            HueSlider.Value = _currentHue;
            _isUpdatingFromPicker = false;
            UpdateSvGradient();
            UpdatePreview();
            return;
        }

        Color color = HsvToRgb(_currentHue, _currentSaturation, _currentValue);
        UpdateHexInputText(color.R, color.G, color.B);
    }

    private void UpdateHexInputText(byte r, byte g, byte b)
    {
        string hex = $"#{r:X2}{g:X2}{b:X2}";
        if (HexInput.Text == hex)
            return;

        _isUpdatingHexInput = true;
        HexInput.Text = hex;
        _isUpdatingHexInput = false;
    }

    private static bool TryParseRgbHex(string? input, out byte r, out byte g, out byte b)
    {
        r = 0;
        g = 0;
        b = 0;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string hex = input.Trim();
        if (hex.StartsWith('#'))
            hex = hex[1..];

        if (hex.Length != 6)
            return false;

        if (!byte.TryParse(hex[..2], NumberStyles.HexNumber, null, out r))
            return false;
        return byte.TryParse(hex.AsSpan(2, 2), NumberStyles.HexNumber, null, out g) && byte.TryParse(hex.AsSpan(4, 2), NumberStyles.HexNumber, null, out b);
    }

    private static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rf = r / 255.0;
        double gf = g / 255.0;
        double bf = b / 255.0;

        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        // Hue
        if (delta == 0)
            h = 0;
        else if (max == rf)
            h = (60 * ((gf - bf) / delta) + 360) % 360;
        else if (max == gf)
            h = (60 * ((bf - rf) / delta) + 120) % 360;
        else
            h = (60 * ((rf - gf) / delta) + 240) % 360;

        // Saturation
        s = max == 0 ? 0 : delta / max;

        // Value
        v = max;
    }

    private static Color HsvToRgb(double h, double s, double v)
    {
        double c = v * s;
        double hp = h / 60.0;
        double x = c * (1 - Math.Abs((hp % 2) - 1));

        double r, g, b;

        switch (hp)
        {
            case < 1:
                (r, g, b) = (c, x, 0);
                break;
            case < 2:
                (r, g, b) = (x, c, 0);
                break;
            case < 3:
                (r, g, b) = (0, c, x);
                break;
            case < 4:
                (r, g, b) = (0, x, c);
                break;
            case < 5:
                (r, g, b) = (x, 0, c);
                break;
            default:
                (r, g, b) = (c, 0, x);
                break;
        }

        double m = v - c;
        return Color.FromRgb(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255)
        );
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}









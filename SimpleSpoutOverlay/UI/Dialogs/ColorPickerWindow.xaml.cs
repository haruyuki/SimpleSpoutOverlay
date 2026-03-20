using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleSpoutOverlay.UI.Dialogs;

public partial class ColorPickerWindow
{
    private bool _isUpdatingHexInput;

    public Color SelectedColor { get; private set; }

    public ColorPickerWindow(Color initialColor)
    {
        InitializeComponent();

        AlphaSlider.Value = initialColor.A;
        RedSlider.Value = initialColor.R;
        GreenSlider.Value = initialColor.G;
        BlueSlider.Value = initialColor.B;

        UpdatePreview();
    }

    private void OnChannelChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        byte a = (byte)AlphaSlider.Value;
        byte r = (byte)RedSlider.Value;
        byte g = (byte)GreenSlider.Value;
        byte b = (byte)BlueSlider.Value;

        SelectedColor = Color.FromArgb(a, r, g, b);
        PreviewRect.Fill = new SolidColorBrush(SelectedColor);

        AlphaValueText.Text = a.ToString();
        RedValueText.Text = r.ToString();
        GreenValueText.Text = g.ToString();
        BlueValueText.Text = b.ToString();

        UpdateHexInputText(r, g, b);
    }

    private void OnHexInputChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingHexInput)
            return;

        if (!TryParseRgbHex(HexInput.Text, out byte r, out byte g, out byte b)) return;
        RedSlider.Value = r;
        GreenSlider.Value = g;
        BlueSlider.Value = b;
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
            RedSlider.Value = r;
            GreenSlider.Value = g;
            BlueSlider.Value = b;
            UpdatePreview();
            return;
        }

        UpdateHexInputText((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
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

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}


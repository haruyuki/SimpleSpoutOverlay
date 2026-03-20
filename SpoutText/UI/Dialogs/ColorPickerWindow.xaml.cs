using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpoutText.UI.Dialogs;

public partial class ColorPickerWindow : Window
{
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

        HexText.Text = $"#{a:X2}{r:X2}{g:X2}{b:X2}";
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}


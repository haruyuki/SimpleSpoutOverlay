using System.Windows.Media;
using System.Windows;

namespace SimpleSpoutOverlay.Models;

/// Represents a single text layer with formatting, color, position, and outline properties.
public sealed class TextLayer(string text, string fontFamily = "Arial", double fontSize = 48) : LayerBase
{
    private string _text = text;

    // Text properties
    public string Text
    {
        get => _text;
        set
        {
            if (!SetProperty(ref _text, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayName));
        }
    }

    protected override string LayerType => "Text";

    public override string DisplayName => string.IsNullOrWhiteSpace(Text) ? "Text" : Text;

    // Formatting properties
    public string FontFamily { get; set; } = fontFamily;

    public double FontSize { get; set; } = fontSize;

    /// Multiplier applied to the base font size to control spacing between lines.
    public double LineHeightMultiplier { get; set; } = 1.0;

    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

    public Color FillColor { get; set; } = Colors.White;

    // Outline properties
    public bool OutlineEnabled { get; set; }

    public Color OutlineColor { get; set; } = Colors.Black;

    public double OutlineThickness { get; set; } = 2;
}
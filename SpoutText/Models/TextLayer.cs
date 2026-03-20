using System.Windows.Media;

namespace SpoutText.Models
{
    /// Represents a single text layer with formatting, color, position, and outline properties.
    public sealed class TextLayer(string text, string fontFamily = "Arial", double fontSize = 48) : LayerBase
    {
        // Text properties
        public string Text { get; set; } = text;

        public override string LayerType => "Text";

        public override string DisplayName => string.IsNullOrWhiteSpace(Text) ? "Text" : Text;

        // Formatting properties
        public string FontFamily { get; set; } = fontFamily;

        public double FontSize { get; set; } = fontSize;

        public Color FillColor { get; set; } = Colors.White;

        // Outline properties
        public bool OutlineEnabled { get; set; }

        public Color OutlineColor { get; set; } = Colors.Black;

        public double OutlineThickness { get; set; } = 2;
    }
}

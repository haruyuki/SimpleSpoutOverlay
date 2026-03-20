using System.Windows.Media;

namespace SpoutText.Models
{
    /// <summary>
    /// Represents a single text layer with formatting, color, position, and outline properties.
    /// </summary>
    public class TextLayer(string text, string fontFamily = "Arial", double fontSize = 48)
    {
        // Outline properties

        public string Text { get; set; } = text;

        public string FontFamily { get; set; } = fontFamily;

        public double FontSize { get; set; } = fontSize;

        public Color FillColor { get; set; } = Colors.White;

        public double PositionX { get; set; } = 10;

        public double PositionY { get; set; } = 10;

        public double ScaleX { get; set; } = 1.0;

        public double ScaleY { get; set; } = 1.0;

        public bool OutlineEnabled { get; set; }

        public Color OutlineColor { get; set; } = Colors.Black;

        public double OutlineThickness { get; set; } = 2;
    }
}


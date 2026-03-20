using System.Windows.Media;

namespace SpoutText.Models
{
    /// <summary>
    /// Represents a single text layer with formatting, color, position, and outline properties.
    /// </summary>
    public class TextLayer
    {
        private string _text = "Sample Text";
        private string _fontFamily = "Arial";
        private double _fontSize = 48;
        private Color _fillColor = Colors.White;
        private double _positionX = 10;
        private double _positionY = 10;
        private double _scaleX = 1.0;
        private double _scaleY = 1.0;

        // Outline properties
        private bool _outlineEnabled = false;
        private Color _outlineColor = Colors.Black;
        private double _outlineThickness = 2;

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        public string FontFamily
        {
            get => _fontFamily;
            set => _fontFamily = value;
        }

        public double FontSize
        {
            get => _fontSize;
            set => _fontSize = value;
        }

        public Color FillColor
        {
            get => _fillColor;
            set => _fillColor = value;
        }

        public double PositionX
        {
            get => _positionX;
            set => _positionX = value;
        }

        public double PositionY
        {
            get => _positionY;
            set => _positionY = value;
        }

        public double ScaleX
        {
            get => _scaleX;
            set => _scaleX = value;
        }

        public double ScaleY
        {
            get => _scaleY;
            set => _scaleY = value;
        }

        public bool OutlineEnabled
        {
            get => _outlineEnabled;
            set => _outlineEnabled = value;
        }

        public Color OutlineColor
        {
            get => _outlineColor;
            set => _outlineColor = value;
        }

        public double OutlineThickness
        {
            get => _outlineThickness;
            set => _outlineThickness = value;
        }

        public TextLayer()
        {
        }

        public TextLayer(string text, string fontFamily = "Arial", double fontSize = 48)
        {
            _text = text;
            _fontFamily = fontFamily;
            _fontSize = fontSize;
        }

        public TextLayer Clone()
        {
            return new TextLayer
            {
                Text = _text,
                FontFamily = _fontFamily,
                FontSize = _fontSize,
                FillColor = _fillColor,
                PositionX = _positionX,
                PositionY = _positionY,
                ScaleX = _scaleX,
                ScaleY = _scaleY,
                OutlineEnabled = _outlineEnabled,
                OutlineColor = _outlineColor,
                OutlineThickness = _outlineThickness
            };
        }
    }
}


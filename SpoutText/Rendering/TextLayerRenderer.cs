using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SpoutText.Models;

namespace SpoutText.Rendering
{
    /// <summary>
    /// Renders text layers to a bitmap with transparent background.
    /// Uses FormattedText and Geometry for outline support.
    /// </summary>
    public class TextLayerRenderer
    {
        private readonly int _width;
        private readonly int _height;
        private readonly double _dpiX;
        private readonly double _dpiY;

        public TextLayerRenderer(int width, int height, double dpiX = 96, double dpiY = 96)
        {
            _width = width;
            _height = height;
            _dpiX = dpiX;
            _dpiY = dpiY;
        }

        /// <summary>
        /// Renders all layers to a bitmap with transparent background.
        /// </summary>
        public RenderTargetBitmap RenderLayers(IEnumerable<TextLayer> layers)
        {
            var bitmap = new RenderTargetBitmap(_width, _height, _dpiX, _dpiY, PixelFormats.Pbgra32);

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Keep transparent background so future output adapters can preserve alpha.
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, _width, _height));

                foreach (var layer in layers)
                {
                    RenderTextLayer(dc, layer);
                }
            }

            bitmap.Render(visual);
            return bitmap;
        }

        private void RenderTextLayer(DrawingContext dc, TextLayer layer)
        {
            if (string.IsNullOrEmpty(layer.Text))
                return;

            var typeface = new Typeface(new FontFamily(layer.FontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            var formattedText = new FormattedText(
                layer.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                layer.FontSize,
                Brushes.Black,
                _dpiX / 96.0);

            var geometry = formattedText.BuildGeometry(new Point(0, 0));

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(layer.ScaleX, layer.ScaleY));
            transformGroup.Children.Add(new TranslateTransform(layer.PositionX, layer.PositionY));
            geometry.Transform = transformGroup;

            if (layer.OutlineEnabled && layer.OutlineThickness > 0)
            {
                var outlinePen = new Pen(new SolidColorBrush(layer.OutlineColor), layer.OutlineThickness)
                {
                    LineJoin = PenLineJoin.Round,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };

                dc.DrawGeometry(null, outlinePen, geometry);
            }

            dc.DrawGeometry(new SolidColorBrush(layer.FillColor), null, geometry);
        }

        /// <summary>
        /// Renders a single layer with outline for preview.
        /// </summary>
        public RenderTargetBitmap RenderSingleLayer(TextLayer layer)
        {
            var bitmap = new RenderTargetBitmap(_width, _height, _dpiX, _dpiY, PixelFormats.Pbgra32);

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, _width, _height));
                RenderTextLayer(dc, layer);
            }

            bitmap.Render(visual);
            return bitmap;
        }
    }
}

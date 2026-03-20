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
    public class TextLayerRenderer(int width, int height, double dpiX = 96, double dpiY = 96)
    {
        /// <summary>
        /// Renders all layers to a bitmap with transparent background.
        /// Renders in reverse order so the first item in the list appears on top visually.
        /// </summary>
        public RenderTargetBitmap RenderLayers(IEnumerable<TextLayer> layers)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, dpiX, dpiY, PixelFormats.Pbgra32);

            DrawingVisual visual = new();
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Keep transparent background so future output adapters can preserve alpha.
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

                // Render in reverse order so the first item in the list appears on top
                foreach (TextLayer layer in layers.Reverse())
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

            Typeface typeface = new Typeface(new FontFamily(layer.FontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            FormattedText formattedText = new FormattedText(
                layer.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                layer.FontSize,
                Brushes.Black,
                dpiX / 96.0);

            Geometry geometry = formattedText.BuildGeometry(new Point(0, 0));

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(layer.ScaleX, layer.ScaleY));
            transformGroup.Children.Add(new TranslateTransform(layer.PositionX, layer.PositionY));
            geometry.Transform = transformGroup;

            if (layer is { OutlineEnabled: true, OutlineThickness: > 0 })
            {
                Pen outlinePen = new(new SolidColorBrush(layer.OutlineColor), layer.OutlineThickness)
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
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, dpiX, dpiY, PixelFormats.Pbgra32);

            DrawingVisual visual = new();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                RenderTextLayer(dc, layer);
            }

            bitmap.Render(visual);
            return bitmap;
        }
    }
}

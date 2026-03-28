using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SimpleSpoutOverlay.Models;

namespace SimpleSpoutOverlay.Rendering
{
    /// Renders text and image layers to a bitmap with transparent background.
    /// Uses FormattedText and Geometry for text outline support.
    public class TextLayerRenderer(int width, int height, double dpiX = 96, double dpiY = 96)
    {
        private readonly Dictionary<string, ImageCacheEntry> _imageCache = new(StringComparer.OrdinalIgnoreCase);

        private sealed class ImageCacheEntry(BitmapSource image, DateTime lastWriteTimeUtc, long length)
        {
            public BitmapSource Image { get; } = image;
            public DateTime LastWriteTimeUtc { get; } = lastWriteTimeUtc;
            public long Length { get; } = length;
        }

        /// Renders all layers to a bitmap with transparent background.
        /// Renders in reverse order so the first item in the list appears on top visually.
        public RenderTargetBitmap RenderLayers(IEnumerable<LayerBase> layers)
        {
            RenderTargetBitmap bitmap = new(width, height, dpiX, dpiY, PixelFormats.Pbgra32);

            DrawingVisual visual = new();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

                foreach (LayerBase layer in layers.Reverse())
                {
                    RenderLayer(dc, layer);
                }
            }

            bitmap.Render(visual);
            return bitmap;
        }

        private void RenderLayer(DrawingContext dc, LayerBase layer)
        {
            switch (layer)
            {
                case TextLayer textLayer:
                    RenderTextLayer(dc, textLayer);
                    break;
                case ImageLayer imageLayer:
                    RenderImageLayer(dc, imageLayer);
                    break;
            }
        }

        private void RenderTextLayer(DrawingContext dc, TextLayer layer)
        {
            if (string.IsNullOrEmpty(layer.Text))
            {
                return;
            }

            Typeface typeface = new(new FontFamily(layer.FontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            FormattedText formattedText = new(
                layer.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                layer.FontSize,
                Brushes.Black,
                dpiX / 96.0);

            double clampedLineHeightMultiplier = Math.Max(layer.LineHeightMultiplier, 0.1);
            formattedText.LineHeight = layer.FontSize * clampedLineHeightMultiplier;

            double alignmentWidth = Math.Max(formattedText.WidthIncludingTrailingWhitespace, 1.0);
            formattedText.MaxTextWidth = alignmentWidth;
            formattedText.TextAlignment = layer.TextAlignment;

            Geometry geometry = formattedText.BuildGeometry(new Point(0, 0));

            TransformGroup transformGroup = new();
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

        private void RenderImageLayer(DrawingContext dc, ImageLayer layer)
        {
            BitmapSource? bitmap = GetImage(layer.ImagePath);
            if (bitmap == null)
            {
                return;
            }

            double drawWidth = bitmap.PixelWidth * Math.Max(layer.ScaleX, 0.01);
            double drawHeight = bitmap.PixelHeight * Math.Max(layer.ScaleY, 0.01);
            Rect destination = new(layer.PositionX, layer.PositionY, drawWidth, drawHeight);
            dc.DrawImage(bitmap, destination);
        }

        private BitmapSource? GetImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                string normalizedPath = Path.GetFullPath(path);
                if (!File.Exists(normalizedPath))
                {
                    _imageCache.Remove(normalizedPath);
                    return null;
                }

                FileInfo fileInfo = new(normalizedPath);
                if (_imageCache.TryGetValue(normalizedPath, out ImageCacheEntry? cached)
                    && cached.LastWriteTimeUtc == fileInfo.LastWriteTimeUtc
                    && cached.Length == fileInfo.Length)
                {
                    return cached.Image;
                }

                BitmapImage image = new();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(normalizedPath, UriKind.Absolute);
                image.EndInit();
                image.Freeze();

                _imageCache[normalizedPath] = new ImageCacheEntry(image, fileInfo.LastWriteTimeUtc, fileInfo.Length);
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}

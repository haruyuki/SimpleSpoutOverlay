using System.Windows.Media.Imaging;

namespace SimpleSpoutOverlay.Rendering
{
    /// Converts WPF RenderTargetBitmap to byte array format suitable for Spout output.
    /// Handles PBGRA32 format conversion to RGBA byte array.
    public static class BitmapConverter
    {
        /// Converts a RenderTargetBitmap to RGBA using caller-provided buffers to avoid per-frame allocations.
        public static void BitmapToRgbaArray(RenderTargetBitmap bitmap, byte[] bgraBuffer, byte[] rgbaBuffer)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8; // PBGRA32 = 4 bytes
            int stride = width * bytesPerPixel;
            int pixelByteCount = stride * height;

            if (bgraBuffer.Length < pixelByteCount)
            {
                throw new ArgumentException("BGRA buffer is too small for the source bitmap.", nameof(bgraBuffer));
            }

            int rgbaByteCount = width * height * 4;
            if (rgbaBuffer.Length < rgbaByteCount)
            {
                throw new ArgumentException("RGBA buffer is too small for the source bitmap.", nameof(rgbaBuffer));
            }

            bitmap.CopyPixels(bgraBuffer, stride, 0);

            // Convert PBGRA32 (premultiplied BGRA) to RGBA
            for (int sourceIndex = 0, outputIndex = 0; sourceIndex < pixelByteCount; sourceIndex += 4, outputIndex += 4)
            {
                // PBGRA format: Blue, Green, Red, Alpha (premultiplied)
                byte b = bgraBuffer[sourceIndex];
                byte g = bgraBuffer[sourceIndex + 1];
                byte r = bgraBuffer[sourceIndex + 2];
                byte a = bgraBuffer[sourceIndex + 3];

                // Un-premultiply alpha if alpha > 0
                if (a > 0)
                {
                    r = (byte)((r * 255) / a);
                    g = (byte)((g * 255) / a);
                    b = (byte)((b * 255) / a);
                }

                // Convert to RGBA
                rgbaBuffer[outputIndex] = r;
                rgbaBuffer[outputIndex + 1] = g;
                rgbaBuffer[outputIndex + 2] = b;
                rgbaBuffer[outputIndex + 3] = a;
            }
        }

    }
}


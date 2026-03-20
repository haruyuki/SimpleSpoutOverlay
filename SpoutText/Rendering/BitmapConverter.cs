using System.Windows.Media.Imaging;

namespace SpoutText.Rendering
{
    /// <summary>
    /// Converts WPF RenderTargetBitmap to byte array format suitable for Spout output.
    /// Handles PBGRA32 format conversion to RGBA byte array.
    /// </summary>
    public static class BitmapConverter
    {
        /// <summary>
        /// Converts a RenderTargetBitmap to a RGBA byte array.
        /// The bitmap should be in PBGRA32 format (with premultiplied alpha).
        /// Output is RGBA format as expected by Spout.
        /// </summary>
        public static byte[] BitmapToRgbaArray(RenderTargetBitmap bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8; // PBGRA32 = 4 bytes
            int stride = width * bytesPerPixel;

            byte[] pixelData = new byte[stride * height];
            bitmap.CopyPixels(pixelData, stride, 0);

            // Convert PBGRA32 (premultiplied BGRA) to RGBA
            byte[] rgbaData = new byte[width * height * 4];

            for (int i = 0; i < pixelData.Length; i += 4)
            {
                // PBGRA format: Blue, Green, Red, Alpha (premultiplied)
                byte b = pixelData[i];
                byte g = pixelData[i + 1];
                byte r = pixelData[i + 2];
                byte a = pixelData[i + 3];

                // Un-premultiply alpha if alpha > 0
                if (a > 0)
                {
                    r = (byte)((r * 255) / a);
                    g = (byte)((g * 255) / a);
                    b = (byte)((b * 255) / a);
                }

                // Convert to RGBA
                int outputIndex = (i / 4) * 4;
                rgbaData[outputIndex] = r;
                rgbaData[outputIndex + 1] = g;
                rgbaData[outputIndex + 2] = b;
                rgbaData[outputIndex + 3] = a;
            }

            return rgbaData;
        }
    }
}


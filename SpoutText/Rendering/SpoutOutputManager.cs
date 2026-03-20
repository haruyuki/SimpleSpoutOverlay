using System.Diagnostics;
using System.Windows.Media.Imaging;
using Spout.Interop;
using Spout.NETCore;

namespace SpoutText.Rendering
{
    /// <summary>
    /// Manages Spout2 output for text layer rendering.
    /// Sends rendered frames to Spout for external application consumption.
    /// </summary>
    public sealed class SpoutOutputManager(int width, int height) : IDisposable
    {
        private SpoutSender? _sender;
        private bool _isInitialized;
        private bool _disposed;
        private byte[]? _bgraBuffer;
        private byte[]? _rgbaBuffer;

        /// <summary>
        /// Initializes the Spout sender with the specified name.
        /// Must be called before SendFrame.
        /// </summary>
        public bool Initialize(string senderName = "SpoutText")
        {
            if (_isInitialized)
                return true;

            try
            {
                _sender = new SpoutSender();

                // First attempt: default GPU interop mode.
                bool created = _sender.CreateSender(senderName, (uint)width, (uint)height, 0);

                // Fallback: memory-share mode can work on setups where GL/DX interop is unavailable.
                if (!created)
                {
                    _sender.MemoryShareMode = true;
                    created = _sender.CreateSender(senderName, (uint)width, (uint)height, 0);
                }

                if (!created)
                {
                    _sender.CPUmode = true;
                    created = _sender.CreateSender(senderName, (uint)width, (uint)height, 0);
                }

                if (!created)
                {
                    Debug.WriteLine(
                        $"Failed to create Spout sender. " +
                        $"IsGLDXready={_sender.IsGLDXready}, " +
                        $"MemoryShareMode={_sender.MemoryShareMode}, " +
                        $"CPUmode={_sender.CPUmode}, " +
                        $"ShareMode={_sender.ShareMode}");
                    _sender.Dispose();
                    _sender = null;
                    return false;
                }

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Spout: {ex.Message}");
                _sender?.Dispose();
                _sender = null;
                return false;
            }
        }

        /// <summary>
        /// Sends a rendered frame to Spout.
        /// Converts the WPF RenderTargetBitmap to RGBA format and sends it.
        /// </summary>
        public void SendFrame(RenderTargetBitmap bitmap)
        {
            if (!_isInitialized || _sender == null)
            {
                Debug.WriteLine("Spout not initialized or sender is null");
                return;
            }

            if (bitmap.PixelWidth != width || bitmap.PixelHeight != height)
            {
                Debug.WriteLine($"Bitmap size mismatch: expected {width}x{height}, got {bitmap.PixelWidth}x{bitmap.PixelHeight}");
                return;
            }

            try
            {
                EnsureBuffers(bitmap);
                byte[] bgraBuffer = _bgraBuffer!;
                byte[] rgbaBuffer = _rgbaBuffer!;
                BitmapConverter.BitmapToRgbaArray(bitmap, bgraBuffer, rgbaBuffer);

                unsafe
                {
                    fixed (byte* pData = rgbaBuffer)
                    {
                        _sender.SendImage(
                            pData,
                            (uint)width,
                            (uint)height,
                            GLFormats.RGBA,
                            false, // WPF pixel buffer is already in top-left origin for our receiver path.
                            0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending Spout frame: {ex.Message}");
            }
        }

        private void EnsureBuffers(RenderTargetBitmap bitmap)
        {
            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
            int bgraLength = bitmap.PixelWidth * bitmap.PixelHeight * bytesPerPixel;
            int rgbaLength = bitmap.PixelWidth * bitmap.PixelHeight * 4;

            if (_bgraBuffer == null || _bgraBuffer.Length != bgraLength)
            {
                _bgraBuffer = new byte[bgraLength];
            }

            if (_rgbaBuffer == null || _rgbaBuffer.Length != rgbaLength)
            {
                _rgbaBuffer = new byte[rgbaLength];
            }
        }

        /// <summary>
        /// Releases the Spout sender resources.
        /// </summary>
        public void Shutdown()
        {
            if (_sender == null)
                return;

            try
            {
                _sender.ReleaseSender();
                _sender.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error shutting down Spout: {ex.Message}");
            }
            finally
            {
                _sender = null;
                _isInitialized = false;
                _bgraBuffer = null;
                _rgbaBuffer = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Shutdown();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

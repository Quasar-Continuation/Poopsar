using Pulsar.Common.Video;
using Pulsar.Common.Video.Compression;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;

namespace Pulsar.Common.Video.Codecs
{
    /// <summary>
    /// Hybrid PNG/JPEG codec used by remote desktop streaming. JPEG retains the original incremental block
    /// encoding for performance while always returning fresh Bitmap instances to avoid lock contention.
    /// </summary>
    public class UnsafeStreamCodec : IDisposable
    {
        public int Monitor { get; }

        public Resolution Resolution { get; }

        public Size CheckBlock { get; }

        public RemoteDesktopImageFormat CompressionFormat { get; }

        public int ImageQuality
        {
            get => _imageQuality;
            private set
            {
                if (_imageQuality == value)
                {
                    return;
                }

                _imageQuality = value;

                if (CompressionFormat == RemoteDesktopImageFormat.Jpeg)
                {
                    lock (_imageProcessLock)
                    {
                        _jpgCompression?.Dispose();
                        _jpgCompression = new JpgCompression(_imageQuality);
                    }
                }
            }
        }

        private int _imageQuality;
        private byte[] _encodeBuffer;
        private Bitmap _decodedBitmap;
        private PixelFormat _encodedFormat;
        private int _encodedWidth;
        private int _encodedHeight;
        private readonly object _imageProcessLock = new object();
        private readonly object _decodeLock = new object();
        private JpgCompression _jpgCompression;
        private bool _disposed;

        private readonly List<Rectangle> _workingBlocks = new List<Rectangle>();
        private readonly List<Rectangle> _finalUpdates = new List<Rectangle>();

        private readonly byte[] _metadataBuffer = new byte[20];
        private readonly byte[] _lengthBuffer = new byte[4];

        public UnsafeStreamCodec(int imageQuality, int monitor, Resolution resolution, RemoteDesktopImageFormat compressionFormat = RemoteDesktopImageFormat.Png)
        {
            if (imageQuality < 0 || imageQuality > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(imageQuality), "Image quality must be between 0 and 100");
            }

            Monitor = monitor;
            Resolution = resolution;
            CompressionFormat = compressionFormat;
            CheckBlock = new Size(50, 1);

            ImageQuality = imageQuality;

            if (compressionFormat == RemoteDesktopImageFormat.Jpeg && _jpgCompression == null)
            {
                _jpgCompression = new JpgCompression(_imageQuality);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _decodedBitmap?.Dispose();
                _decodedBitmap = null;
                _jpgCompression?.Dispose();
                _jpgCompression = null;
            }

            _disposed = true;
        }

        public void CodeImage(IntPtr scan0, int stride, Rectangle scanArea, Size imageSize, PixelFormat format, Stream outStream)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnsafeStreamCodec));
            }

            if (outStream == null)
            {
                throw new ArgumentNullException(nameof(outStream));
            }

            if (!outStream.CanWrite)
            {
                throw new ArgumentException("Stream must be writable", nameof(outStream));
            }

            if (CompressionFormat == RemoteDesktopImageFormat.Png)
            {
                EncodePng(scan0, stride, scanArea, imageSize, format, outStream);
            }
            else
            {
                EncodeJpeg(scan0, scanArea, imageSize, format, outStream);
            }
        }

        public Bitmap DecodeData(Stream inStream)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnsafeStreamCodec));
            }

            if (inStream == null)
            {
                throw new ArgumentNullException(nameof(inStream));
            }

            return CompressionFormat == RemoteDesktopImageFormat.Png ? DecodePng(inStream) : DecodeJpeg(inStream);
        }

        private void EncodePng(IntPtr scan0, int stride, Rectangle scanArea, Size imageSize, PixelFormat format, Stream outStream)
        {
            lock (_imageProcessLock)
            {
                var bounded = Rectangle.Intersect(new Rectangle(Point.Empty, imageSize), scanArea);
                if (bounded.Width <= 0 || bounded.Height <= 0)
                {
                    bounded = new Rectangle(0, 0, imageSize.Width, imageSize.Height);
                }

                if (outStream.CanSeek)
                {
                    outStream.Position = 0;
                    outStream.SetLength(0);
                }

                using (var source = new Bitmap(imageSize.Width, imageSize.Height, stride, format, scan0))
                using (var frame = new Bitmap(bounded.Width, bounded.Height, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(frame))
                    {
                        g.DrawImage(source,
                            new Rectangle(0, 0, bounded.Width, bounded.Height),
                            bounded,
                            GraphicsUnit.Pixel);
                    }

                    frame.Save(outStream, ImageFormat.Png);
                }
            }
        }

        private unsafe void EncodeJpeg(IntPtr scan0, Rectangle scanArea, Size imageSize, PixelFormat format, Stream outStream)
        {
            if (CompressionFormat != RemoteDesktopImageFormat.Jpeg)
            {
                throw new InvalidOperationException("JPEG encoding requested for non-JPEG codec.");
            }

            if (_jpgCompression == null)
            {
                throw new InvalidOperationException("JPEG compression pipeline not initialised.");
            }

            lock (_imageProcessLock)
            {
                byte* pScan0 = GetScan0Pointer(scan0);
                int pixelSize = GetPixelSize(format);
                int stride = imageSize.Width * pixelSize;
                int rawLength = stride * imageSize.Height;

                if (_encodeBuffer == null)
                {
                    InitializeEncodeBuffer(scan0, imageSize, format, stride, rawLength, outStream);
                    return;
                }

                ValidateImageFormat(format, imageSize);

                long startPosition = outStream.Position;
                outStream.Write(new byte[4], 0, 4);

                ProcessImageBlocks(pScan0, scanArea, stride, pixelSize, outStream);

                long totalLength = outStream.Position - startPosition - 4;
                outStream.Position = startPosition;
                outStream.Write(BitConverter.GetBytes(totalLength), 0, 4);
                outStream.Position = startPosition + 4 + totalLength;
            }
        }

        private Bitmap DecodePng(Stream inStream)
        {
            lock (_decodeLock)
            {
                if (inStream.CanSeek)
                {
                    inStream.Position = 0;
                }

                using (var image = Image.FromStream(inStream, useEmbeddedColorManagement: false, validateImageData: false))
                {
                    var clone = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(clone))
                    {
                        g.DrawImage(image, 0, 0, image.Width, image.Height);
                    }

                    return clone;
                }
            }
        }

        private Bitmap DecodeJpeg(Stream inStream)
        {
            lock (_decodeLock)
            {
                inStream.Read(_lengthBuffer, 0, 4);
                int dataSize = BitConverter.ToInt32(_lengthBuffer, 0);

                if (_decodedBitmap == null)
                {
                    byte[] temp = new byte[dataSize];
                    inStream.Read(temp, 0, dataSize);

                    using (MemoryStream stream = new MemoryStream(temp, 0, dataSize))
                    {
                        _decodedBitmap = EnsureWritableBitmap((Bitmap)Bitmap.FromStream(stream));
                    }

                    return CloneDecodedFrame();
                }

                using (Graphics graphics = Graphics.FromImage(_decodedBitmap))
                {
                    DecodeBlocks(inStream, graphics, dataSize);
                }

                return CloneDecodedFrame();
            }
        }

        private Bitmap CloneDecodedFrame()
        {
            if (_decodedBitmap == null)
            {
                throw new InvalidOperationException("Decoder has not produced a frame yet.");
            }

            var rect = new Rectangle(0, 0, _decodedBitmap.Width, _decodedBitmap.Height);
            var pixelFormat = _decodedBitmap.PixelFormat;

            if (pixelFormat != PixelFormat.Undefined)
            {
                try
                {
                    return _decodedBitmap.Clone(rect, pixelFormat);
                }
                catch (OutOfMemoryException)
                {
                    // Fall through to ARGB promotion.
                }
                catch (ArgumentException)
                {
                    // Some indexed/extended formats cannot be cloned directly; promote below.
                }
            }

            return _decodedBitmap.Clone(rect, PixelFormat.Format32bppArgb);
        }

        private static Bitmap EnsureWritableBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            var format = bitmap.PixelFormat;
            if (format == PixelFormat.Format32bppArgb || format == PixelFormat.Format32bppPArgb)
            {
                return bitmap;
            }

            Bitmap converted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(converted))
            {
                g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
            }

            bitmap.Dispose();
            return converted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPixelSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Format24bppRgb:
                    return 3;
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    return 4;
                default:
                    throw new NotSupportedException($"Pixel format {format} is not supported");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe byte* GetScan0Pointer(IntPtr scan0)
        {
            return IntPtr.Size == 8 ? (byte*)scan0.ToInt64() : (byte*)scan0.ToInt32();
        }

        private unsafe void InitializeEncodeBuffer(IntPtr scan0, Size imageSize, PixelFormat format, int stride, int rawLength, Stream outStream)
        {
            _encodedFormat = format;
            _encodedWidth = imageSize.Width;
            _encodedHeight = imageSize.Height;
            _encodeBuffer = new byte[rawLength];

            fixed (byte* ptr = _encodeBuffer)
            {
                using (Bitmap tmpBmp = new Bitmap(imageSize.Width, imageSize.Height, stride, format, scan0))
                {
                    byte[] compressed = _jpgCompression.Compress(tmpBmp);

                    outStream.Write(BitConverter.GetBytes(compressed.Length), 0, 4);
                    outStream.Write(compressed, 0, compressed.Length);
                    NativeMethods.memcpy(new IntPtr(ptr), scan0, (uint)rawLength);
                }
            }
        }

        private void ValidateImageFormat(PixelFormat format, Size imageSize)
        {
            if (_encodedFormat != format)
            {
                throw new InvalidOperationException("PixelFormat differs from previous bitmap");
            }

            if (_encodedWidth != imageSize.Width || _encodedHeight != imageSize.Height)
            {
                throw new InvalidOperationException("Bitmap dimensions differ from previous bitmap");
            }
        }

        private unsafe void ProcessImageBlocks(byte* pScan0, Rectangle scanArea, int stride, int pixelSize, Stream outStream)
        {
            _workingBlocks.Clear();
            _finalUpdates.Clear();

            fixed (byte* encBuffer = _encodeBuffer)
            {
                DetectChangedRows(encBuffer, pScan0, scanArea, stride, pixelSize);
                DetectChangedColumns(encBuffer, pScan0, scanArea, stride, pixelSize);
                WriteCompressedBlocks(pScan0, stride, pixelSize, outStream);
            }
        }

        private unsafe void DetectChangedRows(byte* encBuffer, byte* pScan0, Rectangle scanArea, int stride, int pixelSize)
        {
            Size blockSize = new Size(scanArea.Width, CheckBlock.Height);
            Size lastSize = new Size(scanArea.Width % CheckBlock.Width, scanArea.Height % CheckBlock.Height);
            int lastY = scanArea.Height - lastSize.Height;

            for (int y = scanArea.Y; y < scanArea.Height; y += blockSize.Height)
            {
                if (y == lastY)
                {
                    blockSize = new Size(scanArea.Width, lastSize.Height);
                }

                Rectangle currentBlock = new Rectangle(scanArea.X, y, scanArea.Width, blockSize.Height);
                int offset = (y * stride) + (scanArea.X * pixelSize);

                if (NativeMethods.memcmp(encBuffer + offset, pScan0 + offset, (uint)stride) != 0)
                {
                    if (_workingBlocks.Count > 0)
                    {
                        int lastIndex = _workingBlocks.Count - 1;
                        Rectangle lastBlock = _workingBlocks[lastIndex];

                        if (lastBlock.Y + lastBlock.Height == currentBlock.Y)
                        {
                            _workingBlocks[lastIndex] = new Rectangle(lastBlock.X, lastBlock.Y, lastBlock.Width, lastBlock.Height + currentBlock.Height);
                            continue;
                        }
                    }

                    _workingBlocks.Add(currentBlock);
                }
            }
        }

        private unsafe void DetectChangedColumns(byte* encBuffer, byte* pScan0, Rectangle scanArea, int stride, int pixelSize)
        {
            Size lastSize = new Size(scanArea.Width % CheckBlock.Width, scanArea.Height % CheckBlock.Height);
            int lastX = scanArea.Width - lastSize.Width;

            for (int i = 0; i < _workingBlocks.Count; i++)
            {
                Rectangle block = _workingBlocks[i];
                Size columnSize = new Size(CheckBlock.Width, block.Height);

                for (int x = scanArea.X; x < scanArea.Width; x += columnSize.Width)
                {
                    if (x == lastX)
                    {
                        columnSize = new Size(lastSize.Width, block.Height);
                    }

                    Rectangle currentBlock = new Rectangle(x, block.Y, columnSize.Width, block.Height);

                    if (HasBlockChanged(encBuffer, pScan0, currentBlock, stride, pixelSize))
                    {
                        UpdateEncodeBuffer(encBuffer, pScan0, currentBlock, stride, pixelSize);
                        MergeOrAddBlock(currentBlock);
                    }
                }
            }
        }

        private unsafe bool HasBlockChanged(byte* encBuffer, byte* pScan0, Rectangle block, int stride, int pixelSize)
        {
            uint blockStride = (uint)(pixelSize * block.Width);

            for (int j = 0; j < block.Height; j++)
            {
                int blockOffset = (stride * (block.Y + j)) + (pixelSize * block.X);
                if (NativeMethods.memcmp(encBuffer + blockOffset, pScan0 + blockOffset, blockStride) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private unsafe void UpdateEncodeBuffer(byte* encBuffer, byte* pScan0, Rectangle block, int stride, int pixelSize)
        {
            uint blockStride = (uint)(pixelSize * block.Width);

            for (int j = 0; j < block.Height; j++)
            {
                int blockOffset = (stride * (block.Y + j)) + (pixelSize * block.X);
                NativeMethods.memcpy(encBuffer + blockOffset, pScan0 + blockOffset, blockStride);
            }
        }

        private void MergeOrAddBlock(Rectangle currentBlock)
        {
            if (_finalUpdates.Count > 0)
            {
                int lastIndex = _finalUpdates.Count - 1;
                Rectangle lastBlock = _finalUpdates[lastIndex];

                if (lastBlock.X + lastBlock.Width == currentBlock.X && lastBlock.Y == currentBlock.Y)
                {
                    _finalUpdates[lastIndex] = new Rectangle(lastBlock.X, lastBlock.Y, lastBlock.Width + currentBlock.Width, lastBlock.Height);
                    return;
                }
            }

            _finalUpdates.Add(currentBlock);
        }

        private unsafe void WriteCompressedBlocks(byte* pScan0, int stride, int pixelSize, Stream outStream)
        {
            for (int i = 0; i < _finalUpdates.Count; i++)
            {
                WriteCompressedBlock(pScan0, _finalUpdates[i], stride, pixelSize, outStream);
            }
        }

        private unsafe void WriteCompressedBlock(byte* pScan0, Rectangle rect, int stride, int pixelSize, Stream outStream)
        {
            int blockStride = pixelSize * rect.Width;

            Array.Copy(BitConverter.GetBytes(rect.X), 0, _metadataBuffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(rect.Y), 0, _metadataBuffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(rect.Width), 0, _metadataBuffer, 8, 4);
            Array.Copy(BitConverter.GetBytes(rect.Height), 0, _metadataBuffer, 12, 4);

            outStream.Write(_metadataBuffer, 0, 20);

            long lengthPosition = outStream.Position - 4;
            long dataStart = outStream.Position;

            Bitmap tmpBmp = null;
            BitmapData tmpData = null;

            try
            {
                tmpBmp = new Bitmap(rect.Width, rect.Height, _encodedFormat);
                tmpData = tmpBmp.LockBits(new Rectangle(0, 0, rect.Width, rect.Height), ImageLockMode.ReadWrite, tmpBmp.PixelFormat);

                CopyBlockData(pScan0, (byte*)tmpData.Scan0.ToPointer(), rect, stride, blockStride);
                _jpgCompression.Compress(tmpBmp, outStream);
            }
            finally
            {
                if (tmpData != null)
                {
                    tmpBmp.UnlockBits(tmpData);
                }

                tmpBmp?.Dispose();
            }

            long dataLength = outStream.Position - dataStart;
            long currentPosition = outStream.Position;
            outStream.Position = lengthPosition;
            outStream.Write(BitConverter.GetBytes(dataLength), 0, 4);
            outStream.Position = currentPosition;
        }

        private unsafe void CopyBlockData(byte* source, byte* destination, Rectangle rect, int stride, int blockStride)
        {
            for (int j = 0, offset = 0; j < rect.Height; j++)
            {
                int blockOffset = (stride * (rect.Y + j)) + (rect.X * GetPixelSize(_encodedFormat));
                NativeMethods.memcpy(destination + offset, source + blockOffset, (uint)blockStride);
                offset += blockStride;
            }
        }

        private void DecodeBlocks(Stream inStream, Graphics graphics, int remainingData)
        {
            while (remainingData > 0)
            {
                inStream.Read(_metadataBuffer, 0, 20);

                Rectangle rect = new Rectangle(
                    BitConverter.ToInt32(_metadataBuffer, 0),
                    BitConverter.ToInt32(_metadataBuffer, 4),
                    BitConverter.ToInt32(_metadataBuffer, 8),
                    BitConverter.ToInt32(_metadataBuffer, 12));

                int updateLength = BitConverter.ToInt32(_metadataBuffer, 16);

                byte[] buffer = new byte[updateLength];
                inStream.Read(buffer, 0, updateLength);

                using (MemoryStream stream = new MemoryStream(buffer, 0, updateLength))
                using (Bitmap bitmap = (Bitmap)Image.FromStream(stream))
                {
                    graphics.DrawImage(bitmap, rect.Location);
                }

                remainingData -= updateLength + 20;
            }
        }
    }
}
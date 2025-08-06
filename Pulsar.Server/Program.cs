using Pulsar.Server.DiscordRPC;
using Pulsar.Server.Forms;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pulsar.Server
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (FrmMain mainForm = new FrmMain())
            {
                ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);

                DiscordRPCManager.Initialize(mainForm);

                Application.Run(mainForm);
                
                DiscordRPCManager.Shutdown();
            }
        }
    }
}
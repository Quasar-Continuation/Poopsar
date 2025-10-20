using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar.Server.Plugins
{
    internal sealed class PluginManager
    {
        private readonly IServerContext _context;
        private readonly List<IServerPlugin> _plugins = new List<IServerPlugin>();
        private FileSystemWatcher _watcher;
        private readonly object _lock = new object();
        public event EventHandler PluginsChanged;

        public PluginManager(IServerContext context)
        {
            _context = context;
        }

        public IReadOnlyList<IServerPlugin> Plugins => _plugins;

        public void LoadFrom(string folder)
        {
            try
            {
                if (!Directory.Exists(folder)) 
                {
                    Directory.CreateDirectory(folder);
                    _context.Log("Created plugin directory: " + folder);
                }
                
                var enabledDlls = Directory.EnumerateFiles(folder, "*.dll", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                    .Where(f => !IsClientPluginFile(f))
                    .OrderBy(Path.GetFileName)
                    .ToList();

                _context.Log($"Found {enabledDlls.Count} enabled DLL files in: {folder}");
                
                foreach (var dll in enabledDlls)
                {
                    _context.Log("Attempting to load: " + Path.GetFileName(dll));
                    TryLoadDll(dll);
                }

                _context.Log($"Loaded {_plugins.Count} plugins successfully");
                StartWatcher(folder);
            }
            catch (Exception ex)
            {
                _context.Log("PluginManager error: " + ex.Message);
            }
        }

        private void StartWatcher(string folder)
        {
            try
            {
                _watcher = new FileSystemWatcher(folder);
                _watcher.Filter = "*.dll*";
                _watcher.IncludeSubdirectories = false;
                _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                _watcher.Created += OnFileChanged;
                _watcher.Changed += OnFileChanged;
                _watcher.Deleted += OnFileChanged;
                _watcher.Renamed += OnFileRenamed;
                _watcher.Error += OnWatcherError;
                _watcher.EnableRaisingEvents = true;
                _context.Log("Plugin watcher started for: " + folder);
            }
            catch (Exception ex)
            {
                _context.Log("Plugin watcher error: " + ex.Message);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (IsClientPluginFile(e.FullPath))
            {
                _context.Log($"Ignoring client plugin file {e.ChangeType}: {e.Name}");
                return;
            }
            _context.Log($"File {e.ChangeType}: {e.Name}");
            Task.Delay(500).ContinueWith(_ => 
            {
                try
                {
                    ReloadPlugins();
                }
                catch (Exception ex)
                {
                    _context.Log($"Plugin reload error: {ex.Message}");
                }
            });
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (IsClientPluginFile(e.FullPath) && IsClientPluginFile(e.OldFullPath))
            {
                _context.Log($"Ignoring client plugin rename: {e.OldName} -> {e.Name}");
                return;
            }
            _context.Log($"File renamed: {e.OldName} -> {e.Name}");
            Task.Delay(500).ContinueWith(_ => 
            {
                try
                {
                    ReloadPlugins();
                }
                catch (Exception ex)
                {
                    _context.Log($"Plugin reload error: {ex.Message}");
                }
            });
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            _context.Log($"Plugin watcher error: {e.GetException().Message}");
            try
            {
                _watcher?.Dispose();
                var folder = Path.GetDirectoryName(_watcher?.Path ?? "");
                if (!string.IsNullOrEmpty(folder))
                {
                    StartWatcher(folder);
                }
            }
            catch (Exception ex)
            {
                _context.Log($"Failed to restart watcher: {ex.Message}");
            }
        }

        public void ReloadPlugins()
        {
            lock (_lock)
            {
                try
                {
                    var folder = Path.GetDirectoryName(_watcher.Path);
                    if (!Directory.Exists(folder))
                    {
                        _context.Log("Plugin directory does not exist: " + folder);
                        return;
                    }
                    
                    var enabledDlls = Directory.EnumerateFiles(folder, "*.dll", SearchOption.TopDirectoryOnly)
                        .Where(f => !f.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                        .Where(f => !IsClientPluginFile(f))
                        .OrderBy(Path.GetFileName)
                        .ToList();

                    var currentPluginNames = _plugins.Select(p => p.Name).ToHashSet();
                    var currentDllNames = enabledDlls.Select(Path.GetFileName).ToHashSet();
                    var newPlugins = new List<string>();
                    var removedPlugins = new List<string>();

                    var pluginsToRemove = _plugins.Where(p => 
                    {
                        var dllName = p.GetType().Assembly.GetName().Name + ".dll";
                        return !currentDllNames.Contains(dllName);
                    }).ToList();

                    foreach (var plugin in pluginsToRemove)
                    {
                        removedPlugins.Add(plugin.Name);
                        
                        if (plugin is IUIExtensionPlugin uiPlugin)
                        {
                            UIExtensionManager.UnregisterUIExtension(uiPlugin);
                            _context.Log("Unregistered UI extension: " + plugin.Name);
                        }
                        
                        _plugins.Remove(plugin);
                        _context.Log($"Plugin removed: {plugin.Name}");
                    }

                    foreach (var dll in enabledDlls)
                    {
                        var pluginName = TryLoadDll(dll);
                        if (!string.IsNullOrEmpty(pluginName) && !currentPluginNames.Contains(pluginName))
                        {
                            newPlugins.Add(pluginName);
                        }
                    }

                    if (newPlugins.Count > 0 || removedPlugins.Count > 0)
                    {
                        var changes = new List<string>();
                        
                        if (newPlugins.Count > 0)
                        {
                            var pluginList = string.Join(", ", newPlugins.Take(5));
                            var moreText = newPlugins.Count > 5 ? $" and {newPlugins.Count - 5} more" : "";
                            changes.Add($"Added {newPlugins.Count} plugin{(newPlugins.Count > 1 ? "s" : "")}: {pluginList}{moreText}");
                        }
                        
                        if (removedPlugins.Count > 0)
                        {
                            var removedList = string.Join(", ", removedPlugins.Take(5));
                            var moreRemoved = removedPlugins.Count > 5 ? $" and {removedPlugins.Count - 5} more" : "";
                            changes.Add($"Removed {removedPlugins.Count} plugin{(removedPlugins.Count > 1 ? "s" : "")}: {removedList}{moreRemoved}");
                        }

                        _context.Log(string.Join("; ", changes));
                    }
                    else
                    {
                        _context.Log($"Plugin scan complete: {_plugins.Count} plugins loaded");
                    }
                    
                    PluginsChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    _context.Log("Plugin reload error: " + ex.Message);
                    PluginsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string TryLoadDll(string path)
        {
            try
            {
                if (IsClientPluginFile(path))
                {
                    _context.Log($"Skipping client plugin assembly for server load: {Path.GetFileName(path)}");
                    return null;
                }

                var dllName = Path.GetFileName(path);
                var alreadyLoaded = _plugins.Any(p => 
                {
                    var loadedDllName = p.GetType().Assembly.GetName().Name + ".dll";
                    return string.Equals(loadedDllName, dllName, StringComparison.OrdinalIgnoreCase);
                });

                if (alreadyLoaded)
                {
                    return null;
                }

                var asm = Assembly.LoadFrom(path);
                var types = asm.GetTypes().Where(t => !t.IsAbstract && typeof(IServerPlugin).IsAssignableFrom(t));
                string loadedPlugin = null;
                foreach (var t in types)
                {
                    var pluginName = TryInit(t, Path.GetFileName(path));
                    if (!string.IsNullOrEmpty(pluginName))
                        loadedPlugin = pluginName;
                }
                return loadedPlugin;
            }
            catch (ReflectionTypeLoadException rtle)
            {
                _context.Log("Plugin load error: " + rtle.Message);
                foreach (var e in rtle.LoaderExceptions)
                    _context.Log("  " + e?.Message);
            }
            catch (Exception ex)
            {
                _context.Log("Plugin load error: " + ex.Message);
            }
            return null;
        }

        private string TryInit(Type t, string source)
        {
            try
            {
                if (Activator.CreateInstance(t) is IServerPlugin p)
                {
                    p.Initialize(_context);
                    _plugins.Add(p);
                    _context.Log("Loaded plugin '" + p.Name + "' " + p.Version + " from " + source);
                    
                    if (p is IUIExtensionPlugin uiPlugin)
                    {
                        UIExtensionManager.RegisterUIExtension(uiPlugin);
                        _context.Log("Registered UI extension: " + p.Name);
                    }
                    
                    PluginsChanged?.Invoke(this, EventArgs.Empty);
                    
                    return p.Name;
                }
            }
            catch (Exception ex)
            {
                _context.Log("Plugin init failed: " + ex.Message);
            }
            return null;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        private static bool IsClientPluginFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var fileName = Path.GetFileName(path);
            return fileName != null && fileName.EndsWith(".Client.dll", StringComparison.OrdinalIgnoreCase);
        }
    }
}

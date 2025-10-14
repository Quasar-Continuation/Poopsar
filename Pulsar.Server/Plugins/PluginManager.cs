using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pulsar.Server.Plugins
{
    public class PluginManager
    {
        private readonly IServerContext _context;
        private readonly List<IServerPlugin> _plugins = new List<IServerPlugin>();
        private FileSystemWatcher _watcher;
        private readonly object _lock = new object();

        public event EventHandler PluginsChanged;

        public IReadOnlyList<IServerPlugin> Plugins => _plugins.AsReadOnly();

        public PluginManager(IServerContext context)
        {
            _context = context;
        }

        public void LoadFrom(string directory)
        {
            lock (_lock)
            {
                _plugins.Clear();
                _context.ClearPluginMenuItems();

                if (!Directory.Exists(directory))
                {
                    _context.Log($"Plugin directory not found: {directory}");
                    return;
                }

                foreach (var file in Directory.GetFiles(directory, "*.dll"))
                {
                    if (file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)) continue;

                    try
                    {
                        var assembly = Assembly.LoadFile(file);
                        foreach (var type in assembly.GetTypes())
                        {
                            if (typeof(IServerPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                            {
                                var plugin = (IServerPlugin)Activator.CreateInstance(type);
                                _plugins.Add(plugin);
                                _context.Log($"Loaded server plugin: {plugin.Name} (Version: {plugin.Version}) from {Path.GetFileName(file)}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _context.Log($"Error loading plugin from {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
                _plugins.Sort((p1, p2) => string.Compare(p1.Name, p2.Name, StringComparison.OrdinalIgnoreCase));
                OnPluginsChanged();
                SetupWatcher(directory);
            }
        }

        public void UnloadAll()
        {
            lock (_lock)
            {
                foreach (var plugin in _plugins)
                {
                    try { plugin.Cleanup(); } catch { }
                }
                _plugins.Clear();
                _context.ClearPluginMenuItems();
                _watcher?.Dispose();
                _watcher = null;
                OnPluginsChanged();
            }
        }

        private void SetupWatcher(string directory)
        {
            _watcher?.Dispose();
            _watcher = new FileSystemWatcher(directory)
            {
                Filter = "*.dll",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnPluginFileChanged;
            _watcher.Created += OnPluginFileChanged;
            _watcher.Deleted += OnPluginFileChanged;
            _watcher.Renamed += OnPluginFileChanged;
        }

        private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce to avoid multiple reloads for a single save operation
            Task.Delay(500).ContinueWith(_ =>
            {
                _context.Log($"Plugin file change detected: {e.ChangeType} {e.FullPath}. Reloading plugins...");
                LoadFrom(_watcher.Path);
            });
        }

        private void OnPluginsChanged()
        {
            PluginsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
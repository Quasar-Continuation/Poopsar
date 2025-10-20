using Pulsar.Common.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pulsar.Server.Plugins
{
    internal sealed class ClientPluginDescriptor
    {
        public ClientPluginDescriptor(string pluginId, string typeName, byte[] assemblyBytes, string version, byte[] initData)
        {
            PluginId = pluginId;
            TypeName = typeName;
            AssemblyBytes = assemblyBytes;
            Version = version;
            InitData = initData;
        }

        public string PluginId { get; }
        public string TypeName { get; }
        public byte[] AssemblyBytes { get; }
        public byte[] InitData { get; }
        public string Version { get; }
        public string CacheKey => $"{PluginId}:{Version}";
    }

    internal sealed class ClientPluginCatalog : IDisposable
    {
        private readonly IServerContext _context;
        private readonly object _sync = new object();
        private FileSystemWatcher _watcher;
        private string _folder = string.Empty;
        private ClientPluginDescriptor[] _plugins = Array.Empty<ClientPluginDescriptor>();

        public event EventHandler PluginsChanged;

        public ClientPluginCatalog(IServerContext context)
        {
            _context = context;
        }

        public IReadOnlyList<ClientPluginDescriptor> Plugins => _plugins;

        public void LoadFrom(string folder)
        {
            lock (_sync)
            {
                _folder = folder;
                EnsureDirectoryExists();
                ReloadInternal();
                StartWatcher();
            }

            PluginsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Reload()
        {
            lock (_sync)
            {
                if (string.IsNullOrEmpty(_folder))
                {
                    return;
                }

                ReloadInternal();
            }

            PluginsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
                _context?.Log("Created client plugin directory: " + _folder);
            }
        }

        private void ReloadInternal()
        {
            var descriptors = new List<ClientPluginDescriptor>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var files = Directory.EnumerateFiles(_folder, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(IsClientPluginFile)
                .OrderBy(Path.GetFileName);

            foreach (var file in files)
            {
                var descriptor = TryLoad(file);
                if (descriptor == null)
                {
                    continue;
                }

                if (!seenIds.Add(descriptor.PluginId))
                {
                    _context?.Log($"Duplicate client plugin id '{descriptor.PluginId}' detected in '{Path.GetFileName(file)}'; skipping duplicate.");
                    continue;
                }

                descriptors.Add(descriptor);
                _context?.Log($"Loaded client plugin '{descriptor.PluginId}' v{descriptor.Version} from {Path.GetFileName(file)}");
            }

            _plugins = descriptors.ToArray();
        }

        private ClientPluginDescriptor TryLoad(string path)
        {
            try
            {
                if (!IsClientPluginFile(path))
                {
                    return null;
                }

                var bytes = File.ReadAllBytes(path);
                var asm = Assembly.Load(bytes);
                var pluginType = asm.GetTypes()
                    .FirstOrDefault(t => typeof(IUniversalPlugin).IsAssignableFrom(t) && !t.IsAbstract);

                if (pluginType == null)
                {
                    _context?.Log($"Client plugin '{Path.GetFileName(path)}' does not expose an IUniversalPlugin implementation.");
                    return null;
                }

                if (Activator.CreateInstance(pluginType) is not IUniversalPlugin pluginInstance)
                {
                    _context?.Log($"Client plugin '{Path.GetFileName(path)}' could not be instantiated.");
                    return null;
                }

                var pluginId = string.IsNullOrWhiteSpace(pluginInstance.PluginId)
                    ? pluginType.FullName ?? Path.GetFileNameWithoutExtension(path)
                    : pluginInstance.PluginId;

                var version = string.IsNullOrWhiteSpace(pluginInstance.Version)
                    ? "1.0.0"
                    : pluginInstance.Version;

                var initPath = Path.ChangeExtension(path, ".init");
                byte[] initBytes = null;
                if (File.Exists(initPath))
                {
                    initBytes = File.ReadAllBytes(initPath);
                }

                return new ClientPluginDescriptor(
                    pluginId,
                    pluginType.FullName ?? pluginType.Name,
                    bytes,
                    version,
                    initBytes);
            }
            catch (ReflectionTypeLoadException rtle)
            {
                _context?.Log($"Client plugin load error ({Path.GetFileName(path)}): {rtle.Message}");
                foreach (var exception in rtle.LoaderExceptions)
                {
                    if (!string.IsNullOrWhiteSpace(exception?.Message))
                    {
                        _context?.Log("  " + exception.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _context?.Log($"Client plugin load error ({Path.GetFileName(path)}): {ex.Message}");
            }

            return null;
        }

        private void StartWatcher()
        {
            _watcher?.Dispose();

            _watcher = new FileSystemWatcher(_folder)
            {
                IncludeSubdirectories = false,
                Filter = "*.dll*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            _watcher.Created += OnFileChanged;
            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;
            _watcher.Error += OnWatcherError;
            _watcher.EnableRaisingEvents = true;

            _context?.Log("Client plugin watcher started for: " + _folder);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!IsClientPluginFile(e.FullPath))
            {
                return;
            }
            ScheduleReload($"Client plugin file {e.ChangeType}: {e.Name}");
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (!IsClientPluginFile(e.FullPath) && !IsClientPluginFile(e.OldFullPath))
            {
                return;
            }
            ScheduleReload($"Client plugin file renamed: {e.OldName} -> {e.Name}");
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            _context?.Log($"Client plugin watcher error: {e.GetException().Message}");
        }

        private void ScheduleReload(string reason)
        {
            _context?.Log(reason);
            Task.Delay(500).ContinueWith(_ =>
            {
                try
                {
                    Reload();
                }
                catch (Exception ex)
                {
                    _context?.Log($"Client plugin reload error: {ex.Message}");
                }
            });
        }

        private static bool IsClientPluginFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var fileName = Path.GetFileName(path);
            return fileName != null && fileName.EndsWith(".Client.dll", StringComparison.OrdinalIgnoreCase);
        }
    }
}

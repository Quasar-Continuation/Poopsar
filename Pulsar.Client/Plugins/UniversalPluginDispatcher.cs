using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pulsar.Client.Plugins
{
    public static class UniversalPluginDispatcher
    {
        private static readonly ConcurrentDictionary<string, object> LoadedPlugins = new ConcurrentDictionary<string, object>();
        private static readonly ConcurrentDictionary<string, DateTime> PluginLoadTimes = new ConcurrentDictionary<string, DateTime>();

        public static void RegisterPlugin(string pluginId, object plugin)
        {
            LoadedPlugins[pluginId] = plugin;
            PluginLoadTimes[pluginId] = DateTime.UtcNow;
        }

        public static object ExecuteCommand(string pluginId, string command, byte[] parameters)
        {
            if (!LoadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                return CreatePluginResult(false, "Plugin not found", false);
            }

            try
            {
                var executeMethod = plugin.GetType().GetMethod("ExecuteCommand");
                var result = executeMethod.Invoke(plugin, new object[] { command, parameters });
                var isCompleteProperty = plugin.GetType().GetProperty("IsComplete");
                bool isComplete = isCompleteProperty != null ? (bool)isCompleteProperty.GetValue(plugin) : false;
                var shouldUnloadProperty = result.GetType().GetProperty("ShouldUnload");
                bool shouldUnload = shouldUnloadProperty != null ? (bool)shouldUnloadProperty.GetValue(result) : false;

                if (shouldUnload || isComplete)
                {
                    UnloadPlugin(pluginId);
                }

                return result;
            }
            catch (Exception ex)
            {
                UnloadPlugin(pluginId);
                return CreatePluginResult(false, ex.Message, false);
            }
        }

        private static object CreatePluginResult(bool success, string message, bool shouldUnload)
        {
            var pluginResultType = typeof(PluginResult);
            var result = Activator.CreateInstance(pluginResultType);
            var successProperty = pluginResultType.GetProperty("Success");
            var messageProperty = pluginResultType.GetProperty("Message");
            var shouldUnloadProperty = pluginResultType.GetProperty("ShouldUnload");
            
            successProperty?.SetValue(result, success);
            messageProperty?.SetValue(result, message);
            shouldUnloadProperty?.SetValue(result, shouldUnload);
            
            return result;
        }

        public static void UnloadPlugin(string pluginId)
        {
            if (LoadedPlugins.TryRemove(pluginId, out var plugin))
            {
                try 
                { 
                    var cleanupMethod = plugin.GetType().GetMethod("Cleanup");
                    cleanupMethod?.Invoke(plugin, null);
                } 
                catch { }
                PluginLoadTimes.TryRemove(pluginId, out _);
            }
        }

        public static bool IsPluginLoaded(string pluginId)
        {
            return LoadedPlugins.ContainsKey(pluginId);
        }

        public static string[] GetLoadedPluginIds()
        {
            return LoadedPlugins.Keys.ToArray();
        }
    }
}

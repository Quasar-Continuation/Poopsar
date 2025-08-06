using MessagePack;
using MessagePack.Resolvers;
using Pulsar.Common.Messages.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pulsar.Common.Networking
{
    /// <summary>
    /// High-performance MessagePack serializer for Pulsar messages with polymorphic support.
    /// Optimized for MessagePack 3.1.4 with minimal overhead.
    /// </summary>
    public static class PulsarMessagePackSerializer
    {
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, string> _reverseTypeCache = new Dictionary<Type, string>();
        private static readonly object _cacheLock = new object();
        private static bool _initialized = false;

        /// <summary>
        /// MessagePack options configuration optimized for performance
        /// </summary>
        private static readonly MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard
            .WithResolver(CompositeResolver.Create(
                // Enable dynamic enum serialization
                DynamicEnumAsStringResolver.Instance,
                // Enable dynamic generic type serialization  
                DynamicGenericResolver.Instance,
                // Enable dynamic union (polymorphic) serialization
                DynamicUnionResolver.Instance,
                // Enable dynamic object serialization
                DynamicObjectResolver.Instance,
                // Standard resolver for primitive types
                StandardResolver.Instance
            ))
            .WithCompression(MessagePackCompression.Lz4BlockArray); // Add compression for better performance

        /// <summary>
        /// Serializes an IMessage object to MessagePack binary format with type information
        /// </summary>
        /// <param name="message">The message to serialize</param>
        /// <returns>Serialized binary data</returns>
        public static byte[] Serialize(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Create a wrapper that includes type information for polymorphic deserialization
                var messageType = message.GetType();
                var typeName = GetTypeName(messageType);
                
                // Serialize the message directly without using object type
                var messageData = MessagePackSerializer.Serialize(messageType, message, _options);
                
                var wrapper = new MessageWrapper
                {
                    TypeName = typeName,
                    Data = messageData
                };

                return MessagePackSerializer.Serialize(wrapper, _options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize message of type {message.GetType().Name}", ex);
            }
        }

        /// <summary>
        /// Deserializes MessagePack binary data back to an IMessage object
        /// </summary>
        /// <param name="data">The binary data to deserialize</param>
        /// <returns>Deserialized IMessage object</returns>
        public static IMessage Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data));

            try
            {
                // Deserialize the wrapper to get type information
                var wrapper = MessagePackSerializer.Deserialize<MessageWrapper>(data, _options);
                
                // Get the actual type from our cache
                var messageType = GetTypeFromName(wrapper.TypeName);
                if (messageType == null)
                {
                    throw new InvalidOperationException($"Unknown message type: {wrapper.TypeName}");
                }

                // Deserialize the actual message data
                return (IMessage)MessagePackSerializer.Deserialize(messageType, wrapper.Data, _options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize message", ex);
            }
        }

        /// <summary>
        /// Gets a short type name for efficient serialization
        /// </summary>
        private static string GetTypeName(Type type)
        {
            lock (_cacheLock)
            {
                if (_reverseTypeCache.TryGetValue(type, out var cachedName))
                    return cachedName;

                // For types not in cache, use full name
                var typeName = type.FullName;
                _reverseTypeCache[type] = typeName;
                return typeName;
            }
        }

        /// <summary>
        /// Gets a type from its name using the cache
        /// </summary>
        private static Type GetTypeFromName(string typeName)
        {
            lock (_cacheLock)
            {
                if (_typeCache.TryGetValue(typeName, out var cachedType))
                    return cachedType;

                // Try to load the type dynamically
                try
                {
                    var type = Type.GetType(typeName);
                    if (type != null && typeof(IMessage).IsAssignableFrom(type))
                    {
                        _typeCache[typeName] = type;
                        _reverseTypeCache[type] = typeName;
                        return type;
                    }
                    
                    // Try searching in loaded assemblies
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(typeName);
                        if (type != null && typeof(IMessage).IsAssignableFrom(type))
                        {
                            _typeCache[typeName] = type;
                            _reverseTypeCache[type] = typeName;
                            return type;
                        }
                    }
                }
                catch
                {
                    // Type not found
                }

                return null;
            }
        }

        /// <summary>
        /// Wrapper class for polymorphic message serialization
        /// </summary>
        [MessagePackObject(AllowPrivate = true)]
        internal class MessageWrapper
        {
            [Key(0)]
            public string TypeName { get; set; }

            [Key(1)]
            public byte[] Data { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar.Common.Messages
{
    /// <summary>
    /// Legacy TypeRegistry - no longer needed with MessagePack serialization.
    /// Kept for compatibility during migration.
    /// </summary>
    public static class TypeRegistry
    {
        /// <summary>
        /// Legacy method - no longer needed with MessagePack.
        /// </summary>
        /// <param name="parent">The parent type</param>
        /// <param name="type">Type to be added</param>
        public static void AddTypeToSerializer(Type parent, Type type)
        {
            // No-op: MessagePack handles polymorphism through custom resolvers
        }

        /// <summary>
        /// Legacy method - no longer needed with MessagePack.
        /// </summary>
        /// <param name="parent">The parent type</param>
        /// <param name="types">Types to add</param>
        public static void AddTypesToSerializer(Type parent, params Type[] types)
        {
            // No-op: MessagePack handles polymorphism through custom resolvers
        }

        /// <summary>
        /// Gets all types that implement the specified interface.
        /// Still used by legacy code during migration.
        /// </summary>
        public static IEnumerable<Type> GetPacketTypes(Type type)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
        }
    }
}

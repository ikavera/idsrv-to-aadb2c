using System;
using Microsoft.Extensions.Caching.Memory;

namespace Auth.Service.Cache.Impl
{
    public class CacheService : ICacheService
    {
        static readonly object _lockObject = new object();
        private readonly MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        public void AddItem(string key, object value)
        {
            AddItem(key, value, DateTimeOffset.MaxValue);
        }

        public void AddItem(string key, object value, DateTimeOffset timeToLive)
        {
            lock (_lockObject)
            {
                cache.Set(key, value ?? DBNull.Value, timeToLive);
            }
        }

        public void AddItem(string key, object value, TimeSpan timeToLive)
        {
            lock (_lockObject)
            {
                cache.Set(key, value, timeToLive);
            }
        }

        public object GetItem(string key)
        {
            lock (_lockObject)
            {
                cache.TryGetValue(key, out var result);
                return result;
            }
        }

        public void ClearItem(string key)
        {
            lock (_lockObject)
            {
                cache.Remove(key);
            }
        }

        public void Replace(string key, object value)
        {
            var item = GetItem(key);
            if (item != null)
            {
                lock (_lockObject)
                {
                    cache.Remove(key);
                    cache.Set(key, value, DateTimeOffset.MaxValue);
                }
            }
            else
            {
                lock (_lockObject)
                {
                    cache.Set(key, value, DateTimeOffset.MaxValue);
                }
            }
        }
    }
}

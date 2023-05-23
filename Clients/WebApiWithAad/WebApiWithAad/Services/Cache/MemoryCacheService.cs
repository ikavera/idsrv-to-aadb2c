using Microsoft.Extensions.Caching.Memory;
using System.Collections;

namespace WebApiWithAad.Services.Cache
{
    public class MemoryCacheService : ICacheService
    {
        static readonly object _lockObject = new object();
        private readonly MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        public void Add(string key, object value)
        {
            Add(key, value, new TimeSpan(DateTimeOffset.MaxValue.ToUnixTimeSeconds()));
        }

        public void Add(string key, object value, TimeSpan timeToLive)
        {
            lock (_lockObject)
            {
                cache.Set(key, value, timeToLive);
            }
        }

        public List<T> GetAllItems<T>()
        {
            lock (_lockObject)
            {
                List<T> list = new List<T>();
                IDictionaryEnumerator cacheEnumerator = (IDictionaryEnumerator)(cache as IEnumerable).GetEnumerator();

                while (cacheEnumerator.MoveNext())
                {
                    if (cacheEnumerator.Value is T)
                    {
                        list.Add((T)cacheEnumerator.Value);
                    }
                }

                return list;
            }
        }

        public object Get(string key)
        {
            lock (_lockObject)
            {
                cache.TryGetValue(key, out var result);
                return result;
            }
        }

        public void Remove(string key)
        {
            lock (_lockObject)
            {
                cache.Remove(key);
            }
        }

        public void Replace(string key, object value)
        {
            var item = Get(key);
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

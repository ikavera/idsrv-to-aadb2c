using System;

namespace Auth.Service.Cache
{
    public interface ICacheService
    {
        void AddItem(string key, object value);
        void AddItem(string key, object value, DateTimeOffset timeToLive);
        void AddItem(string key, object value, TimeSpan timeToLive);
        object GetItem(string key);
        void ClearItem(string key);
        void Replace(string key, object value);
    }
}

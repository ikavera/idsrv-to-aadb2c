namespace WebApiWithAad.Services.Cache
{
    public interface ICacheService
    {
        void Add(string key, object value);
        void Add(string key, object value, TimeSpan timeToLive);
        object Get(string key);
        void Remove(string key);
        void Replace(string key, object value);
        List<T> GetAllItems<T>();
    }
}

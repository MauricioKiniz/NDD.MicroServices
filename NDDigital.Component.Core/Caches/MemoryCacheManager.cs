using System;
using System.Runtime.Caching;

namespace NDDigital.Component.Core.Caches
{
    public class MemoryCacheManager : IDataCache, IDisposable
    {
        private MemoryCache _memoryCache;
        private object locked = new object();

        public bool IsMemory
        {
            get { return true; }
        }

        public MemoryCacheManager(string name)
        {
            _memoryCache = new MemoryCache(name);
        }

        public T Get<T>(string key, string region = null)
        {
            lock (locked)
            {
                object getted = _memoryCache.Get(key);
                return (getted == null) ? default(T) : (T)getted;
            }
        }

        public void Put(string key, object data, string region = null)
        {
            lock (locked)
            {
                try
                {
                    _memoryCache.Set(key, data, new CacheItemPolicy()
                    {
                        AbsoluteExpiration = new DateTimeOffset(new DateTime(2050, 12, 31))
                    });
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }

        public void Delete(string key)
        {
            _memoryCache.Remove(key);
        }
    }
}

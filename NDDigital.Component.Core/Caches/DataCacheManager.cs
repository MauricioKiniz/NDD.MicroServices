using NDDigital.Component.Core.Manager;
using NDDigital.Component.Core.Util.Dynamics;
using RedisBoost;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NDDigital.Component.Core.Caches
{
    public static class DataCacheManager
    {
        private static readonly IDictionary<string, IDataCache> Caches;
        private const int DefaultPort = 22233;
        private const int MaxConnections = 3;

        static DataCacheManager()
        {
            Caches = new Dictionary<string, IDataCache>();
            RedisClient.DefaultSerializer = new RedisBinarySerializer();
        }

        public static void Start()
        {
            LoadCaches();
        }

        public static void Stop()
        {
            foreach (var cache in Caches.Values)
                cache.Dispose();
            Caches.Clear();
        }

        public static void Delete()
        {
            Caches.Clear();
        }

        private static void LoadCaches()
        {
            var caches = MiddlewareConfManager.GetCaches();

            if (caches.Length == 0)
                return;

            foreach (dynamic cache in caches)
            {
                IDataCache manager;
                string name;

                if (cache.MemoryCache != null)
                {
                    name = cache.MemoryCache.Name;
                    manager = new MemoryCacheManager(name);
                }
                else
                {
                    name = cache.ServerCache.Name;
                    DynamicXmlObject[] servers = cache.ServerCache.GetElements("Server");
                    var config = new ServerDataCacheConfig()
                    {
                        Database = cache.ServerCache.Database,
                        Port = cache.ServerCache.Port ?? DefaultPort,
                        MaxConnectionsToServer = cache.ServerCache.MaxConnectionsToServer ?? MaxConnections,
                        Servers = servers.Select(p => p.Value).ToArray(),
                        SerializationKind = cache.ServerCache.SerializationKind
                    };
                    manager = new ServerRedisCacheManager(config);
                }

                if (Caches.ContainsKey(name))
                    throw new ArgumentException($"Cache with the same name exists in the CacheManager. Inactivate one off them. Cache Name {name}");

                Caches.Add(name, manager);
            }
        }

        public static IDataCache GetCache(string name)
        {
            return Caches[name];
        }
    }
}

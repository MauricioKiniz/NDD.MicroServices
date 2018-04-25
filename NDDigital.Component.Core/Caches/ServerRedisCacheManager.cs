using RedisBoost;
using RedisBoost.Core.Serialization;
using System;

namespace NDDigital.Component.Core.Caches
{
    public class ServerRedisCacheManager : IDataCache
    {
        private static readonly int InactiveTimeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;
        private static readonly string CNConnectionString = "data source={0}:{1};initial catalog=\"{2}\"";

        private IRedisClientsPool _pool;
        private ServerDataCacheConfig _config;
        private string _connString;
        private BasicRedisSerializer _cacheSerializer;

        public ServerRedisCacheManager(ServerDataCacheConfig config)
        {
            _config = config;
            _connString = string.Format(CNConnectionString, config.Servers[0], config.Port, config.Database);
            _pool = RedisClient.CreateClientsPool(config.MaxConnectionsToServer, InactiveTimeout);

            switch (config.SerializationKind)
            {
                case CacheSerializerEnum.Binary:
                    _cacheSerializer = new RedisBinarySerializer();
                    break;
                case CacheSerializerEnum.Xml:
                    _cacheSerializer = new RedisXmlSerializer();
                    break;
                case CacheSerializerEnum.Json:
                    _cacheSerializer = new RedisJsonSerializer();
                    break;
                default:
                    _cacheSerializer = new RedisBinarySerializer();
                    break;
            }
        }

        public bool IsMemory
        {
            get { return false; }
        }

        public void Dispose()
        {
            _pool.Dispose();
        }

        public T Get<T>(string key, string region = null)
        {
            var client = _pool.CreateClientAsync(_connString, _cacheSerializer).Result;

            var task = client.GetAsync(key);
            task.Wait();

            if (task.Result.IsNull)
                return default(T);

            return task.Result.As<T>();
        }

        public void Put(string key, object data, string region = null)
        {
            var client = _pool.CreateClientAsync(_connString, _cacheSerializer).Result;

            client.SetAsync(key, data);
        }

        public void Delete(string key)
        {
            var client = _pool.CreateClientAsync(_connString, _cacheSerializer).Result;

            client.DelAsync(key);
        }
    }
}

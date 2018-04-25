namespace NDDigital.Component.Core.Caches
{
    public class ServerDataCacheConfig
    {
        public string Name { get; set; }

        public string Database { get; set; }

        public string[] Servers { get; set; }

        public int Port { get; set; }

        public int MaxConnectionsToServer { get; set; }

        public CacheSerializerEnum SerializationKind { get; set; }
    }

    public enum CacheSerializerEnum
    {
        Binary = 0,
        Xml = 1,
        Json = 2
    }
}

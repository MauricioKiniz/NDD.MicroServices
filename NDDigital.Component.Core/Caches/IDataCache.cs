using System;

namespace NDDigital.Component.Core.Caches
{
    public interface IDataCache : IDisposable
    {
        void Put(string key, object data, string region = null);

        T Get<T>(string key, string region = null);

        void Delete(string key);

        bool IsMemory { get; }
    }
}

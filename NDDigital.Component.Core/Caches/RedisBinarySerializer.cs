using RedisBoost.Core.Serialization;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Xml.Serialization;

namespace NDDigital.Component.Core.Caches
{
    public class RedisBinarySerializer : BasicRedisSerializer
    {
        public override byte[] Serialize(object value)
        {
            IFormatter formatter = new BinaryFormatter();
            using (var mem = new MemoryStream())
            {
                formatter.Serialize(mem, value);
                mem.Flush();
                return mem.ToArray();
            }
        }

        public override object Deserialize(Type type, byte[] value)
        {
            IFormatter formatter = new BinaryFormatter();
            using (var mem = new MemoryStream(value))
            {
                return formatter.Deserialize(mem);
            }
        }
    }

    public class RedisXmlSerializer : BasicRedisSerializer
    {
        public override byte[] Serialize(object value)
        {
            XmlSerializer formatter = new XmlSerializer(value.GetType());
            using (var mem = new MemoryStream())
            {
                formatter.Serialize(mem, value);
                mem.Flush();
                return mem.ToArray();
            }
        }

        public override object Deserialize(Type type, byte[] value)
        {
            XmlSerializer formatter = new XmlSerializer(type);
            using (var mem = new MemoryStream(value))
            {
                return formatter.Deserialize(mem);
            }
        }
    }

    public class RedisJsonSerializer : BasicRedisSerializer
    {
        public override byte[] Serialize(object value)
        {
            DataContractJsonSerializer formatter = new DataContractJsonSerializer(value.GetType());
            using (var mem = new MemoryStream())
            {
                formatter.WriteObject(mem, value);
                mem.Flush();
                return mem.ToArray();
            }
        }

        public override object Deserialize(Type type, byte[] value)
        {
            DataContractJsonSerializer formatter = new DataContractJsonSerializer(type);
            using (var mem = new MemoryStream(value))
            {
                return formatter.ReadObject(mem);
            }
        }
    }
}

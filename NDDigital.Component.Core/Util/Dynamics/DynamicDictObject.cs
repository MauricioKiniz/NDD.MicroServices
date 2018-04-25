using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace NDDigital.Component.Core.Util.Dynamics
{
    public class DynamicDictObject : DynamicObject
    {
        private SortedList<string, object> _config = new SortedList<string, object>();
        private object locked = new object();

        private DynamicDictObject()
        {
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            lock (locked)
            {
                _config.TryGetValue(binder.Name, out result);
                return result != null;
            }
        }

        public static DynamicDictObject Create(IEnumerable<KeyValuePair<string, object>> list)
        {
            var ddo = new DynamicDictObject();
            foreach (var kvp in list)
                ddo._config.Add(kvp.Key, kvp.Value);
            return ddo;
        }

        public static DynamicDictObject Create()
        {
            return new DynamicDictObject();
        }

        public void Add(string key, object value)
        {
            lock (locked)
                _config.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            lock (locked)
                return _config.Any(x => x.Key == key);
        }

        public object GetValue(string key)
        {
            lock (locked)
                return _config.FirstOrDefault(x => x.Key == key).Value;
        }
    }
}

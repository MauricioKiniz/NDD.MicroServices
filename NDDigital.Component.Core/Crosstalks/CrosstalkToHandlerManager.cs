using NDDigital.Component.Core.Util.Dynamics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NDDigital.Component.Core.Crosstalks
{
    public static class CrosstalkToHandlerManager
    {
        private static List<ICrosstalkToHandlerCreator> Creators;

        static CrosstalkToHandlerManager()
        {
            Creators = new List<ICrosstalkToHandlerCreator>();
        }

        public static void Start(string directoryToSearch)
        {
            List<Type> list = new List<Type>();
            var files = Directory.GetFiles(directoryToSearch, "NDDigital*.*");
            var runType = typeof(ICrosstalkToHandlerCreator);

            foreach (var file in files)
            {
                if (file.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) ||
                    file.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    Assembly ass = Assembly.LoadFrom(file);
                    var types = ass.GetTypes().Where(p => runType.IsAssignableFrom(p) && p.IsInterface == false);
                    if (types.Count() > 0)
                        list.AddRange(types);
                }
            }

            foreach (Type tp in list)
                Creators.Add((ICrosstalkToHandlerCreator)Activator.CreateInstance(tp));
        }

        public static object GetMessage(string crosstalk)
        {
            dynamic dynCrosstalk = DynamicXmlObject.Parse(crosstalk);
            int processCode = dynCrosstalk.CrosstalkHeader.ProcessCode;
            int messageType = dynCrosstalk.CrosstalkHeader.MessageType;

            foreach (var creator in Creators)
            {
                object created = creator.GetMessageFrom(processCode, messageType, crosstalk);

                if (created != null)
                    return created;
            }

            return null;
        }
    }
}

using NDDigital.Component.Core.Util;
using NDDigital.Component.Core.Util.Dynamics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NDDigital.Component.Core.Manager
{
    public static class MiddlewareConfManager
    {
        private static DynamicXmlObject XmlConf;
        private static object locked = new object();
        private static MiddlewareConfWatcher Watcher;
        private static readonly string EventSource = "NddMiddleware";
        private static bool EventSecurityAccess = true;

        static MiddlewareConfManager()
        {
            try
            {
                if (EventLog.SourceExists(EventSource, Environment.MachineName) == false)
                    EventLog.CreateEventSource(EventSource, EventSource);
            }
            catch (Exception)
            {
                EventSecurityAccess = false;
            }
            LoadConfiguration();
        }

        private static void LoadConfiguration()
        {
            lock (locked)
            {
                string xml = LoadFileFromDirectory();
                XmlConf = DynamicXmlObject.Parse(xml);
                Watcher = new MiddlewareConfWatcher();
                Watcher.RegisterWatcher((string)ConfigContext.Data.MiddlewareFileMapper);
            }
        }

        private static string LoadFileFromDirectory()
        {
            string fileMapper = ConfigContext.Data.MiddlewareFileMapper;

            if (!File.Exists(fileMapper))
                throw new ArgumentException($"File Mapper not found. File Mapper: {fileMapper}");

            var xml = File.ReadAllText(fileMapper);
            var schema = UtilHelper.GetMiddlewareSchema();

            var validation = new SchemaValidation();
            validation.Validate(xml, schema);
            var errors = validation.Errors;
            if (errors.Length > 0)
            {
                StringBuilder sb = new StringBuilder("Middleware validation error.").AppendLine();
                foreach (var error in errors)
                    sb.AppendLine(error);
                throw new ArgumentException(sb.ToString());
            }
            return xml;
        }

        public static void GetDeltaChange(string xmlData, List<DeltaChange> changes)
        {
            lock (locked)
            {
                try
                {
                    string newXml = LoadFileFromDirectory();
                    CalculateDelta(newXml, changes);
                    if (changes.Count > 0)
                    {
                        XmlConf.Dispose();
                        XmlConf = DynamicXmlObject.Parse(newXml);
                    }
                }
                catch (Exception e)
                {
                    if (EventSecurityAccess)
                    {
                        string fileMapper = ConfigContext.Data.MiddlewareFileMapper;
                        UtilHelper.WriteLogEntry(new EventLog(EventSource, Environment.MachineName, EventSource),
                            "Error on loading file: {0} - error: {1}", fileMapper, e.Message);
                    }
                }
            }
        }

        private static void CalculateDelta(string newXml, List<DeltaChange> changes)
        {
            using (DynamicXmlObject newConf = DynamicXmlObject.Parse(newXml))
            {
                dynamic middleware = (dynamic)newConf;
                bool autoRefresh = middleware.AutoRefresh;
                if (autoRefresh == false)
                    return;
                var oldDocEndpoints = GetEndpoints();
                var newDocEndpoints = newConf.GetElements("Endpoint");
                GetEndpointChanges(oldDocEndpoints, newDocEndpoints, changes);
            }
        }

        private static void GetEndpointChanges(DynamicXmlObject[] oldDocEndpoints, DynamicXmlObject[] newDocEndpoints, List<DeltaChange> changes)
        {
            var oldDocEndpointsDynamic = oldDocEndpoints.Cast<dynamic>();
            var newDocEndpointsDynamic = newDocEndpoints.Cast<dynamic>();
            foreach (dynamic endpoint in newDocEndpointsDynamic)
            {
                string id = endpoint.Id;
                if (oldDocEndpointsDynamic.FirstOrDefault(p => (string)p.Id == id) == null)
                    changes.Add(new EndpointDeltaChange() { Kind = DeltaChangeEnum.Activated, EndpointId = id });
            }
            foreach (dynamic endpoint in oldDocEndpointsDynamic)
            {
                string id = endpoint.Id;
                if (newDocEndpointsDynamic.FirstOrDefault(p => (string)p.Id == id) == null)
                    changes.Add(new EndpointDeltaChange() { Kind = DeltaChangeEnum.Deactivated, EndpointId = id });
            }
            foreach (dynamic endpoint in oldDocEndpointsDynamic)
            {
                string id = endpoint.Id;
                var endpointChanged = newDocEndpointsDynamic.FirstOrDefault(p => (string)p.Id == id && p.IsEqual(endpoint) == false);
                if (endpointChanged != null)
                {
                    bool endChanged = endpointChanged.Active;
                    bool endpointAct = endpoint.Active;
                    if (endChanged != endpointAct)
                    {
                        bool active = endpointChanged.Active;
                        var kind = (active) ? DeltaChangeEnum.Activated : DeltaChangeEnum.Deactivated;
                        changes.Add(new EndpointDeltaChange() { Kind = kind, EndpointId = id });
                    }
                    else
                        changes.Add(new EndpointDeltaChange() { Kind = DeltaChangeEnum.Changed, EndpointId = id });
                }
            }
        }

        private static bool FilterById(dynamic element, object id)
        {
            string elementId = element.Id;
            return elementId.Equals(id);
        }

        private static bool FilterEndpointByName(dynamic element, object name)
        {
            string elementId = element.Name;
            return elementId.Equals(name);
        }

        private static bool FilterEndpointByGroupId(dynamic element, object ids)
        {
            var allIds = (string[])ids;
            string elementId = element.Id;
            return allIds.FirstOrDefault(p => elementId.Equals(p)) != null;
        }

        public static DynamicXmlObject GetEndpoint(string id)
        {
            lock (locked)
                return XmlConf.GetFirstElement("Endpoint", FilterById, id);
        }

        public static DynamicXmlObject GetEndpointByName(string name)
        {
            lock (locked)
                return XmlConf.GetFirstElement("Endpoint", FilterEndpointByName, name);
        }

        public static DynamicXmlObject[] GetEndpoints(string[] allIds)
        {
            lock (locked)
                return XmlConf.GetElements("Endpoint", FilterEndpointByGroupId, allIds);
        }

        public static DynamicXmlObject[] GetEndpoints()
        {
            lock (locked)
                return XmlConf.GetElements("Endpoint");
        }

        public static DynamicXmlObject[] GetMappers()
        {
            lock (locked)
                return XmlConf.GetElements("Mapper");
        }

        public static DynamicXmlObject[] GetQueueReferences()
        {
            lock (locked)
                return XmlConf.GetElements("QueueReference");
        }

        public static DynamicXmlObject GetQueueReferenceById(string id)
        {
            lock (locked)
                return XmlConf.GetFirstElement("QueueReference", FilterById, id);
        }


        public static DynamicXmlObject[] GetCaches(bool onlyActives = true)
        {
            lock (locked)
            {
                return XmlConf.GetElements("Cache", delegate (dynamic element, object tag)
                {
                    if (onlyActives == false) return true;
                    bool active = element.Active ?? true;
                    return active;
                });
            }
        }

        public static DynamicXmlObject GetScanAssemblies()
        {
            lock (locked)
            {
                return XmlConf.GetFirstElement("ScanAssemblies");
            }
        }
    }
}

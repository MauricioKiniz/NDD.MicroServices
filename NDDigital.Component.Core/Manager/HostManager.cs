using Microsoft.CSharp.RuntimeBinder;
using NDDigital.Component.Core.Caches;
using NDDigital.Component.Core.Util;
using NDDigital.Component.Core.Util.Dynamics;
using NServiceBus;
using NServiceBus.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Manager
{
    public static class HostManager
    {
        private static IDictionary<string, ServiceBusManager> BusManagers = new ConcurrentDictionary<string, ServiceBusManager>();
        private static Type[] RunOnStartAndStopList;

        public static void Start(bool waitFor = false, bool inTask = true)
        {
            DisposeAllBusManagers();
            RegisterLog4Net();
            DataCacheManager.Start();
            CreateBusEntities(waitFor, inTask);
            ExecuteStartAndStopEntities();
        }

        private static void ExecuteStartAndStopEntities()
        {
            RunOnStartAndStopList = GetAllRunOnStartAndStopClasses();
            foreach (var typeToRun in RunOnStartAndStopList)
            {
                var rss = (IRunOnStartAndStop)Activator.CreateInstance(typeToRun);
                rss.Start();
            }
        }

        private static void CreateBusEntities(bool waitFor, bool inTask = true)
        {
            DynamicXmlObject[] configEndpoints;
            string endpoints;

            try
            {
                endpoints = ConfigContext.Data.Endpoints;
            }
            catch (RuntimeBinderException)
            {
                endpoints = null;
            }

            if (string.IsNullOrEmpty(endpoints))
                configEndpoints = MiddlewareConfManager.GetEndpoints();
            else if (endpoints.Equals(string.Empty))
                throw new ArgumentException($"No endpoints to process was found in the registry.");
            else
            {
                var singleEndpoints = endpoints.Split('|');
                configEndpoints = MiddlewareConfManager.GetEndpoints(singleEndpoints);
            }

            if (inTask)
            {
                var tasks = new List<Task>();

                foreach (dynamic endpoint in configEndpoints)
                {
                    bool active = endpoint.Active;

                    if (!active)
                        continue;

                    var t = new Task(() =>
                    {
                        CreateBusManager(endpoint);
                    });

                    tasks.Add(t);
                    t.Start();
                }

                if (waitFor)
                    Task.WaitAll(tasks.ToArray());
            }
            else
            {
                foreach (dynamic endpoint in configEndpoints)
                {
                    bool active = endpoint.Active;

                    if (!active)
                        continue;

                    var manager = new ServiceBusManager();
                    manager.Start(endpoint);
                    string id = endpoint.Id;
                    BusManagers.Add(id, manager);
                }
            }
        }

        private static void CreateBusManager(dynamic endpoint)
        {
            var manager = new ServiceBusManager();
            manager.Start(endpoint);
            string id = endpoint.Id;
            BusManagers.Add(id, manager);
        }

        public static void ProcessChanges(List<DeltaChange> changes)
        {
            var deactivatedChanges = changes.Where(p => p.Kind == DeltaChangeEnum.Deactivated || p.Kind == DeltaChangeEnum.Changed);
            var activatedChanges = changes.Where(p => p.Kind == DeltaChangeEnum.Activated || p.Kind == DeltaChangeEnum.Changed);

            List<Task> tasks = new List<Task>();

            foreach (var change in deactivatedChanges)
            {
                EndpointDeltaChange edc = (EndpointDeltaChange)change;
                var serviceBusManager = BusManagers[edc.EndpointId];
                serviceBusManager.Stop();
                BusManagers.Remove(edc.EndpointId);
            }

            foreach (var change in activatedChanges)
            {
                EndpointDeltaChange edc = (EndpointDeltaChange)change;
                var t = new Task(() =>
                {
                    dynamic endpoint = MiddlewareConfManager.GetEndpoint(edc.EndpointId);
                    CreateBusManager(endpoint);
                });
                tasks.Add(t);
                t.Start();
            }
            Task.WaitAll(tasks.ToArray());
        }

        private static Type[] GetAllRunOnStartAndStopClasses()
        {
            var list = new List<Type>();
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var files = Directory.GetFiles(path, "NDDigital*.*");
            var runType = typeof(IRunOnStartAndStop);

            foreach (var file in files)
            {
                if (!file.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) &&
                    !file.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)) continue;
                var ass = Assembly.LoadFrom(file);

                var types = ass.GetTypes().Where(p => runType.IsAssignableFrom(p) && p.IsInterface == false);

                if (types.Any())
                    list.AddRange(types);
            }

            return list.ToArray();
        }

        private static void DisposeAllBusManagers()
        {
            foreach (var manager in BusManagers.Values)
                manager.Stop();

            BusManagers.Clear();
        }

        public static void Stop()
        {
            DisposeAllBusManagers();
            DataCacheManager.Stop();

            foreach (var typeToRun in RunOnStartAndStopList)
            {
                var rss = (IRunOnStartAndStop)Activator.CreateInstance(typeToRun);
                rss.Stop();
            }
        }

        public static IEndpointInstance GetEndpointInstance(string id)
        {
            ServiceBusManager sbm;
            return BusManagers.TryGetValue(id, out sbm) ? sbm.EndpointInstance : null;
        }

        #region Log4Net

        private static LogWatcher _watcher;

        private static void RegisterLog4Net()
        {
            _watcher = new LogWatcher();
            string logFileName = ConfigContext.Data.LogFileName;
            var hasLogFile = _watcher.RegisterLog(logFileName);
            var logger = LogManager.GetLogger(typeof(HostManager));

            if (hasLogFile == false)
                logger.Debug("Log file is missing or not found in execution directory. Default values will be used");
        }

        #endregion
    }
}

using NDDigital.Component.Core.Manager.EnpointCreation;
using NDDigital.Component.Core.Mutators;
using NDDigital.Component.Core.Util;
using NDDigital.Component.Core.Util.Dynamics;
using NHibernate.Cfg;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Newtonsoft.Json;
using NServiceBus.Persistence;
using NServiceBus.Persistence.NHibernate;
using NServiceBus.Transport.SQLServer;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;

namespace NDDigital.Component.Core.Manager
{
    public class ServiceBusManager
    {
        public static ILog Logger = LogManager.GetLogger(typeof(ServiceBusManager));

        private IEndpointCreation[] _creations;
        public IScheduler Scheduler { get; set; }
        public IEndpointInstance EndpointInstance { get; set; }

        public string EndpointId { get; set; } = "{Empty}";
        public string EndpointName { get; set; } = "Not informed";

        public ServiceBusManager()
        {
            _creations = new IEndpointCreation[] {
                new EndpointConfigurationCreation(),
                new EndpointPersistenceCreation(),
                new EndpointTransportCreation(),
                new EndpointRecoveriabilityAndConcurrencyCreation(),
                new EndpointEnableDisableFeaturesCreation(),
                new EndpointScanAssembliesCreation(),
                new EndpointStartEndpointCreation(this),
                new EndpointSubscriptionCreation(this),
                new EndpointSchedulerCreation(this)
            };
        }

        public void Start(dynamic endpoint)
        {
            EndpointConfiguration cfg = null;

            try
            {
                EndpointId = endpoint.Id;
                EndpointName = endpoint.Name;
                foreach (var creationTask in _creations)
                    cfg = creationTask.Create(cfg, endpoint);
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Enpoint: {0} - {1} Error on Creation. Error: {2} - {3}", 
                    EndpointId, EndpointName, e.Message, e.StackTrace);
                FreeInstances();
                throw e;
            }
        }


        public void Stop()
        {
            UtilHelper.WriteDebug(Logger, "Stoping endpoint: {0} - {1}", EndpointId, EndpointName);
            FreeInstances();
            UtilHelper.WriteDebug(Logger, "Endpoint: {0} - {1} stoped", EndpointId, EndpointName);
        }

        private void FreeInstances()
        {
            if (Scheduler != null)
            {
                Scheduler.Shutdown();
                Scheduler = null;
            }
            if (EndpointInstance != null)
            {
                EndpointInstance.Stop().Wait();
                EndpointInstance = null;
            }
        }
    }
}

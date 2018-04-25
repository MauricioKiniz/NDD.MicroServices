using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NHibernate.Cfg;
using NDDigital.Component.Core.Util;
using NServiceBus.Persistence;
using NServiceBus.Persistence.NHibernate;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointPersistenceCreation : EndpointCreationBase, IEndpointCreation
    {

        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            bool isMemoryPersistence = endpoint.IsMemoryPersistence;
            string endpointId = endpoint.Id;
            UtilHelper.WriteDebug(_logger, "Endpoint: {0} Persistence: {1}", endpointId, isMemoryPersistence);
            if (isMemoryPersistence)
            {
                cfg.UsePersistence<InMemoryPersistence>();
            }
            else
            {
                var cfgData = ConfigContext.Data;
                Configuration nhConfiguration = new Configuration();

                string dialect = cfgData.NHibernateDialect;
                string connectionString = cfgData.NServiceBusPersistence;
                if (string.IsNullOrEmpty(dialect) || string.IsNullOrEmpty(connectionString))
                    throw new ArgumentException("MHibernate Dialect or Connection String is empty ou null");
                nhConfiguration.Properties["dialect"] = cfgData.NHibernateDialect;
                nhConfiguration.Properties["connection.connection_string"] = cfgData.NServiceBusPersistence;
                nhConfiguration.Properties["default_schema"] = DefaultSchema;
                var persistence = cfg.UsePersistence<NHibernatePersistence>();
                persistence.UseConfiguration(nhConfiguration);
            }
            return cfg;
        }

    }
}

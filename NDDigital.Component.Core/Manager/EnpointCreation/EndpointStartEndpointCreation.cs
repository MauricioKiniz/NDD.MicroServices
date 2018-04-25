using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NDDigital.Component.Core.Util;
using NServiceBus.Features;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointStartEndpointCreation : EndpointCreationBase, IEndpointCreation
    {
        private ServiceBusManager _manager;

        public EndpointStartEndpointCreation(ServiceBusManager manager)
        {
            _manager = manager;
        }

        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            UtilHelper.WriteDebug(_logger, "Create EndpointInstance Endpoint {0}", (string)endpoint.Id);
            var task = NServiceBus.Endpoint.Start(cfg);
            task.Wait();
            _manager.EndpointInstance = task.Result;
            UtilHelper.WriteDebug(_logger, "Endpoint {0} - Created", (string)endpoint.Id);
            return cfg;
        }
    }
}

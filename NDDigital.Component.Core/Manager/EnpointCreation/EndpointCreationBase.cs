using NServiceBus.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public abstract class EndpointCreationBase
    {
        protected ILog _logger = LogManager.GetLogger(typeof(ServiceBusManager));
        protected static readonly string DefaultSchema = "middleware";

        protected bool IsSendOnly(dynamic endpoint)
        {
            return endpoint.Process == null;
        }
    }
}

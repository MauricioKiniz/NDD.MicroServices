using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NDDigital.Component.Core.Util;
using log4net;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointConfigurationCreation : EndpointCreationBase, IEndpointCreation
    {

        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            if (endpoint == null)
                throw new ArgumentException("endpoint parameter can not be null");
            string endpointName = endpoint.Name;
            string endpointId = endpoint.Id;
            UtilHelper.WriteDebug(_logger, "Initialize Endpoint - Id: '{0}' Name: '{1}'", endpointId, endpointName);
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint Name can not be empty ou null");
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint Id can not be empty ou null");
            cfg = new EndpointConfiguration(endpointName);
            if (endpointName.ToLower() != "particular.servicecontrol")
                cfg.MakeInstanceUniquelyAddressable(endpointId);
            cfg.License(UtilHelper.GetLicense());
            cfg.UseSerialization<NewtonsoftSerializer>().Settings(JsonConfig.GetJsonSerializerSettings());
            if (IsSendOnly(endpoint))
                cfg.SendOnly();
            return cfg;
        }
    }
}

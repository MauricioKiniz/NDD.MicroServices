using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    interface IEndpointCreation
    {
        EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint);
    }
}

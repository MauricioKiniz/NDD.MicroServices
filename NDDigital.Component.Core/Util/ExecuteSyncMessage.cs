using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using System.Threading;

namespace MKSistemas.Component.Core.Util
{

    public class ExecuteSyncMessage
    {
        public async Task<T> Execute<T>(object message, IEndpointInstance endpoint)
        {
            return await endpoint.Request<T>(message).ConfigureAwait(false);
        }

        public async Task<T> Execute<T>(object message, IEndpointInstance endpoint, TimeSpan timeToWait)
        {

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(timeToWait);
            return await endpoint.Request<T>(message, cancellationTokenSource.Token).ConfigureAwait(false);
        }

    }
}

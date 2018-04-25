using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Behaviors
{
    public class PublishBehavior : Behavior<IOutgoingPublishContext>
    {
        public override Task Invoke(IOutgoingPublishContext context, Func<Task> next)
        {
            context.Headers.Add("PublishedMessage", true.ToString());
            next();
            return Task.CompletedTask;
        }
    }

    public class PublishOutLogicContext : Behavior<IOutgoingLogicalMessageContext>
    {
        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            string isPublishMessage;
            if (context.Headers.TryGetValue("PublishedMessage", out isPublishMessage))
            {
                context.Headers.Remove("PublishedMessage");
                foreach(var item in context.RoutingStrategies)
                {
                    UnicastRoutingStrategy strategy = item as UnicastRoutingStrategy;
                    if (strategy == null)
                        continue;
                    UnicastAddressTag address = strategy.Apply(context.Headers) as UnicastAddressTag;
                    SendOptions options = new SendOptions();
                    options.SetDestination(address.Destination);
                    context.Send(context.Message.Instance, options); // here comes a exception
                }
            } else
                next();
            return Task.CompletedTask;
        }
    }
}

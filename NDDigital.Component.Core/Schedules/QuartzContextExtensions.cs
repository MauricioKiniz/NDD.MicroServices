using NServiceBus;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Schedules
{
    public static class QuartzContextExtensions
    {
        public static IEndpointInstance EndpointInstance(this IJobExecutionContext context)
        {
            return (IEndpointInstance)context.Scheduler.Context["EndpointInstance"];
        }

        public static void SetEndpointInstance(this IScheduler scheduler, IEndpointInstance instance)
        {
            scheduler.Context["EndpointInstance"] = instance;
        }

    }
}

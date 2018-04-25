using NDDigital.Component.Core.Util;
using NServiceBus;
using NServiceBus.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Schedules
{
    public class SendMessageJob : IJob
    {

        static ILog log = LogManager.GetLogger("SendMessageJob");

        public void Execute(IJobExecutionContext context)
        {
 
        }

        Task IJob.Execute(IJobExecutionContext context)
        {
            try
            {
                IEndpointInstance endpointInstance = context.EndpointInstance();
                string assemblyName = context.JobDetail.JobDataMap.GetString(GlobalScheduleConstants.AssemblyName);
                string typeName = context.JobDetail.JobDataMap.GetString(GlobalScheduleConstants.TypeName);
                Assembly contextAssembly = Assembly.Load(assemblyName);
                IScheduleDataDefinition messageSender = (IScheduleDataDefinition)contextAssembly.CreateInstance(typeName);
                if (messageSender == null)
                    throw new ArgumentException($"TypeName: '{typeName}' not found in the Assembly: '{assemblyName}'");
                messageSender.SendMessage(endpointInstance, context);
            }
            catch (Exception e)
            {
                UtilHelper.WriteError(log,
                    "Error processing Schedule: '{0}' - Job '{1}' - Group: '{2}'",
                    context.Scheduler.SchedulerName,
                    context.JobDetail.Key.Name,
                    context.JobDetail.Key.Group);
                throw new ApplicationException(
                    $"Error processing Schedule: '{context.Scheduler.SchedulerName}' - Job '{context.JobDetail.Key.Name}' - Group: '{context.JobDetail.Key.Group}'", e);
            }
            return Task.CompletedTask;

        }
    }
}

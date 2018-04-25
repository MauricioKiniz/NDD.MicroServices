using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using Quartz.Impl;
using System.Collections.Specialized;
using NDDigital.Component.Core.Util;
using NDDigital.Component.Core.Schedules;
using System.Reflection;
using Quartz;
using Quartz.Impl.Matchers;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointSchedulerCreation : EndpointCreationBase, IEndpointCreation
    {
        private ServiceBusManager _manager;

        public EndpointSchedulerCreation(ServiceBusManager manager)
        {
            _manager = manager;
        }

        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            dynamic scheduler = endpoint.GetFirstElement("Scheduler");
            if (scheduler == null)
                return cfg;
            string endpointId = endpoint.Id;
            string endpointName = endpoint.Name;

            CreateScheduler(endpointId);
            ScheduleJobs(scheduler, endpointName, endpointId);

            return cfg;
        }

        private async void CreateScheduler(string endpointId)
        {
            var cfgData = ConfigContext.Data;
            string connectionString = cfgData.QuartzConnectionString;

            NameValueCollection properties = new NameValueCollection();
            properties["quartz.scheduler.instanceName"] = "Scheduler_" + endpointId;
            properties["quartz.scheduler.instanceId"] = "instance_one";
            properties["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz";
            properties["quartz.jobStore.useProperties"] = "true";
            properties["quartz.jobStore.dataSource"] = "default";
            properties["quartz.jobStore.tablePrefix"] = "QRTZ_";
            properties["quartz.jobStore.lockHandler.type"] = "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz";
            properties["quartz.dataSource.default.connectionString"] = connectionString;
            properties["quartz.dataSource.default.provider"] = "SqlServer-20";

            var schedulerFactory = new StdSchedulerFactory(properties);
            _manager.Scheduler = await schedulerFactory.GetScheduler().ConfigureAwait(false);
            _manager.Scheduler.SetEndpointInstance(_manager.EndpointInstance);
            await _manager.Scheduler.Start().ConfigureAwait(false);
        }

        private void ScheduleJobs(dynamic scheduler, string endpointName, string endpointId)
        {
            var quartzSchedule = _manager.Scheduler;
            var jobs = scheduler.GetElements("Job");
            foreach (dynamic job in jobs)
                ScheduleJob(endpointName, endpointId, quartzSchedule, job);
        }

        private async void ScheduleJob(string endpointName, string endpointId, IScheduler quartzSchedule, dynamic job)
        {
            string assemblyName = job.AssemblyName;
            string typeName = job.TypeName;
            string jobId = job.JobId;
            string groupId = job.GroupId;
            if (string.IsNullOrEmpty(assemblyName))
                throw new ArgumentException("AssemblyName can not be null in a scheduler");
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("TypeName can not be null in a scheduler");
            if (string.IsNullOrEmpty(jobId))
                throw new ArgumentException("JobId can not be null in a scheduler");
            if (string.IsNullOrEmpty(groupId))
                throw new ArgumentException("GroupId can not be null in a scheduler");

            Assembly ass = Assembly.Load(assemblyName);
            Type classType = ass.GetType(typeName);

            if (classType == null)
                throw new ArgumentException($"TypeName: '{typeName}' not exists in the Assembly: '{assemblyName}'");
            if (typeof(IScheduleDataDefinition).IsAssignableFrom(classType) == false)
                throw new ArgumentException($"TypeName: '{typeName}' not implements the interface 'IScheduleDataDefinition' in the Assembly: '{assemblyName}'");

            IScheduleDataDefinition scheduleDataInstance = (IScheduleDataDefinition)ass.CreateInstance(typeName);

            JobKey jkey = new JobKey(jobId, groupId);
            var jobDetail = await quartzSchedule.GetJobDetail(jkey).ConfigureAwait(false);

            if (jobDetail == null)
            {
                var jobInstance = JobBuilder.Create<SendMessageJob>().
                    WithIdentity(jobId, groupId).
                    UsingJobData(GlobalScheduleConstants.AssemblyName, assemblyName).
                    UsingJobData(GlobalScheduleConstants.TypeName, typeName);

                var JobDataList = job.GetElements("JobData");
                if (JobDataList != null)
                {
                    foreach (var jobData in JobDataList)
                    {
                        string key = jobData.Key;
                        string value = jobData.Value;
                        jobInstance.UsingJobData(key, value);
                    }
                }

                jobDetail = jobInstance.Build();
            }
            var triggerCollection = scheduleDataInstance.GetTrigger(jobDetail, quartzSchedule, endpointName, endpointId);
            if (triggerCollection != null)
                foreach (ITrigger trigger in triggerCollection)
                    await _manager.Scheduler.ScheduleJob(jobDetail, trigger).ConfigureAwait(false);
        }
    }
}

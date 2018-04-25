using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Performance;
using NDDigital.Component.Core.Util;
using NDDigital.Component.Core.Mutators;
using NDDigital.Component.Core.Behaviors;
using log4net;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointEnableDisableFeaturesCreation : EndpointCreationBase, IEndpointCreation
    {
        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            cfg.EnableFeature<Scheduler>();
            if (IsSendOnly(endpoint))
                return cfg;
            var cfgData = ConfigContext.Data;
            bool audit = endpoint.Process.Audit;
            bool createQueues = bool.Parse(cfgData.CreateQueues);
            int criticalErrorTimeout = int.Parse(cfgData.CriticalErrorTimeout);

            UtilHelper.WriteDebug(_logger, "Endpoint {0} - Enable Disable Features", (string)endpoint.Id);

            cfg.DisableFeature<AutoSubscribe>();
            //cfg.EnableInstallers();
            cfg.EnableDurableMessages();

            if (createQueues == false)
                cfg.DoNotCreateQueues();

            if (audit)
            {
                string auditQueue = cfgData.AuditQueue;
                if (string.IsNullOrEmpty(auditQueue))
                    auditQueue = "audit";
                cfg.EnableFeature<Audit>();
                cfg.AuditProcessedMessagesTo(auditQueue);
            }
            else
                cfg.DisableFeature<Audit>();

            string errQueue = cfgData.ErrorQueue;
            if (string.IsNullOrEmpty(errQueue))
                errQueue = "error";
            cfg.SendFailedMessagesTo(errQueue);

            bool enablePerformanceCounters = bool.Parse(ConfigContext.GetValueFromKey("EnablePerformanceCounters", "false"));
            if (enablePerformanceCounters)
            {
                var counters = cfg.EnableWindowsPerformanceCounters();
                counters.EnableSLAPerformanceCounters(TimeSpan.FromSeconds(100));
            }
            cfg.RegisterComponents(components =>
            {
                components.ConfigureComponent<MessageCrosstalkMutator>(DependencyLifecycle.InstancePerCall);
                components.ConfigureComponent<TransportMessageCompressionMutator>(DependencyLifecycle.InstancePerCall);
            });

            cfg.TimeToWaitBeforeTriggeringCriticalErrorOnTimeoutOutages(TimeSpan.FromSeconds(criticalErrorTimeout));
            cfg.DefineCriticalErrorAction(new Func<ICriticalErrorContext, Task>(context =>
            {
                string failMessage = string.Format("Critical error shutting down:'{0}'.", context.Error);
                try
                {
                    UtilHelper.WriteFatal(_logger, failMessage, context.Exception);
                    LogManager.Flush(5000); // cinco segundo no maximo para executar o flush
                }
                finally
                {
                    System.Environment.FailFast(failMessage, context.Exception);
                }
                return Task.CompletedTask;
            }));
            return cfg;
        }
    }
}

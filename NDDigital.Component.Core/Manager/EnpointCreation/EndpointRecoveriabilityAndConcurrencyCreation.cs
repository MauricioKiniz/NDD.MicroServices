using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NDDigital.Component.Core.Util;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointRecoveriabilityAndConcurrencyCreation : EndpointCreationBase, IEndpointCreation
    {
        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            UtilHelper.WriteDebug(_logger, "Endpoint {0} - Recover and Concurrency", (string)endpoint.Id);
            if (IsSendOnly(endpoint))
                return cfg;
            dynamic process = endpoint.Process;
            int maxRetries = process.MaxRetries;
            int maximumConcurrencyLevel = process.MaximumConcurrencyLevel;
            cfg.LimitMessageProcessingConcurrencyTo(maximumConcurrencyLevel);

            var timeoutMaxConcurrencyLevelObj = process.TimeoutManagerMaximumConcurrencyLevel;
            if (timeoutMaxConcurrencyLevelObj != null)
            {
                int timeoutMaxConcurrencyLevel = (int)timeoutMaxConcurrencyLevelObj;
                var timeoutManager = cfg.TimeoutManager();
                timeoutManager.LimitMessageProcessingConcurrencyTo(timeoutMaxConcurrencyLevel);
            }
            var recoverability = cfg.Recoverability();
            recoverability.Immediate(
                customizations: immediate =>
                {
                    immediate.NumberOfRetries(maxRetries);
                });

            bool enabled = process.SecondLevelEnabled;
            int numberOfRetries = (enabled) ? process.SecondLevelRetries : 0;
            int secondLevelInterval = (enabled) ? process.SecondLevelInterval : 0;
            TimeSpan timeIncrease = TimeSpan.FromSeconds(secondLevelInterval);
            recoverability.Delayed(
                customizations: delayed =>
                {
                    delayed.NumberOfRetries(numberOfRetries);
                    delayed.TimeIncrease(timeIncrease);
                });
            recoverability.DisableLegacyRetriesSatellite();
            return cfg;
        }
    }
}

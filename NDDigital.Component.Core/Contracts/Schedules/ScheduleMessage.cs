using NServiceBus;
using System;

namespace NDDigital.Component.Core.Contracts.Schedules
{
    public class ScheduleMessage : IMessage
    {
        public Guid EnterpriseId { get; set; }

        public string MessageData { get; set; }

        public string ScheduledMessageCreator { get; set; }

        public string ScheduleName { get; set; }

        public TimeSpan StartInterval { get; set; }

        public TimeSpan NextInterval { get; set; }
    }

    public class ScheduleTimeoutMessage : IMessage
    {
        public TimeSpan Interval { get; set; }
    }
}

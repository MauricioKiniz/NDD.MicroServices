using NServiceBus;
using System;

namespace NDDigital.Component.Core.Schedules
{
    public class ScheduleSagaData: IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual Guid EnterpriseId { get; set; }

        public virtual string ScheduleName { get; set; }

        public virtual bool IsTaskAlreadyScheduled { get; set; }

        public virtual string MessageData { get; set; }

        public virtual string ScheduleBusinessMessageCreator { get; set; }
    }
}

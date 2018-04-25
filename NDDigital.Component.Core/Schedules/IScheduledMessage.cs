using System;

namespace NDDigital.Component.Core.Schedules
{
    public interface IScheduledMessage
    {
        object CreateMessage(Guid enterpiseId, string messageData);
    }
}

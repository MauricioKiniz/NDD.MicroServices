using NDDigital.Component.Core.Contracts.Schedules;
using NServiceBus;
using System;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Schedules
{
    public class ScheduleSaga : Saga<ScheduleSagaData>,
        IAmStartedByMessages<ScheduleMessage>,
        IHandleTimeouts<ScheduleTimeoutMessage>
    {
        public Task Handle(ScheduleMessage message, IMessageHandlerContext context)
        {
            if (!Data.IsTaskAlreadyScheduled)
            {
                Data.EnterpriseId = message.EnterpriseId;
                Data.ScheduleName = message.ScheduleName;
                Data.IsTaskAlreadyScheduled = true;
                Data.MessageData = message.MessageData;
                Data.ScheduleBusinessMessageCreator = message.ScheduledMessageCreator;

                var schMessage = new ScheduleTimeoutMessage { Interval = message.NextInterval };

                RequestTimeout<ScheduleTimeoutMessage>(context, message.StartInterval, schMessage);
            }
            Data.ScheduleBusinessMessageCreator = message.ScheduledMessageCreator;
            return Task.CompletedTask;
        }

        public async Task Timeout(ScheduleTimeoutMessage state, IMessageHandlerContext context)
        {
            Type tp = Type.GetType(Data.ScheduleBusinessMessageCreator);

            if (tp == null)
                throw new ArgumentException("The type class was not found. " + Data.ScheduleBusinessMessageCreator);

            if (typeof(IScheduledMessage).IsAssignableFrom(tp) == false)
                throw new ArgumentException("The type class is not compatible. The class must implement IScheduledMessage. " + Data.ScheduleBusinessMessageCreator);

            IScheduledMessage scheduledMessage = (IScheduledMessage)Activator.CreateInstance(tp);
            object newMessage = scheduledMessage.CreateMessage(Data.EnterpriseId, Data.MessageData);
            await context.Send(newMessage).ConfigureAwait(false);
            await RequestTimeout<ScheduleTimeoutMessage>(context, state.Interval, state).ConfigureAwait(false);
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ScheduleSagaData> mapper)
        {
            mapper.ConfigureMapping<ScheduleMessage>(msg => msg.ScheduleName).ToSaga(saga => saga.ScheduleName);
        }
    }
}

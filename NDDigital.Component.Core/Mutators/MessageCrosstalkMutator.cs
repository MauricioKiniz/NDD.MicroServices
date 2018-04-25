using System;
using System.Threading.Tasks;
using NDDigital.Component.Core.Contracts.Crosstalk;
using NDDigital.Component.Core.Crosstalks;
using NServiceBus.MessageMutator;

namespace NDDigital.Component.Core.Mutators
{
    public class MessageCrosstalkMutator : IMutateOutgoingMessages
    {
        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
            object message = context.OutgoingMessage;
            if (message is CrosstalkMessage)
            {
                CrosstalkMessage cm = (CrosstalkMessage)message;
                object newMessage = CrosstalkToHandlerManager.GetMessage(cm.Crosstalk);

                if (newMessage != null)
                    message = newMessage;
            }
            context.OutgoingMessage = message;
            return Task.CompletedTask;
        }
    }
}

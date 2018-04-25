using System;
using System.Threading.Tasks;
using NDDigital.Component.Core.Contracts.Crosstalk;
using NDDigital.Component.Core.Domain.MessageResponse;
using NDDigital.Component.Core.Util.Dynamics;
using NServiceBus;
using NDDigital.Component.Core.Util;

namespace NDDigital.Component.Core.Handlers
{
    public class CreateResponseHandler : IHandleMessages<CreateResponseMessage>
    {
        private IMessageResponseService _service;

        public CreateResponseHandler(IMessageResponseService service)
        {
            _service = service;
        }

        public void Handle(CreateResponseMessage message)
        {
        }

        public Task Handle(CreateResponseMessage message, IMessageHandlerContext context)
        {
            dynamic crosstalk = DynamicXmlObject.Parse(message.CrosstalkXml);

            var messageResponse = new MessageResponse()
            {
                EnterpriseId = crosstalk.CrosstalkHeader.EnterpriseId,
                MessageId = message.MessageId,
                ProcessCode = crosstalk.CrosstalkHeader.ProcessCode,
                MessageType = crosstalk.CrosstalkHeader.MessageType,
                QueueMessageId = context.MessageId,
                Response = crosstalk.ToString()
            };

            _service.Add(messageResponse);

            return Task.CompletedTask;
        }
    }
}

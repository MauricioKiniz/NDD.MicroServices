using NDDigital.Component.Core.Contracts.Crosstalk;
using NDDigital.Component.Core.Domain.MessageResponse;
using NServiceBus;
using System;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Handlers
{
    public class SaveResponseHandler : IHandleMessages<SaveResponseMessage>
    {
        private IMessageResponseService _service;

        public SaveResponseHandler(IMessageResponseService service)
        {
            _service = service;
        }

        public async Task Handle(SaveResponseMessage message, IMessageHandlerContext context)
        {
            message.Retries = message.Retries + 1;

            MessageResponse messageResponse = _service.GetById(message.MessageId);

            bool messageSaved = _service.Update(messageResponse, message.Retries, message.Response);

            if (!messageSaved)
            {
                SendOptions options = new SendOptions();
                options.DelayDeliveryWith(TimeSpan.FromSeconds(20 * message.Retries));
                await context.Send(message, options);
            }
        }
    }
}

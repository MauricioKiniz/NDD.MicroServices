using System;

namespace NDDigital.Component.Core.Domain.MessageResponse
{
    public interface IMessageResponseService
    {
        void Add(MessageResponse crosstalkResponse);

        MessageResponse GetById(Guid messageId);

        bool Update(MessageResponse crosstalkResponse, int retries, string saveResponse);
    }
}

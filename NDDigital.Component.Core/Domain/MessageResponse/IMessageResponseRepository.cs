using System;

namespace NDDigital.Component.Core.Domain.MessageResponse
{
    public interface IMessageResponseRepository
    {
        MessageResponse GetById(Guid messageId);

        void Add(MessageResponse messageResponse);

        void Update(MessageResponse messageResponse);
    }
}

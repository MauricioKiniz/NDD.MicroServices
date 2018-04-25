using NServiceBus;
using System;

namespace NDDigital.Component.Core.Contracts.Crosstalk
{
    public class SaveResponseMessage : IMessage
    {
        public Guid MessageId { get; set; }

        public string Response { get; set; }

        public int Retries { get; set; }
    }
}

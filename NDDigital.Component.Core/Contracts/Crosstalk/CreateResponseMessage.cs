using NServiceBus;
using System;

namespace NDDigital.Component.Core.Contracts.Crosstalk
{
    public class CreateResponseMessage : IMessage
    {
        public Guid MessageId { get; set; }

        public string CrosstalkXml { get; set; }
    }
}

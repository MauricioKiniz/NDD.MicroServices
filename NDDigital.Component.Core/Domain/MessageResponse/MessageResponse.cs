using System;

namespace NDDigital.Component.Core.Domain.MessageResponse
{
    public class MessageResponse
    {
        public Guid Id { get; set; }

        public Guid MessageId { get; set; }

        public Guid EnterpriseId { get; set; }

        public int ProcessCode { get; set; }

        public int MessageType { get; set; }

        public string QueueMessageId { get; set; }

        public string Response { get; set; }

        public DateTime InsertDate { get; set; }

        public DateTime LastUpdate { get; set; }
    }
}

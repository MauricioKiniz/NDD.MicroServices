using System;

namespace NDDigital.Component.Core.Ado.Data
{
    public class CrosstalkMessageFinalizeData
    {
        public AdoHelper AdoHelper { get; set; }

        public string QueueMessageId { get; set; }

        public string Response { get; set; }

        public Guid MessageId { get; set; }

        public long ProcessTime { get; set; }

        public long ComponentTime { get; set; }
    }
}

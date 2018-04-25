using NServiceBus;

namespace NDDigital.Component.Core.Contracts.Crosstalk
{
    public class CrosstalkMessage : IMessage
    {
        public string Crosstalk { get; set; }
    }
}

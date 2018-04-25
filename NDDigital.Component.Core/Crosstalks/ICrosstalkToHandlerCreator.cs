namespace NDDigital.Component.Core.Crosstalks
{
    public interface ICrosstalkToHandlerCreator
    {
        object GetMessageFrom(int processCode, int messageType, string crosstalkXml);
    }
}

using NDDigital.Component.Core.Domain.MessageResponse;
using NDDigital.Component.Core.Util.Dynamics;
using System;

namespace NDDigital.Component.Core.Service
{
    public class MessageResponseService : IMessageResponseService
    {
        private IMessageResponseRepository _repository;

        public MessageResponseService(IMessageResponseRepository repository)
        {
            _repository = repository;
        }

        public void Add(MessageResponse crosstalkResponse)
        {
            dynamic crosstalk = DynamicXmlObject.Parse(crosstalkResponse.Response);

            crosstalk.CrosstalkHeader.ResponseCode = "200";
            crosstalk.CrosstalkHeader.ResponseMessage = "The message was received and is being processed";

            crosstalkResponse.Response = crosstalk.ToString();

            _repository.Add(crosstalkResponse);
        }

        public MessageResponse GetById(Guid messageId)
        {
            return _repository.GetById(messageId);
        }

        public bool Update(MessageResponse messageResponse, int retries, string saveResponse)
        {
            if (messageResponse == null)
            {
                if (retries > 3)
                    throw new ApplicationException("Message was executed more then 3 times on saving data to messageresponse");
                else
                    return false;
            }
            else
            {
                dynamic crosstalk = DynamicXmlObject.Parse(messageResponse.Response);

                crosstalk.CrosstalkHeader.ResponseCode = "205";
                crosstalk.CrosstalkHeader.ResponseMessage = "The message was processed with success";
                crosstalk.CrosstalkBody.RawData = saveResponse;

                messageResponse.Response = crosstalk.ToString();

                _repository.Update(messageResponse);

                return true;
            }
        }
    }
}

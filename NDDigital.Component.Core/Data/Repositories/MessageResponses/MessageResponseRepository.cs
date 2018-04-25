using NDDigital.Component.Core.Ado.Data;
using NDDigital.Component.Core.Domain.MessageResponse;
using NDDigital.Component.Core.Util;
using System;
using System.Data;

namespace NDDigital.Component.Core.Data.Repositories.MessageResponses
{
    public class MessageResponseRepository : IMessageResponseRepository
    {
        private string _connectionString = ConfigContext.Data.MiddlewareDatabase;

        private static readonly string InsertMessage = "INSERT INTO MIDDLEWARE.MESSAGERESPONSE (ID, MESSAGEID, ENTERPRISEID, PROCESSCODE, MESSAGETYPE, QUEUEMESSAGEID, RESPONSE) VALUES (@ID, @MESSAGEID, @ENTERPRISEID, @PROCESSCODE, @MESSAGETYPE, @QUEUEMESSAGEID, @RESPONSE)";

        private static readonly string SelectMessage = "SELECT MESSAGEID, ENTERPRISEID, PROCESSCODE, MESSAGETYPE, QUEUEMESSAGEID, RESPONSE FROM MIDDLEWARE.MESSAGERESPONSE WITH (UPDLOCK, ROWLOCK) WHERE MESSAGEID = @MESSAGEID";

        private static readonly string UpdateMessage = "UPDATE MIDDLEWARE.MESSAGERESPONSE SET RESPONSE = @RESPONSE, LASTUPDATE = GETDATE() WHERE MESSAGEID = @MESSAGEID";

        public void Add(MessageResponse messageResponse)
        {
            using (AdoHelper adHelp = new AdoHelper(_connectionString))
            {
                adHelp.ExecNonQuery(InsertMessage,
                    "@ID", messageResponse.Id,
                    "@MESSAGEID", messageResponse.MessageId,
                    "@ENTERPRISEID", messageResponse.EnterpriseId,
                    "@PROCESSCODE", messageResponse.ProcessCode,
                    "@MESSAGETYPE", messageResponse.MessageType,
                    "@QUEUEMESSAGEID", messageResponse.QueueMessageId,
                    "@RESPONSE", messageResponse.Response);
            }
        }

        public MessageResponse GetById(Guid messageId)
        {
            MessageResponse messageResponse = null;

            using (AdoHelper adHelp = new AdoHelper(_connectionString))
            {
                adHelp.ExecDataReader(SelectMessage, delegate (IDataRecord record)
                {
                    messageResponse = new MessageResponse()
                    {
                        MessageId = record.GetGuid(0),
                        EnterpriseId = record.GetGuid(1),
                        ProcessCode = record.GetInt32(2),
                        MessageType = record.GetInt32(3),
                        QueueMessageId = record.GetString(4),
                        Response = record.GetString(5)
                    };
                }, "@MESSAGEID", messageId);
            }

            return messageResponse;
        }

        public void Update(MessageResponse messageResponse)
        {
            using (AdoHelper adHelp = new AdoHelper(_connectionString))
            {
                adHelp.ExecNonQuery(UpdateMessage, "@RESPONSE", messageResponse.Response, "@MESSAGEID", messageResponse.MessageId);
            }
        }
    }
}

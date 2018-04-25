namespace NDDigital.Component.Core.Ado.Data
{
    public static class CrosstalkDbHelper
    {
        private static readonly string UpdateResponseTable = @"
            UPDATE 
	            MIDDLEWARE.MESSAGERESPONSE WITH(ROWLOCK)
	            SET 
		            LASTUPDATE = GETDATE(), 
		            QUEUEMESSAGEID = @QUEUEMESSAGEID, 
		            RESPONSE = @RESPONSE
	            WHERE
		            MESSAGEID = @MESSAGEID";

        public static void FinalizeMessage(CrosstalkMessageFinalizeData data)
        {
            data.AdoHelper.ExecNonQuery(UpdateResponseTable,
                "@QUEUEMESSAGEID", data.QueueMessageId,
                "@RESPONSE", data.Response,
                "@MESSAGEID", data.MessageId);
        }
    }
}

using NDDigital.Component.Core.Util.Dynamics;
using System;

namespace NDDigital.Component.Core.Crosstalks
{
    public static class CrosstalkHelper
    {
        public static dynamic CreateCrosstalk(string header, string rawData = null)
        {
            dynamic crosstalk = DynamicXmlObject.Parse(header);
            var cheader = crosstalk.CrosstalkHeader;
            Guid destinationId = cheader.DestinationId;

            if (destinationId == Guid.Empty || destinationId == null)
            {
                destinationId = cheader.EnterpriseId;
                cheader.DestinationId = destinationId.ToString();
            }

            if (string.IsNullOrEmpty(rawData) == false)
                crosstalk.CrosstalkBody.RawData = rawData;

            return crosstalk;
        }

        public static void CheckCrosstalkHeader(string header)
        {
            try
            {
                int intToParse = 0;
                Guid guidData = Guid.Empty;
                dynamic crosstalk = DynamicXmlObject.Parse(header);
                var cheader = crosstalk.CrosstalkHeader;
                string processCode = cheader.ProcessCode;
                string messageType = cheader.MessageType;
                string enterpriseId = cheader.EnterpriseId;
                string destinationId = cheader.destinationId;
                string exchangePattern = cheader.ExchangePattern;
                string token = cheader.Token;

                if (string.IsNullOrEmpty(processCode) || int.TryParse(processCode, out intToParse) == false)
                    throw new CrosstalkException(CrosstalkResponseHelper.R204, "ProcessCode");

                if (string.IsNullOrEmpty(messageType) || int.TryParse(messageType, out intToParse) == false)
                    throw new CrosstalkException(CrosstalkResponseHelper.R204, "MessageType");

                if (string.IsNullOrEmpty(enterpriseId) || Guid.TryParse(enterpriseId, out guidData) == false)
                    throw new CrosstalkException(CrosstalkResponseHelper.R204, "EnterpriseId");

                if (string.IsNullOrEmpty(destinationId) || Guid.TryParse(destinationId, out guidData) == false)
                    throw new CrosstalkException(CrosstalkResponseHelper.R204, "DestinationId");

                if (string.IsNullOrEmpty(token) || Guid.TryParse(token, out guidData) == false)
                    throw new CrosstalkException(CrosstalkResponseHelper.R204, "Token");

                if (string.IsNullOrEmpty(exchangePattern))
                    throw new CrosstalkException(CrosstalkResponseHelper.R204, "ExchangePattern");
            }
            catch (CrosstalkException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CrosstalkException(CrosstalkResponseHelper.R203, e);
            }
        }
    }
}

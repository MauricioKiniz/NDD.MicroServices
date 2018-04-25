using System;

namespace NDDigital.Component.Core.Crosstalks
{
    public class CrosstalkException : ApplicationException
    {
        public string Code { get; set; }

        public CrosstalkException(string code, params string[] dataArr) :
            base(CrosstalkResponseHelper.ResponseFormatted(code, dataArr))
        {
            Code = code;
        }

        public CrosstalkException(string code, Exception innerException, params string[] dataArr) :
            base(CrosstalkResponseHelper.ResponseFormatted(code, dataArr), innerException)
        {
            Code = code;
        }
    }
}

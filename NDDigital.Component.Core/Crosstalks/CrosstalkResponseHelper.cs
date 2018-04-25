using System.Collections.Generic;

namespace NDDigital.Component.Core.Crosstalks
{
    public static class CrosstalkResponseHelper
    {
        private static SortedList<string, string> Responses;

        public static readonly string R200 = "200";
        public static readonly string R201 = "201";
        public static readonly string R202 = "202";
        public static readonly string R203 = "203";
        public static readonly string R204 = "204";
        public static readonly string R205 = "205";
        public static readonly string R999 = "999";

        static CrosstalkResponseHelper()
        {
            Responses = new SortedList<string, string>();

            Responses.Add(R200, "The message was received and is being processed");
            Responses.Add(R201, "The message was rejected because of the following error: '{0}'");
            Responses.Add(R202, "The message can not be read, wait a moment and try again");
            Responses.Add(R203, "The message format is incorrect. The header or body of the message are incorrect");
            Responses.Add(R204, "The message is incorrect. The field: '{0}' in header is incorrect");
            Responses.Add(R205, "The message was processed with success");
            Responses.Add(R999, "Unknown error");
        }

        public static string Response(string index)
        {
            return Responses[index];
        }

        public static string ResponseFormatted(string index, params object[] parameters)
        {
            return string.Format(Response(index), parameters);
        }
    }
}

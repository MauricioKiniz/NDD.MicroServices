using System;
using System.Text;

namespace NDDigital.Component.Core.Ado.Data
{
    public static class DataConst
    {
        private static string GeneralKey = "bmRkIUAjJCUyMDE1MDglISE=";

        public static string Key
        {
            get { return Encoding.UTF8.GetString(Convert.FromBase64String(GeneralKey)); }
        }
    }
}

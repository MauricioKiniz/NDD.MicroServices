using Microsoft.Win32;
using NDDigital.Component.Core.Config;
using NDDigital.Component.Core.Util.Dynamics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace NDDigital.Component.Core.Util
{
    public static class ConfigContext
    {
        private static dynamic DynamicData;
        private static string SectionName = "middlewareGroup/middlewareConf";

        private static MiddlewareSection MiddlewareSection;

        static ConfigContext()
        {
            LoadConfigContext();
        }

        private static void LoadConfigContext()
        {
            try
            {
                MiddlewareSection = (MiddlewareSection)ConfigurationManager.GetSection(SectionName);
                if (MiddlewareSection == null)
                    throw new TypeLoadException($"A seção: '{SectionName}' de configuração não foi definida no arquivo de configuração");
                LoadConfigDictionary();
            } catch(Exception e)
            {
                var log = UtilHelper.GetDefaultEventLog();
                UtilHelper.WriteLogEntry(log, e.Message, EventLogEntryType.Error);
            }
        }

        private static void LoadConfigDictionary()
        {
            DynamicData = DynamicDictObject.Create();
            var properties = MiddlewareSection.GetType().GetProperties();
            foreach (PropertyInfo pinfo in properties)
            {
                string name = pinfo.Name;
                object rawValue = pinfo.GetValue(MiddlewareSection);
                string data = (rawValue == null) ? string.Empty : rawValue.ToString();
                DynamicData.Add(name, data);
            }
        }

        public static dynamic Data
        {
            get
            {
                return DynamicData;
            }
        }

        public static string GetValueFromKey(string key, string defaultValue = null)
        {
            if (!DynamicData.ContainsKey(key))
            {
                if (defaultValue == null)
                    throw new KeyNotFoundException(string.Format("A chave '{0}' não foi encontrada no dicionário", key));

                return defaultValue;
            }

            return DynamicData.GetValue(key);
        }
     
    }
}

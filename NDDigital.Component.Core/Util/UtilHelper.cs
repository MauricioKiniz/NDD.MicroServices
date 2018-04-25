using NServiceBus.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace NDDigital.Component.Core.Util
{
    public static class UtilHelper
    {
        private static Encoding XmlEncoder = new UTF8Encoding(false);

        private static readonly XmlWriterSettings DefaultSettings = new XmlWriterSettings()
        {
            Indent = true,
            OmitXmlDeclaration = true,
            Encoding = new UTF8Encoding(false)
        };

        public static T GetData<T>(object value, T initialValue = default(T), IFormatProvider provider = null)
        {
            Type tType = typeof(T);
            if (value == null || value == DBNull.Value)
                return initialValue;
            if (tType == typeof(string))
                return (T)value;
            MethodInfo mi = tType.GetMethod("Parse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(string) },
                null);
            if (mi == null)
                return initialValue;
            T toParse = (T)tType.Assembly.CreateInstance(tType.FullName);
            return (T)mi.Invoke(toParse, new object[] { value.ToString() });
        }

        public static T GetData<T>(XmlNode nodeValue, T initialValue = default(T), IFormatProvider provider = null)
        {
            return GetData<T>((nodeValue == null || string.IsNullOrEmpty(nodeValue.InnerText)) ? null : nodeValue.InnerText);
        }

        public static T GetDataEnum<T>(XmlNode nodeValue, T initialValue = default(T))
        {
            var value = (nodeValue == null || string.IsNullOrEmpty(nodeValue.InnerText)) ? null : nodeValue.InnerText;
            if (value == null)
                return initialValue;
            object obj = int.Parse(value);
            return (T)obj;
        }

        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        public static string GetLicense()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NDDigital.Component.Core.Util.License.xml";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        public static string GetMiddlewareSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NDDigital.Component.Core.Util.middleware.xsd";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        public static T CreateComponent<T>(string assemblyName, string fullClassName, object[] constructorParameters = null)
        {
            string assName = Path.GetExtension(assemblyName).Equals(".dll", StringComparison.CurrentCultureIgnoreCase) ? assemblyName : assemblyName + ".dll";
            string assPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assName);
            Assembly ass = Assembly.LoadFrom(assPath);
            if (constructorParameters == null)
                return (T)ass.CreateInstance(fullClassName);
            List<Type> types = new List<Type>(ass.ExportedTypes);
            var classType = types.Find(p => p.FullName == fullClassName);
            var classObject = Activator.CreateInstance(classType, constructorParameters);
            return (T)classObject;
        }

        public static string GetEmbededResource(string resourceName, Assembly assemblySearch = null)
        {
            Assembly assembly = (assemblySearch == null) ? Assembly.GetCallingAssembly() : assemblySearch;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        public static byte[] GetEmbededResourceArray(string resourceName, Assembly assemblySearch = null)
        {
            Assembly assembly = (assemblySearch == null) ? Assembly.GetCallingAssembly() : assemblySearch;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var memo = new MemoryStream())
                {
                    stream.CopyTo(memo);
                    return memo.ToArray();
                }
            }
        }

        public static void ClearList<T>(ref T list)
        {
            Type listType = list.GetType();
            MethodInfo mi = listType.GetMethod("Clear");
            if (mi != null)
            {
                mi.Invoke(list, null);
                list = default(T);
            }
        }


        public static byte[] CompressDataInZipFormat(string toReturn)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("root");
                    using (StreamWriter writer = new StreamWriter(entry.Open()))
                        writer.Write(toReturn);
                }
                return ms.ToArray();
            }
        }

        public static string GetFromCompressedZip(byte[] rawData)
        {
            using (ZipArchive zipProc = new ZipArchive(new MemoryStream(rawData)))
            {
                Encoding sourceEncode = Encoding.UTF8;
                var entry = zipProc.GetEntry("root");
                using (var entryStream = new MemoryStream())
                {
                    using (var entryOpened = entry.Open())
                    {
                        entryOpened.CopyTo(entryStream);
                        var entryDataArr = Encoding.Convert(sourceEncode,
                            sourceEncode, entryStream.ToArray());
                        return sourceEncode.GetString(entryDataArr);
                    }
                }
            }
        }

        public static string GetStringFromXmlNode(this XmlNode node)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
            using (XmlNoNamespaceWriter xw = new XmlNoNamespaceWriter(writer))
                node.WriteTo(xw);
            return sb.ToString();
        }

        private static readonly string EventLogSource = "NDDigital.DefaultLog";
        private static readonly string EventLogName = "Application";
        public static EventLog GetDefaultEventLog()
        {
            try
            {
                EventLog eventLog = new EventLog(EventLogName);
                eventLog.Source = EventLogSource;
                return eventLog;
            } catch
            {
                return null;
            }
        }

        public static void WriteLogEntry(EventLog log, string message, params object[] values)
        {
            WriteLogEntry(log, message, EventLogEntryType.Information, values);
        }

        public static void WriteLogEntry(EventLog log,
            string message,
            EventLogEntryType evnType,
            params object[] values)
        {
            if (log == null)
                return;
            string mess = string.Format(message, values);
            try
            {
                log.WriteEntry(mess, evnType);
            } catch(InvalidOperationException)
            {

            }
        }



        public static string GetQueueFullName(string name, string machine)
        {
            return name + "@" + machine;
        }

        public static string GetFullStackTrace(Exception e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(e.StackTrace);
            Exception inner = e.InnerException;
            while (inner != null)
            {
                sb.AppendLine("------------------------");
                sb.AppendLine(inner.StackTrace);
                inner = inner.InnerException;
            }
            return sb.ToString();
        }

        public static string XmlSerialize(object source)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(ms, DefaultSettings))
                {
                    XmlSerializer serial = new XmlSerializer(source.GetType());
                    serial.Serialize(writer, source);
                    writer.Flush();
                    return XmlEncoder.GetString(ms.ToArray());
                }
            }
        }

        public static void WriteDebug(ILog log, string message, params object[] parameters)
        {
            if (log.IsDebugEnabled)
            {
                if (parameters == null)
                    log.Debug(message);
                else
                    log.DebugFormat(message, parameters);
            }
        }

        public static void WriteError(ILog log, string message, params object[] parameters)
        {
            if (log.IsErrorEnabled)
            {
                if (parameters == null)
                    log.Error(message);
                else
                    log.ErrorFormat(message, parameters);
            }
        }

        public static void WriteInfo(ILog log, string message, params object[] parameters)
        {
            if (log.IsInfoEnabled)
            {
                if (parameters == null)
                    log.Info(message);
                else
                    log.InfoFormat(message, parameters);
            }
        }

        public static void WriteFatal(ILog log, string message, params object[] parameters)
        {
            if (log.IsFatalEnabled)
            {
                if (parameters == null)
                    log.Fatal(message);
                else
                    log.FatalFormat(message, parameters);
            }
        }

        public static void WriteFatal(ILog log, string message, Exception exception)
        {
            if (log.IsFatalEnabled)
                log.Fatal(message, exception);
        }

        public static void GetAllIEventTypes(List<Type> externalList)
        {
            if (externalList.Count == 0)
                return;
            Type eventType = typeof(NServiceBus.IEvent);
            var list = externalList.FindAll(element => eventType.IsAssignableFrom(element));
            externalList.Clear();
            externalList.AddRange(list);
        }

        public static string CalculateMD5Hash(string input)
        {

            // Primeiro passo, calcular o MD5 hash a partir da string
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            return Convert.ToBase64String(hash);
        }
    }
}

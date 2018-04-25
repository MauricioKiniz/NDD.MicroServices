using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Net;
using System.Text;
using System.Xml;
using NServiceBus.Support;
using NServiceBus.Transports.Msmq;
using NServiceBus;
using NServiceBus.Transport.Msmq;

namespace NDDigital.Component.Core.Util
{
    /// <summary>
    ///     MSMQ-related utility functions
    /// </summary>
    public class MsmqUtils
    {
        public static string GetFullPath(MsmqAddress value)
        {
            IPAddress ipAddress;
            if(IPAddress.TryParse(value.Machine, out ipAddress))
            {
                return PREFIX_TCP + GetFullPathWithoutPrefix(value);
            }

            return PREFIX + GetFullPathWithoutPrefix(value);
        }

        public static string GetFullPath(string queue, bool withDirectPrefix = false)
        {
            if (withDirectPrefix)
                return DIRECTPREFIX + GetFullPathWithoutPrefix(queue, RuntimeEnvironment.MachineName);
            return PREFIX + GetFullPathWithoutPrefix(queue, RuntimeEnvironment.MachineName);
        }

        public static string GetFullPathWithoutPrefix(MsmqAddress address)
        {
            return GetFullPathWithoutPrefix(address.Queue, address.Machine);
        }

        public static string GetFullPathWithoutPrefix(string queue, string machine)
        {
            return machine + MsmqUtils.PRIVATE + queue;
        }

        public static Dictionary<string, string> ExtractHeaders(Message msmqMessage)
        {
            var headers = DeserializeMessageHeaders(msmqMessage);

            //note: we can drop this line when we no longer support interop btw v3 + v4
            if(msmqMessage.ResponseQueue != null)
            {
                headers[Headers.ReplyToAddress] = GetIndependentAddressForQueue(msmqMessage.ResponseQueue).ToString();
            }

            if(Enum.IsDefined(typeof(MessageIntentEnum), msmqMessage.AppSpecific))
            {
                headers[Headers.MessageIntent] = ((MessageIntentEnum)msmqMessage.AppSpecific).ToString();
            }

            headers[Headers.CorrelationId] = GetCorrelationId(msmqMessage, headers);

            return headers;
        }

        static Dictionary<string, string> DeserializeMessageHeaders(Message m)
        {
            var result = new Dictionary<string, string>();

            if(m.Extension.Length == 0)
            {
                return result;
            }

            //This is to make us compatible with v3 messages that are affected by this bug:
            //http://stackoverflow.com/questions/3779690/xml-serialization-appending-the-0-backslash-0-or-null-character
            var extension = Encoding.UTF8.GetString(m.Extension).TrimEnd('\0');
            object o;
            using(var stream = new StringReader(extension))
            {
                using(var reader = XmlReader.Create(stream, new XmlReaderSettings
                {
                    CheckCharacters = false
                }))
                {
                    o = headerSerializer.Deserialize(reader);
                }
            }

            foreach(var pair in (List<HeaderInfo>)o)
            {
                if(pair.Key != null)
                {
                    result.Add(pair.Key, pair.Value);
                }
            }

            return result;
        }

        static MsmqAddress GetIndependentAddressForQueue(MessageQueue q)
        {
            var arr = q.FormatName.Split('\\');
            var queueName = arr[arr.Length - 1];

            var directPrefixIndex = arr[0].IndexOf(DIRECTPREFIX);
            if(directPrefixIndex >= 0)
            {
                return new MsmqAddress(queueName, arr[0].Substring(directPrefixIndex + DIRECTPREFIX.Length));
            }

            var tcpPrefixIndex = arr[0].IndexOf(DIRECTPREFIX_TCP);
            if(tcpPrefixIndex >= 0)
            {
                return new MsmqAddress(queueName, arr[0].Substring(tcpPrefixIndex + DIRECTPREFIX_TCP.Length));
            }

            try
            {
                // the pessimistic approach failed, try the optimistic approach
                arr = q.QueueName.Split('\\');
                queueName = arr[arr.Length - 1];
                return new MsmqAddress(queueName, q.MachineName);
            }
            catch
            {
                throw new Exception("Could not translate format name to independent name: " + q.FormatName);
            }
        }

        static string GetCorrelationId(Message message, Dictionary<string, string> headers)
        {
            string correlationId;

            if(headers.TryGetValue(Headers.CorrelationId, out correlationId))
            {
                return correlationId;
            }

            if(message.CorrelationId == "00000000-0000-0000-0000-000000000000\\0")
            {
                return null;
            }

            //msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end that the sender added to make it compatible
            //The replace can be removed in v5 since only v3 messages will need this
            return message.CorrelationId.Replace("\\0", String.Empty);
        }


        const string DIRECTPREFIX = "DIRECT=OS:";
        const string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        const string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;
        const string PREFIX = "FormatName:" + DIRECTPREFIX;
        internal const string PRIVATE = "\\private$\\";
        static System.Xml.Serialization.XmlSerializer headerSerializer = new System.Xml.Serialization.XmlSerializer(typeof(List<HeaderInfo>));
        //static ILog Logger = LogManager.GetLogger<MsmqUtils>();
    }

    public class MsmqAddress
    {

        /// <summary>
        /// The (lowercase) name of the queue not including the name of the machine or location depending on the address mode.
        /// </summary>
        public readonly string Queue;

        /// <summary>
        /// The (lowercase) name of the machine or the (normal) name of the location depending on the address mode.
        /// </summary>
        public readonly string Machine;

        /// <summary>
        /// Parses a string and returns an Address.
        /// </summary>
        /// <param name="address">The full address to parse.</param>
        /// <returns>A new instance of <see cref="Address"/>.</returns>
        public static MsmqAddress Parse(string address)
        {
            if(string.IsNullOrEmpty(address))
                throw new ArgumentException("address can not be empty");

            var split = address.Split('@');

            if(split.Length > 2)
            {
                var message = string.Format("Address contains multiple @ characters. Address supplied: '{0}'", address);
                throw new ArgumentException(message, "address");
            }

            var queue = split[0];
            if(string.IsNullOrWhiteSpace(queue))
            {
                var message = string.Format("Empty queue part of address. Address supplied: '{0}'", address);
                throw new ArgumentException(message, "address");
            }

            string machineName;
            if(split.Length == 2)
            {
                machineName = split[1];
                if(string.IsNullOrWhiteSpace(machineName))
                {
                    var message = string.Format("Empty machine part of address. Address supplied: '{0}'", address);
                    throw new ArgumentException(message, "address");
                }
                machineName = ApplyLocalMachineConventions(machineName);
            }
            else
            {
                machineName = RuntimeEnvironment.MachineName;
            }

            return new MsmqAddress(queue, machineName);
        }

        static string ApplyLocalMachineConventions(string machineName)
        {
            if(
                machineName == "." ||
                machineName.ToLower() == "localhost" ||
                machineName == IPAddress.Loopback.ToString()
                )
            {
                return RuntimeEnvironment.MachineName;
            }
            return machineName;
        }

        /// <summary>
        /// Instantiate a new Address for a known queue on a given machine.
        /// </summary>
        ///<param name="queueName">The queue name.</param>
        ///<param name="machineName">The machine name.</param>
        public MsmqAddress(string queueName, string machineName)
        {
            Queue = queueName;
            Machine = machineName;
        }

        /// <summary>
        /// Returns a string representation of the address.
        /// </summary>
        public override string ToString()
        {
            return Queue + "@" + Machine;
        }

        /// <summary>
        /// Returns a string representation of the address.
        /// </summary>
        public string ToString(string qualifier)
        {
            return Queue + "." + qualifier + "@" + Machine;
        }

    }

}

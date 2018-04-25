using log4net;
using NDDigital.Component.Core.Util;
using NServiceBus;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Messages;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Mutators
{
    public class TransportMessageCompressionMutator : IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages
    {
        private static ILog logger = LogManager.GetLogger("TransportMessageCompressionMutator");
        private static readonly int MaxLenght = 2 * (1024 * 1024);
        private static readonly string CompactedKey = "NDDigital.IWasCompressed";
        private static readonly string DiskShareKey = "NDDigital.MessageOnDisk";

        public Task MutateIncoming(MutateIncomingTransportMessageContext context)
        {
            
            if (!context.Headers.ContainsKey(CompactedKey))
                return Task.CompletedTask;

            byte[] buffer = null;
            if (context.Headers.ContainsKey(DiskShareKey))
            {
                string fileName = context.Headers[DiskShareKey];
                buffer = File.ReadAllBytes(fileName);
                logger.InfoFormat("Reading message in disk: {0}", fileName);
            }
            else
                buffer = context.Body;

            using (GZipStream bigStream = new GZipStream(new MemoryStream(buffer), CompressionMode.Decompress))
            {
                MemoryStream bigStreamOut = new MemoryStream();
                bigStream.CopyTo(bigStreamOut);
                context.Body = bigStreamOut.ToArray();
            }
            return Task.CompletedTask;
        }

        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            if (context.OutgoingBody.Length > MaxLenght)
            {
                logger.InfoFormat("transportMessage.Body size before compression: {0}", context.OutgoingBody.Length);
                MemoryStream mStream = new MemoryStream(context.OutgoingBody);
                MemoryStream outStream = new MemoryStream();

                using (GZipStream tinyStream = new GZipStream(outStream, CompressionMode.Compress))
                {
                    mStream.CopyTo(tinyStream);
                }
                context.OutgoingBody = outStream.ToArray();
                context.OutgoingHeaders[CompactedKey] = "true";
                logger.InfoFormat("transportMessage.Body size after compression: {0}", context.OutgoingBody.Length);

                if (context.OutgoingBody.Length > MaxLenght)
                {
                    string filePath = ConfigContext.Data.FileSharePath;
                    if (string.IsNullOrEmpty(filePath) == false)
                    {
                        context.OutgoingBody = null;
                        string fileName = Path.Combine(filePath, Guid.NewGuid().ToString("N")) + ".dat";
                        context.OutgoingHeaders[DiskShareKey] = fileName;
                        File.WriteAllBytes(fileName, outStream.ToArray());
                        logger.InfoFormat("Message saved to disk: {0}", fileName);
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}

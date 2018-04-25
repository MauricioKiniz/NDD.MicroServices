using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NDDigital.Component.Core.Util;
using System.Transactions;
using NServiceBus.Transport.SQLServer;
using System.Reflection;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointTransportCreation : EndpointCreationBase, IEndpointCreation
    {
        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            var cfgData = ConfigContext.Data;
            string transportKindStr = cfgData.TransportKind;
            int transportKind = string.IsNullOrEmpty(transportKindStr) ? 0 : int.Parse(transportKindStr);
            bool enableTransaction = endpoint.EnableTransaction;
            bool enableDistributeTransaction = endpoint.EnableDistributeTransaction;
            int transactionTimeout = endpoint.TransactionTimeout;
            IsolationLevel isoLevel = (IsolationLevel)endpoint.IsolationLevel;

            if (transportKind != 0 && transportKind != 1)
                throw new ApplicationException($"TransportKind: '{transportKind}' not supported");

            UtilHelper.WriteDebug(_logger, "Endpoint {0} - Transport: {1}", (string)endpoint.Id, transportKind == 0 ? "MSMQ" : "Sql Server");

            if (transportKind == 0) // MSMQ
            {
                var transport = cfg.UseTransport<MsmqTransport>();
                if (enableDistributeTransaction)
                    transport.Transactions(TransportTransactionMode.TransactionScope);
                else if (enableTransaction)
                    transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
                else
                    transport.Transactions(TransportTransactionMode.None);
                transport.TransactionScopeOptions(
                    isolationLevel: isoLevel,
                    timeout: TimeSpan.FromSeconds(transactionTimeout));
                transport.SubscriptionAuthorizer(context =>
                {
                    return true;
                });
                UtilHelper.WriteDebug(_logger, "Endpoint {0} - Publishers", (string)endpoint.Id);
                PublisherEvents(transport.Routing<MsmqTransport>(), endpoint);
                UtilHelper.WriteDebug(_logger, "Endpoint {0} - Mappers", (string)endpoint.Id);
                DefineMappersForTransport(transport.Routing());
            }
            else if (transportKind == 1) // sql server
            {
                var transport = cfg.UseTransport<SqlServerTransport>();
                transport.ConnectionString((string)cfgData.QueueDatabaseConnection);
                transport.TimeToWaitBeforeTriggeringCircuitBreaker(TimeSpan.FromMinutes(3));
                transport.DefaultSchema(DefaultSchema);
                if (enableDistributeTransaction)
                    transport.Transactions(TransportTransactionMode.TransactionScope);
                else if (enableTransaction)
                    transport.Transactions(TransportTransactionMode.ReceiveOnly);
                else
                    transport.Transactions(TransportTransactionMode.None);
                transport.TransactionScopeOptions(
                    isolationLevel: isoLevel,
                    timeout: TimeSpan.FromSeconds(transactionTimeout));
                transport.SubscriptionAuthorizer(context =>
                {
                    return true;
                });
                UtilHelper.WriteDebug(_logger, "Endpoint {0} - Publishers", (string)endpoint.Id);
                PublisherEvents(transport.Routing<SqlServerTransport>(), endpoint);
                UtilHelper.WriteDebug(_logger, "Endpoint {0} - Mappers", (string)endpoint.Id);
                DefineMappersForTransport(transport.Routing());
            }
            return cfg;
        }

        private void PublisherEvents(RoutingSettings<MsmqTransport> routing, dynamic endpoint)
        {
            if (IsSendOnly(endpoint))
                return;
            string endpointName = endpoint.Name;
            List<Type> publisherTypes = new List<Type>();
            try
            {
                dynamic process = endpoint.Process;
                dynamic publishers = process.GetElements("Publisher");
                GetAllPublisherTypes(publishers, publisherTypes);
                Type eventType = typeof(IEvent);
                foreach (Type tp in publisherTypes)
                {
                    if (eventType.IsAssignableFrom(tp) == false)
                        continue;
                    routing.RegisterPublisher<MsmqTransport>(tp, endpointName);
                }
            }
            finally
            {
                UtilHelper.ClearList(ref publisherTypes);
            }
        }

        private void PublisherEvents(RoutingSettings<SqlServerTransport> routing, dynamic endpoint)
        {
            if (IsSendOnly(endpoint))
                return;
            string endpointName = endpoint.Name;
            List<Type> publisherTypes = new List<Type>();
            try
            {
                dynamic process = endpoint.Process;
                dynamic publishers = process.GetElements("Publisher");
                GetAllPublisherTypes(publishers, publisherTypes);
                foreach (Type tp in publisherTypes)
                    routing.RegisterPublisher<SqlServerTransport>(tp, endpointName);
            }
            finally
            {
                UtilHelper.ClearList(ref publisherTypes);
            }
        }

        private void GetAllPublisherTypes(dynamic elements, List<Type> elementTypes)
        {
            foreach (var element in elements)
            {
                string assemblyName = element.AssemblyName;
                Assembly contractAssembly = Assembly.Load(assemblyName);
                dynamic typeName = element.GetFirstElement("TypeName");
                if (typeName != null)
                {
                    string name = (string)typeName;
                    Type tp = contractAssembly.GetType(name);
                    elementTypes.Add(tp);
                }
                else
                {
                    dynamic namespaceName = element.GetFirstElement("Namespace");
                    if (namespaceName != null)
                    {
                        string name = (string)namespaceName;
                        var allTypes = contractAssembly.GetTypes();
                        foreach (var tp in allTypes)
                            if (tp.Namespace == name)
                                elementTypes.Add(tp);
                    }
                    else
                        elementTypes.AddRange(contractAssembly.GetTypes());
                }
            }
            UtilHelper.GetAllIEventTypes(elementTypes);
        }

        private void DefineMappersForTransport(RoutingSettings routing)
        {
            var mappers = MiddlewareConfManager.GetMappers();

            foreach (dynamic mapper in mappers)
            {
                string assemblyName = mapper.AssemblyName;
                string nameSpace = mapper.Namespace;
                string typeName = mapper.TypeFullName;
                string messages = mapper.Messages;
                string endpointId = mapper.EndpointId;
                string endpointName = null;
                dynamic endpoint = MiddlewareConfManager.GetEndpoint(endpointId);
                if (endpoint == null)
                {
                    dynamic queueRef = MiddlewareConfManager.GetQueueReferenceById(endpointId);
                    if (queueRef != null)
                        endpointName = queueRef.Name;
                }
                else
                    endpointName = endpoint.Name;
                if (string.IsNullOrEmpty(endpointName))
                    throw new ArgumentException($"Endpoint Name not found to endpointId: '{endpointId}'");
                Assembly messageAssembly = Assembly.Load(assemblyName);

                if (string.IsNullOrEmpty(typeName) == false)
                {
                    Type messType = messageAssembly.GetType(typeName, true);
                    routing.RouteToEndpoint(messageType: messType, destination: endpointName);
                }
                else if (string.IsNullOrEmpty(nameSpace) == false)
                    routing.RouteToEndpoint(assembly: messageAssembly, @namespace: nameSpace, destination: endpointName);
                else
                    routing.RouteToEndpoint(assembly: messageAssembly, destination: endpointName);

            }
        }

    }
}

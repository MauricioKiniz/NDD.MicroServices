using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NDDigital.Component.Core.Util;
using System.Reflection;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointSubscriptionCreation : EndpointCreationBase, IEndpointCreation
    {
        private ServiceBusManager _manager;

        public EndpointSubscriptionCreation(ServiceBusManager manager)
        {
            _manager = manager;
        }

        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            UtilHelper.WriteDebug(_logger, "EndpointInstance Subscribe/Unsubscribe Messages - Start: {0} ", (string)endpoint.Id);
            if (IsSendOnly(endpoint) == false)
                SubscribeMessages(_manager.EndpointInstance, endpoint);
            UtilHelper.WriteDebug(_logger, "EndpointInstance Subscribe/Unsubscribe Messages - End: {0}", (string)endpoint.Id);
            return cfg;
        }

        private void SubscribeMessages(IEndpointInstance endpointInstance, dynamic endpoint)
        {
            dynamic process = endpoint.Process;
            dynamic subscriptions = process.GetElements("Subscription");
            List<Type> subscribeList = new List<Type>();
            List<Type> unsubscribeList = new List<Type>();
            try
            {
                GetAllSubscriptionTypes(subscriptions, subscribeList, unsubscribeList);
                foreach (var element in unsubscribeList)
                    endpointInstance.Unsubscribe(element).Wait();
                foreach (var element in subscribeList)
                {
                    var task = endpointInstance.Subscribe(element);
                    task.Wait();
                }
            }
            finally
            {
                UtilHelper.ClearList(ref subscribeList);
                UtilHelper.ClearList(ref unsubscribeList);
            }
        }

        private void GetAllSubscriptionTypes(dynamic elements, List<Type> subscriptionList, List<Type> unsubscriptionList)
        {
            foreach (var element in elements)
            {
                string assemblyName = element.AssemblyName;
                Assembly contractAssembly = Assembly.Load(assemblyName);
                bool unsubscribe = element.Unsubscribe != null;
                dynamic typeName = element.GetFirstElement("TypeName");
                if (typeName != null)
                {
                    string name = (string)typeName;
                    Type tp = contractAssembly.GetType(name);
                    if (unsubscribe)
                        unsubscriptionList.Add(tp);
                    else
                        subscriptionList.Add(tp);
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
                            {
                                if (unsubscribe)
                                    unsubscriptionList.Add(tp);
                                else
                                    subscriptionList.Add(tp);
                            }
                    }
                    else
                    {
                        var allTypes = contractAssembly.GetTypes();
                        if (unsubscribe)
                            unsubscriptionList.AddRange(allTypes);
                        else
                            subscriptionList.AddRange(allTypes);
                    }
                }
            }
            UtilHelper.GetAllIEventTypes(unsubscriptionList);
            UtilHelper.GetAllIEventTypes(subscriptionList);
        }
    }
}

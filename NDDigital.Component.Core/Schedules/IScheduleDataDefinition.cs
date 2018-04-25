using NServiceBus;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Schedules
{
    public interface IScheduleDataDefinition
    {
        /// <summary>
        /// Retorna uma trigger do Quartz. Esta trigger ao ser disparada irá ser responsável por disparar a mensagem de processamento no middleware,
        /// </summary>
        /// <remarks>
        /// E de responsabilidade de quem implementa este metodo remover a trigger anterior do schedule caso a trigger anterior tenha de ser alterada
        /// devido a alguma mudança de parametrização da solução. Para remover a trigger usar o metodo schedule.UnscheduleJob(triggerkey).
        /// Toda vez que uma trigger for retornada o processo tenta schedular ela no Quartz
        /// </remarks>
        /// <param name="job">Job a ser criado ou já criado no Quartz.</param>
        /// <param name="schedule">Schedule onde o Job e a Trigger estao criados</param>
        /// <param name="endpointName">Nome da fila conforme aparece no arquivo de configuração - middleware.xml</param>
        /// <param name="endpointId">Identificador da fila conforme aparece no arquivo de configuração - middleware.xml</param>
        /// <returns>Uma colecao de triggers do Quartz que devem ser schedulados com o Job ou <code>null</code> caso não se deva schedular pois nenhuma das triggers ja existetes 
        /// sofreram mudanças
        /// </returns>
        ICollection<ITrigger> GetTrigger(IJobDetail job, IScheduler schedule, string endpointName, string endpointId);
        /// <summary>
        /// Responsavel postar ou publicar as mensagens elencadas para o Job e trigger. 
        /// </summary>
        /// <remarks>
        /// E de responsabilidade de quem implementa este método criar caso necessario uma transacao distribuida para garantir processos de
        /// post e publicacao de mensagens com processos de banco de dados 
        /// </remarks>
        /// <param name="endpointInstance">Enpoint do NSB. Send ou Publish de mensagens</param>
        /// <param name="context">Contexto de execução do Quartz</param>
        void SendMessage(IEndpointInstance endpointInstance, IJobExecutionContext context);
    }


    /*
     *  Exemplo de implementação
     *  
     * public class JobTesteSchedule : IScheduleDataDefinition
    {
        private static readonly int Seconds = 120;
        private static readonly string SecondsKey = "Seconds";

        public void SendMessage(IEndpointInstance endpointInstance, IJobExecutionContext context)
        {
        }

        public ICollection<ITrigger> GetTrigger(IJobDetail job, IScheduler schedule, string endpointName, string endpointId)
        {
            var tkey = new TriggerKey("trigger_" + job.Key.Name, job.Key.Group);

            var oldTrigger = schedule.GetTrigger(tkey);

            if (oldTrigger != null)
            {
                int oldSeconds = oldTrigger.JobDataMap.GetInt(SecondsKey);
                if (oldSeconds == Seconds)
                    return null;
                else
                    schedule.UnscheduleJob(tkey);
            }

            var trigger = TriggerBuilder.Create()
                    .WithIdentity(tkey)
                    .StartNow()
                    .WithSimpleSchedule(
                        action: builder =>
                        {
                            builder.WithIntervalInSeconds(Seconds).RepeatForever();
                        })
                    .UsingJobData(SecondsKey, Seconds.ToString())
                    .Build();
            List<ITrigger> list = new List<ITrigger>();
            list.Add(trigger);
            return list;
        }
    }
     */


}

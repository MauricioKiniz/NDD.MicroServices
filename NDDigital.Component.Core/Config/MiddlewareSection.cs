using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Config
{
    public class MiddlewareSection: ConfigurationSection
    {
        [ConfigurationProperty("auditQueue", DefaultValue = "audit", IsRequired = false)]
        public String AuditQueue
        {
            get
            {
                return this["auditQueue"] as String;
            }
             set
            {
                this["auditQueue"] = value;
            }
        }

        [ConfigurationProperty("errorQueue", DefaultValue = "error", IsRequired = false)]
        public string ErrorQueue
        {
            get
            {
                return this["errorQueue"] as string;
            }
            set
            {
                this["errorQueue"] = value;
            }
        }

        [ConfigurationProperty("createQueues", DefaultValue = true, IsRequired = false)]
        public bool CreateQueues
        {
            get
            {
                return (bool)this["createQueues"];
            }
            set
            {
                this["createQueues"] = value;
            }
        }

        [ConfigurationProperty("criticalErrorTimeout", DefaultValue = 60, IsRequired = false)]
        public int CriticalErrorTimeout
        {
            get
            {
                return (int)this["criticalErrorTimeout"];
            }
            set
            {
                this["criticalErrorTimeout"] = value;
            }
        }

        [ConfigurationProperty("logFileName", DefaultValue = "", IsRequired = false)]
        public string LogFileName
        {
            get
            {
                return this["logFileName"] as string;
            }
            set
            {
                this["logFileName"] = value;
            }
        }

        [ConfigurationProperty("middlewareDatabase", DefaultValue = "", IsRequired = false)]
        public string MiddlewareDatabase
        {
            get
            {
                return this["middlewareDatabase"] as string;
            }
            set
            {
                this["middlewareDatabase"] = value;
            }
        }

        [ConfigurationProperty("middlewareFileMapper", DefaultValue = "", IsRequired = true)]
        public string MiddlewareFileMapper
        {
            get
            {
                return this["middlewareFileMapper"] as string;
            }
            set
            {
                this["middlewareFileMapper"] = value;
            }
        }

        [ConfigurationProperty("nHibernateDialect", DefaultValue = "NHibernate.Dialect.MsSql2012Dialect", IsRequired = false)]
        public string NHibernateDialect
        {
            get
            {
                return this["nHibernateDialect"] as string;
            }
            set
            {
                this["nHibernateDialect"] = value;
            }
        }

        [ConfigurationProperty("nServiceBusPersistence", DefaultValue = "", IsRequired = false)]
        public string NServiceBusPersistence
        {
            get
            {
                return this["nServiceBusPersistence"] as string;
            }
            set
            {
                this["nServiceBusPersistence"] = value;
            }
        }

        [ConfigurationProperty("quartzConnectionString", DefaultValue = "", IsRequired = false)]
        public string QuartzConnectionString
        {
            get
            {
                return this["quartzConnectionString"] as string;
            }
            set
            {
                this["quartzConnectionString"] = value;
            }
        }

        [ConfigurationProperty("queueDatabaseConnection", DefaultValue = "", IsRequired = false)]
        public string QueueDatabaseConnection
        {
            get
            {
                return this["queueDatabaseConnection"] as string;
            }
            set
            {
                this["queueDatabaseConnection"] = value;
            }
        }

        [ConfigurationProperty("transportKind", DefaultValue = 0, IsRequired = false)]
        public int TransportKind
        {
            get
            {
                return (int)this["transportKind"];
            }
            set
            {
                this["transportKind"] = value;
            }
        }

        [ConfigurationProperty("enablePerformanceCounters", DefaultValue = false, IsRequired = false)]
        public bool EnablePerformanceCounters
        {
            get
            {
                return (bool)this["enablePerformanceCounters"];
            }
            set
            {
                this["enablePerformanceCounters"] = value;
            }
        }

        [ConfigurationProperty("fileSharePath", DefaultValue = "", IsRequired = false)]
        public string FileSharePath
        {
            get
            {
                return (string)this["fileSharePath"];
            }
            set
            {
                this["fileSharePath"] = value;
            }
        }

        [ConfigurationProperty("endpoints", DefaultValue = "", IsRequired = false)]
        public string Endpoints
        {
            get
            {
                return (string)this["endpoints"];
            }
            set
            {
                this["endpoints"] = value;
            }
        }




    }
}

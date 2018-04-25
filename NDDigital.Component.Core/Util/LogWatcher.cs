using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using NServiceBus.Logging;
using NServiceBus;

namespace NDDigital.Component.Core.Util
{
    public class LogWatcher : IDisposable
    {
        private string _logFile;
        private FileSystemWatcher _logFileWatcher;

        public void Dispose()
        {
            if(_logFileWatcher != null)
                _logFileWatcher.Dispose();
        }
        public bool RegisterLog(string logFileName)
        {
            bool hasLogFile = false;
            if(File.Exists(logFileName))
            {
                _logFile = logFileName;
                Configure();
                _logFileWatcher = new FileSystemWatcher();
                _logFileWatcher.Path = Path.GetDirectoryName(logFileName);
                _logFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                _logFileWatcher.Filter = logFileName;
                _logFileWatcher.Changed += LogFileWatcher_Changed;
                _logFileWatcher.EnableRaisingEvents = true;
                hasLogFile = true;
            }
            else
            {
                PatternLayout layout = new PatternLayout
                {
                    ConversionPattern = "%d | [%t] | %-5p | %c | %m%n"
                };
                layout.ActivateOptions();
                var appender = new RollingFileAppender
                {
                    DatePattern = "yyyy-MM-dd'.txt'",
                    RollingStyle = RollingFileAppender.RollingMode.Composite,
                    MaxFileSize = 10 * 1024 * 1024,
                    MaxSizeRollBackups = 10,
                    LockingModel = new FileAppender.MinimalLock(),
                    StaticLogFileName = false,
                    File = Path.Combine(Environment.CurrentDirectory, "defaultlogfile.txt"),
                    AppendToFile = true,
                    Threshold = Level.Debug,
                    Layout = layout
                };
                appender.ActivateOptions();
                BasicConfigurator.Configure(appender);
            }
            LogManager.Use<Log4NetFactory>();
            return hasLogFile;
        }

        private void LogFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Configure();
        }

        private void Configure()
        {
            using(var fstream = File.OpenRead(_logFile))
            {
                string xmlData = new StreamReader(fstream).ReadToEnd();
                using(MemoryStream mstream = new MemoryStream())
                {
                    var buffer = Encoding.Default.GetBytes(xmlData);
                    mstream.Write(buffer, 0, buffer.Length);
                    mstream.Seek(0, SeekOrigin.Begin);
                    XmlConfigurator.Configure(mstream);
                }
            }
        }

    }
}

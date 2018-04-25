using NDDigital.Component.Core.Util;
using NServiceBus.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Manager
{
    public class MiddlewareConfWatcher
    {
        private string _watchedFile;
        private FileSystemWatcher _fileWatcher;
        private static ILog Logger = LogManager.GetLogger(typeof(MiddlewareConfWatcher));
        private string _hashData;

        public void Dispose()
        {
            if (_fileWatcher != null)
                _fileWatcher.Dispose();
        }
        public void RegisterWatcher(string middlewareFileName)
        {
            if (File.Exists(middlewareFileName))
            {
                _watchedFile = middlewareFileName;
                _fileWatcher = new FileSystemWatcher();
                _fileWatcher.Path = Path.GetDirectoryName(middlewareFileName);
                _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                _fileWatcher.Filter = Path.GetFileName(middlewareFileName);
                _fileWatcher.Changed += LogFileWatcher_Changed;
                _fileWatcher.EnableRaisingEvents = true;
            }
        }

        private void LogFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                UtilHelper.WriteDebug(Logger, "Iniciando recarga do arquivo: {0}", _watchedFile);
                string xmlData = null;
                using (var fstream = File.OpenRead(_watchedFile))
                    xmlData = new StreamReader(fstream).ReadToEnd();
                UtilHelper.WriteError(Logger, "Finalizando recarga do arquivo", _watchedFile);
                string newHash = UtilHelper.CalculateMD5Hash(xmlData);
                if (_hashData == null || _hashData != newHash)
                {
                    _hashData = newHash;
                    List<DeltaChange> changes = new List<DeltaChange>();
                    MiddlewareConfManager.GetDeltaChange(xmlData, changes);
                    if (changes.Count > 0)
                        HostManager.ProcessChanges(changes);
                }
            }
            catch (Exception ex)
            {
                UtilHelper.WriteError(Logger, "Erro na leitura do arquivo: {0}. Erro reportado: {1}", _watchedFile, ex.Message);
            }
        }
    }
}

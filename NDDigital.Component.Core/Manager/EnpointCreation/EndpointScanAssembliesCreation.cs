using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NDDigital.Component.Core.Util;
using System.IO;
using System.Text.RegularExpressions;

namespace NDDigital.Component.Core.Manager.EnpointCreation
{
    public class EndpointScanAssembliesCreation : EndpointCreationBase, IEndpointCreation
    {
        public EndpointConfiguration Create(EndpointConfiguration cfg, dynamic endpoint)
        {
            UtilHelper.WriteDebug(_logger, "Endpoint {0} - Restrict Global Assemblies", (string)endpoint.Id);
            DefineScanAssemblies(cfg); // Global
            UtilHelper.WriteDebug(_logger, "Endpoint {0} - Restrict Endpoint Assemblies", (string)endpoint.Id);
            DefineScanAssemblies(cfg, endpoint); // Endpoint local
            return cfg;
        }

        private void DefineScanAssemblies(EndpointConfiguration cfg, dynamic endpoint = null)
        {
            dynamic scanRoot = null;
            if (endpoint == null)
                scanRoot = MiddlewareConfManager.GetScanAssemblies();
            else
                scanRoot = endpoint.GetFirstElement("ScanAssemblies");
            if (scanRoot != null)
            {
                var scanner = cfg.AssemblyScanner();

                object scanAss = scanRoot.ScanAssembliesInNestedDirectories;
                object scanApp = scanRoot.ScanAppDomainAssemblies;
                object throwEx = scanRoot.ThrowExceptions;

                if (scanAss != null)
                    scanner.ScanAssembliesInNestedDirectories = (bool)scanRoot.ScanAssembliesInNestedDirectories;
                if (scanApp != null)
                    scanner.ScanAppDomainAssemblies = (bool)scanRoot.ScanAppDomainAssemblies;
                if (throwEx != null)
                    scanner.ThrowExceptions = (bool)scanRoot.ThrowExceptions;

                // Exclude Assembly
                List<string> dllNames = new List<string>();
                ScanForExcludeAssemblies(scanRoot, scanner, dllNames);
                ScanForAssembliesRegEx(scanRoot, scanner, dllNames, true);
                ScanForAssembliesRegEx(scanRoot, scanner, dllNames, false);
                if (dllNames.Count > 0)
                {
                    var list = dllNames.FindAll(p => p.StartsWith("nservicebus", StringComparison.CurrentCultureIgnoreCase) == false);
                    scanner.ExcludeAssemblies(list.ToArray());
                }
                UtilHelper.ClearList(ref dllNames);
            }

        }

        private void ScanForAssembliesRegEx(
            dynamic scanRoot,
            AssemblyScannerConfiguration scanner,
            List<string> dllNames,
            bool exclude = true)
        {
            dynamic excludeAssembliesRegex = scanRoot.GetElements((exclude) ? "ExcludeAssemblyRegex" : "IncludeAssemblyRegex");
            List<string> regexList = new List<string>();
            foreach (var ea in excludeAssembliesRegex)
                regexList.Add((string)ea);
            if (regexList.Count > 0)
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                ProcessDirectory(dllNames, exclude, regexList, baseDirectory);
                if (scanner.ScanAssembliesInNestedDirectories)
                {
                    foreach (var directory in Directory.GetDirectories(baseDirectory))
                        ProcessDirectory(dllNames, exclude, regexList, directory);
                }
            }
        }

        private void ProcessDirectory(
            List<string> dllNames,
            bool exclude,
            List<string> regexList,
            string baseDirectory
        )
        {
            foreach (var file in Directory.GetFiles(baseDirectory, "*.dll"))
            {
                bool hasMatch = false;
                string fileName = Path.GetFileName(file);
                foreach (var pattern in regexList)
                {
                    if (exclude)
                    {
                        if (Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase))
                        {
                            dllNames.Add(fileName);
                            break;
                        }
                    }
                    else
                    {
                        if (Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase))
                        {
                            hasMatch = true;
                            break;
                        }
                    }
                }
                if (exclude == false && hasMatch == false)
                    dllNames.Add(fileName);
            }
        }

        private List<string> ScanForExcludeAssemblies(dynamic scanRoot, AssemblyScannerConfiguration scanner, List<string> dllNames)
        {
            dynamic excludeAssemblies = scanRoot.GetElements("ExcludeAssembly");
            foreach (var ea in excludeAssemblies)
                dllNames.Add((string)ea);
            return dllNames;
        }

    }
}

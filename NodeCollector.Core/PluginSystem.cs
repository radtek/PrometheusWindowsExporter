using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using NodeExporterCore;
using System.Diagnostics;

namespace NodeCollector.Core
{
    public class PluginSystem
    {
        public static PluginCollection LoadCollectors()
        {
            PluginCollection loadedPlugins = new PluginCollection();

            string pluginFilter = "NodeCollector.*.dll";
            string dllDirectory = AppDomain.CurrentDomain.BaseDirectory;
            GVars.MyLog.WriteEntry(string.Format("Loading available collectors from directory {0} (filter {1})", dllDirectory, pluginFilter));

            // Search for all filtes matching the filter and try to load them                
            string[] pluginFiles = Directory.GetFiles(dllDirectory, pluginFilter);
            foreach (string filename in pluginFiles)
            {
                try {
                    NodeCollector.Core.INodeCollector plugin = PluginSystem.LoadAssembly(filename);
                    loadedPlugins.Add(plugin);
                }
                catch(Exception ex)
                {
                    GVars.MyLog.WriteEntry(string.Format("Unable to load collector plugin {0}: {1}.", filename, ex.Message.ToString()), EventLogEntryType.Error, 0);
                }
            }

            // Log a list of loaded plugins to eventlog
            List<string> pluginNames = loadedPlugins.Select(x => x.GetName()).ToList();
            pluginNames.Sort();
            GVars.MyLog.WriteEntry(string.Format("Loaded plugins: {0}", string.Join(", ", pluginNames)));

            return loadedPlugins;
        }

        private static NodeCollector.Core.INodeCollector LoadAssembly(string assemblyPath)
        {
            string assembly = Path.GetFullPath(assemblyPath);
            Assembly ptrAssembly = Assembly.LoadFile(assembly);
            foreach (Type item in ptrAssembly.GetTypes())
            {
                if (!item.IsClass) continue;
                if (item.GetInterfaces().Contains(typeof(NodeCollector.Core.INodeCollector)))
                {
                    return (NodeCollector.Core.INodeCollector)Activator.CreateInstance(item);
                }
            }
            throw new Exception("Invalid DLL, Interface not found!");
        }

    }

    public class PluginCollection : List<NodeCollector.Core.INodeCollector>
    {
    }

}

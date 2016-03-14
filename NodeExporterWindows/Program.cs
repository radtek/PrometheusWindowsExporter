using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Prometheus;
using NodeCollector;

namespace NodeExporterWindows
{
    class Program
    {
        static void Main(string[] args)
        {
            string dllDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] pluginFiles = Directory.GetFiles(dllDirectory, "NodeCollector.*.dll");

            List<NodeCollector.Core.INodeCollector> runningPlugins = new List<NodeCollector.Core.INodeCollector>();
            foreach (string filename in pluginFiles)
            {
                NodeCollector.Core.INodeCollector plugin = Program.LoadAssembly(filename);
                plugin.RegisterMetrics();
                runningPlugins.Add(plugin);
            }

            // netsh http add urlacl url="http://+:9100/" user=everyone OR user=domain\xx
            MetricServer metricServer;
            while (true)
            {
                try
                {
                    metricServer = new MetricServer(port: 9100);
                    metricServer.Start();
                    break;
                }
                catch
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    continue;
                }
            }


            /*Console.WriteLine("Sleeping...");
            Thread.Sleep(TimeSpan.FromSeconds(15));
            foreach(NodeCollector.Core.INodeCollector plugin in runningPlugins)
            {
                plugin.Shutdown();
            }
        */
            //metricServer.Stop();

            Console.WriteLine("Waiting...");
            Console.ReadLine();
            Console.WriteLine("Exiting...");
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
}

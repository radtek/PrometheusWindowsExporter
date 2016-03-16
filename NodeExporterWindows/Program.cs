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
using NodeExporterCore;
using NodeExporterWindows.Properties;
using System.Diagnostics;

namespace NodeExporterWindows
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize the global logging, usually this should go to the regular Windows eventlog
            GVars.MyLog = new System.Diagnostics.EventLog("Application");
            GVars.MyLog.Source = "PrometheusNodeExporter";

            // Load all plugins an initialize the metrics
            NodeCollector.Core.PluginCollection availablePlugins = NodeCollector.Core.PluginSystem.LoadCollectors();
            foreach(NodeCollector.Core.INodeCollector collector in availablePlugins)
            {
                collector.RegisterMetrics();
            }

            ushort port = NodeExporterWindows.Properties.Settings.Default.Port;
            string metricUrl = "metrics/";
            GVars.MyLog.WriteEntry(string.Format("Start Prometheus exporter service on port :{0}/tcp (url {1}).", port, metricUrl),
                EventLogEntryType.Information, 0);
            MetricServer metricServer = new MetricServer(port: port, url: metricUrl);
            metricServer.Start();

            Console.WriteLine("Press [ENTER] twice to exit...");
            Console.ReadLine();
            Console.ReadLine();
        }

    }
}

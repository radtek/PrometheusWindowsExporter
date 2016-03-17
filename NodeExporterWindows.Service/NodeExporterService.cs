using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using NodeCollector;
using Prometheus;
using NodeExporterCore;
using NodeExporterWindows.Service.Properties;

// Docu: http://blog.nager.at/2012/01/visual-studio-2010-windows-service-erstellen/
// #Install
// InstallUtil.exe "C:\Projects\WindowsService1\bin\Debug\WindowsService1.exe"
// #Uninstall
// InstallUtil.exe /u "C:\Projects\WindowsService1\bin\Debug\WindowsService1.exe"


namespace NodeExporterWindows.Service
{
    public partial class NodeExporterService : ServiceBase
    {
        public NodeExporterService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Initialize the global logging, usually this should go to the regular Windows eventlog
            GVars.MyLog = this.eventLogMaster;

            // Load all plugins an initialize the metrics
            NodeCollector.Core.PluginCollection availablePlugins = NodeCollector.Core.PluginSystem.LoadCollectors();
            foreach (NodeCollector.Core.INodeCollector collector in availablePlugins)
            {
                collector.RegisterMetrics();
            }

            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            ushort port = NodeExporterWindows.Service.Properties.Settings.Default.Port;
            string metricUrl = "metrics/";
            GVars.MyLog.WriteEntry(string.Format("Start Prometheus exporter v{0} service on port :{1}/tcp (url {2}).", version, port, metricUrl),
                EventLogEntryType.Information, 0);
            MetricServer metricServer = new MetricServer(port: port, url: metricUrl);
            metricServer.Start();
        }

        protected override void OnStop()
        {
            GVars.MyLog.WriteEntry("Shutting down Prometheus exporter.", EventLogEntryType.Information, 0);
        }

    }
}

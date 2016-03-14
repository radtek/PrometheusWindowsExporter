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
using PrometheusCollector;
using Prometheus;

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
            string dllDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] pluginFiles = Directory.GetFiles(dllDirectory, "PrometheusCollector.*.dll");

            foreach (string filename in pluginFiles)
            {
                IPrometheusCollector plugin = this.LoadAssembly(filename);
                plugin.RegisterMetrics();
            }

            MetricServer metricServer = new MetricServer(port: 9100);
            metricServer.Start();
        }

        protected override void OnStop()
        {
        }

        private IPrometheusCollector LoadAssembly(string assemblyPath)
        {
            string assembly = Path.GetFullPath(assemblyPath);
            Assembly ptrAssembly = Assembly.LoadFile(assembly);
            foreach (Type item in ptrAssembly.GetTypes())
            {
                if (!item.IsClass) continue;
                if (item.GetInterfaces().Contains(typeof(IPrometheusCollector)))
                {
                    return (IPrometheusCollector)Activator.CreateInstance(item);
                }
            }
            throw new Exception("Invalid DLL, Interface not found!");
        }

    }
}

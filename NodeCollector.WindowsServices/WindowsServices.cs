using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
using Prometheus;
using NodeCollector;
using NodeCollector.WindowsServices.Properties;
using NodeExporterCore;
using System.Management;

namespace NodeCollector.WindowsServices
{

    public class CounterEntry
    {
        public System.Diagnostics.PerformanceCounter PerfCounter { get; set; }

        public object PrometheusCollector { get; set; }
    }

    public class WindowsServices : NodeCollector.Core.INodeCollector
    {
        private System.Threading.Timer MetricUpdateTimer;
        private Prometheus.Gauge WindowsServiceGauge;

        public WindowsServices()
        {
            this.WindowsServiceGauge = Metrics.CreateGauge("windows_service_status", "Current state if Windows services.", labelNames: new[] { "name", "caption", "startmode"});
        }

        public string GetName()
        {
            return "WindowsServices";
        }

        public string GetVersion()
        {
            string version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
            return version;
        }

        public void RegisterMetrics()
        {
            // Load search interval from properties.
            TimeSpan rumpTime = TimeSpan.FromSeconds(NodeCollector.WindowsServices.Properties.Settings.Default.SearchRumpTime);
            TimeSpan searchInterval = TimeSpan.FromSeconds(NodeCollector.WindowsServices.Properties.Settings.Default.SearchInvervalSeconds);
            GVars.MyLog.WriteEntry(string.Format("Initializing WindowsServices collector v{0} (Search interval is {1}s, Rump time is {2}s).", 
                this.GetVersion(), searchInterval.TotalSeconds, rumpTime.TotalSeconds), EventLogEntryType.Information, 1000);

            // Initialize a timer to search all XX minutes for new updates.
            // The process is very time intersive, so please do not lower this value below one hour.
            this.MetricUpdateTimer = new System.Threading.Timer(this.UpdateMetrics,
                this,
                Convert.ToInt32(rumpTime.TotalMilliseconds),
                Convert.ToInt32(searchInterval.TotalMilliseconds)
                );

        }

        public void Shutdown()
        {
            GVars.MyLog.WriteEntry("Shutting down WindowsServices collector.", EventLogEntryType.Warning, 1000);
            this.MetricUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void UpdateMetrics(object state)
        {
            Debug.WriteLine(string.Format("NodeCollector.WindowsServices::UpdateMetrics(): Reading perfmon counters ({0}).", DateTime.Now.ToString()));

            // Collect details about hard disks
            ManagementObjectCollection results;
            try
            {
                SelectQuery selectQuery = new SelectQuery(@"SELECT * FROM Win32_Service");
                ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(selectQuery);
                results = managementObjectSearcher.Get();
            }
            catch(Exception ex)
            {
                GVars.MyLog.WriteEntry(string.Format("WindowsServices: Failed to query WMI for service: {0}.", ex.Message.ToString(), EventLogEntryType.Error, 1000));
                return;
            }
            
            foreach (ManagementObject managementObject in results)
            {
                string serviceName = string.Empty;
                try
                {
                    serviceName = managementObject["Name"].ToString().Replace(@"\", "/");
                    if(serviceName.Length < 1)
                    {
                        serviceName = "n/a";
                    }

                    string caption = this.GetMOBValueString(managementObject, "Caption");
                    string startMode = this.GetMOBValueString(managementObject, "StartMode");
                    string stateString = this.GetMOBValueString(managementObject, "State");

                    int stateVal = 0;
                    if(stateString == "Running")
                    {
                        stateVal = 1;
                    }

                    this.WindowsServiceGauge.Labels(serviceName, caption, startMode).Set(stateVal);
                }
                catch(Exception ex)
                {
                    GVars.MyLog.WriteEntry(string.Format("WindowsServices: Unable to add service to metrics: {0}: {1}", serviceName, ex.Message.ToString()), EventLogEntryType.Error, 1000);
                }
            }

        }

        private string GetMOBValueString(ManagementObject obj, string name, string default_value="")
        {
            string tmp;
            try
            {
                tmp = obj[name].ToString();
            }
            catch
            {
                tmp = default_value;
            }
            return tmp;
        }

        private double GetMOBValueDouble(ManagementObject obj, string name, double default_value=0)
        {
            double tmp;
            try
            {
                tmp = Convert.ToDouble(obj[name]);
            }
            catch
            {
                tmp = default_value;
            }
            return tmp;
        }


    }
}

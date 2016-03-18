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
using NodeCollector.WindowsDisks.Properties;
using NodeExporterCore;
using System.Management;

namespace NodeCollector.WindowsDisks
{

    public class CounterEntry
    {
        public System.Diagnostics.PerformanceCounter PerfCounter { get; set; }

        public object PrometheusCollector { get; set; }
    }

    public class WindowsDisks : NodeCollector.Core.INodeCollector
    {
        private System.Threading.Timer MetricUpdateTimer;
        private Prometheus.Gauge NodeFileSystemSize;
        private Prometheus.Gauge NodeFileSystemFree;

        public WindowsDisks()
        {
            this.NodeFileSystemSize = Metrics.CreateGauge("node_filesystem_size", "Filesystem size in bytes.", labelNames: new[] { "device", "fstype", "mountpoint" });
            this.NodeFileSystemFree = Metrics.CreateGauge("node_filesystem_free", "Filesystem free space in bytes.", labelNames: new[] { "device", "fstype", "mountpoint" });
        }

        public string GetName()
        {
            return "WindowsDisks";
        }

        public string GetVersion()
        {
            string version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
            return version;
        }

        public void RegisterMetrics()
        {
            // Load search interval from properties.
            TimeSpan rumpTime = TimeSpan.FromSeconds(NodeCollector.WindowsDisks.Properties.Settings.Default.SearchRumpTime);
            TimeSpan searchInterval = TimeSpan.FromSeconds(NodeCollector.WindowsDisks.Properties.Settings.Default.SearchInvervalSeconds);
            GVars.MyLog.WriteEntry(string.Format("Initializing WindowsDisks collector v{0} (Search interval is {1}s, Rump time is {2}s).", 
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
            GVars.MyLog.WriteEntry("Shutting down WindowsDisks collector.", EventLogEntryType.Warning, 1000);
            this.MetricUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void UpdateMetrics(object state)
        {
            Debug.WriteLine(string.Format("NodeCollector.WindowsDisks::UpdateMetrics(): Reading perfmon counters ({0}).", DateTime.Now.ToString()));

            // Collect details about hard disks
            SelectQuery selectQuery = new SelectQuery(@"SELECT * FROM Win32_Volume WHERE DriveType!='5'");
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(selectQuery);
            ManagementObjectCollection results = managementObjectSearcher.Get();
            foreach (ManagementObject managementObject in results)
            {
                string device = "n/a";
                try
                {
                    device = managementObject["DeviceID"].ToString().Replace(@"\", "/");

                    string fstype;
                    try
                    {
                        fstype = managementObject["FileSystem"].ToString().Replace(@"\", "/");
                    }
                    catch
                    {
                        fstype = "";
                    }

                    string mountpoint;
                    try
                    {
                        mountpoint = managementObject["DriveLetter"].ToString().Replace(@"\", "/");
                    }
                    catch
                    {
                        mountpoint = "";
                    }

                    double capacityBytes = Convert.ToDouble(managementObject["Capacity"]);
                    double freeBytes = Convert.ToDouble(managementObject["FreeSpace"]);
                    this.NodeFileSystemSize.Labels(device, fstype, mountpoint).Set(capacityBytes);
                    this.NodeFileSystemFree.Labels(device, fstype, mountpoint).Set(freeBytes);
                }
                catch(Exception ex)
                {
                    GVars.MyLog.WriteEntry(string.Format("Unable to add volume to metrics: {0}: {1}", device, ex.Message.ToString()), EventLogEntryType.Error, 0);
                }
            }

        }

    }
}

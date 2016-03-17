using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prometheus;
using NodeCollector;
using System.Threading;
using WUApiLib;
using System.Diagnostics;
using System.Timers;
using NodeCollector.WindowsUpdates.Properties;
using NodeExporterCore;

namespace NodeCollector.WindowsUpdates
{
    public class WindowsUpdates : NodeCollector.Core.INodeCollector
    {
        private System.Threading.Timer MetricUpdateTimer;
        private Prometheus.Gauge WindowsUpdateMissingUpdatesTotal;
        private Prometheus.Gauge WindowsUpdateLastScanTime;
        private Prometheus.Summary WindowsUpdateScanDuration;

        public WindowsUpdates()
        {
            this.WindowsUpdateMissingUpdatesTotal = Metrics.CreateGauge("windows_updates_missing_total", "Number of missing Windows patches");
            this.WindowsUpdateMissingUpdatesTotal.Set(0); // maybe this is the default

            this.WindowsUpdateLastScanTime = Metrics.CreateGauge("windows_updates_last_scan_time", "Last search time, in unixtime.");
            this.WindowsUpdateLastScanTime.Set(0);

            this.WindowsUpdateScanDuration = Metrics.CreateSummary("windows_updates_last_scan_duration_milliseconds", "Last scan time, milliseconds.");
        }

        public string GetName()
        {
            return "WindowsUpdates";
        }

        public string GetVersion()
        {
            string version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
            return version;
        }

        public void RegisterMetrics()
        {
            // Load search interval from properties.
            TimeSpan randomOffset = (NodeCollector.WindowsUpdates.Properties.Settings.Default.UseRandomOffset == true) ? TimeSpan.FromSeconds(new Random().Next(120, 5*60)) : TimeSpan.FromSeconds(0);
            TimeSpan rumpTime = TimeSpan.FromSeconds(NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchRumpTime);
            TimeSpan searchInterval = TimeSpan.FromSeconds(NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchInvervalSeconds) + randomOffset;
            GVars.MyLog.WriteEntry(string.Format("Initializing WindowsUpdates collector v{0} (Search interval is {1}s, Rump time is {2}s).", 
                this.GetVersion(), searchInterval.TotalSeconds, rumpTime.TotalSeconds), EventLogEntryType.Information, 1000);

            // Initialize a timer to search all XX minutes for new updates.
            // The process is very time intersive, so please do not lower this value below one hour.
            this.MetricUpdateTimer = new System.Threading.Timer(this.SearchForUpdates, 
                this, 
                Convert.ToInt32(rumpTime.TotalMilliseconds), 
                Convert.ToInt32(searchInterval.TotalMilliseconds)
                );

            return;
        }

        public void Shutdown()
        {
            GVars.MyLog.WriteEntry("Shutting down WindowsUpdates collector.", EventLogEntryType.Warning, 1000);
            this.MetricUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SearchForUpdates(object state)
        {
            bool searchOnline = NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchOnline;

            GVars.MyLog.WriteEntry(string.Format("Start searching for new Windows updates (Search online: {0})", searchOnline.ToString().ToLower()), EventLogEntryType.Information, 1000);
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                UpdateSession updSession = new UpdateSession();
                IUpdateSearcher updSearcher = updSession.CreateUpdateSearcher();
                updSearcher.Online = searchOnline;

                ISearchResult searchResult = updSearcher.Search(NodeCollector.WindowsUpdates.Properties.Settings.Default.WindowsUpdateSearchFilter);
                if (searchResult.Updates.Count > 0)
                {
                    GVars.MyLog.WriteEntry(string.Format("Found {0} missing Windows updates. Updating metric.", searchResult.Updates.Count), EventLogEntryType.Information, 1000);
                }
                else
                {
                    GVars.MyLog.WriteEntry("No missing Windows updates found.", EventLogEntryType.Information, 1000);
                }
                this.WindowsUpdateMissingUpdatesTotal.Set(searchResult.Updates.Count);

                /*
                foreach (IUpdate update in searchResult.Updates)
                { }
                */

                // Set the last scan time to current timestamp
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                this.WindowsUpdateLastScanTime.Set(unixTimestamp);

                // How long goes this scan take?
                this.WindowsUpdateScanDuration.Observe(sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                GVars.MyLog.WriteEntry(string.Format("Failed to query for missing Windows updates: {0}", ex.Message.ToString()), EventLogEntryType.Error, 1000);
            }

            return;
        }

    }
}

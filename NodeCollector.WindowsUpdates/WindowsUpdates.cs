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
        private Prometheus.Gauge WindowsUpdateGauge;

        public WindowsUpdates()
        {
            this.WindowsUpdateGauge = null; // do not initialize the matric as loon as we don't have a valid result
        }

        public string GetName()
        {
            return "WindowsUpdates";
        }

        public void RegisterMetrics()
        {
            // Load search interval from properties.
            TimeSpan rumpTime = TimeSpan.FromSeconds(NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchRumpTime);
            TimeSpan searchInterval = TimeSpan.FromSeconds(NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchInvervalSeconds);
            GVars.MyLog.WriteEntry(string.Format("Initializing WindowUpdates collector (Search interval is {0}s, Rump time is {1}s).", searchInterval.TotalSeconds, rumpTime.TotalSeconds), EventLogEntryType.Information, 1000);

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
            GVars.MyLog.WriteEntry("Shutting down WindowUpdates collector.", EventLogEntryType.Warning, 1000);
            this.MetricUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SearchForUpdates(object state)
        {
            GVars.MyLog.WriteEntry(string.Format("Start searching for new Windows updates."), EventLogEntryType.Information, 1000);
            Stopwatch sw = new Stopwatch();

            try
            {

                if (this.WindowsUpdateGauge == null)
                {
                    this.WindowsUpdateGauge = Metrics.CreateGauge("windows_updates_pending_total", "Number of pending Windows patches");
                }

                UpdateSession updSession = new UpdateSession();
                IUpdateSearcher updSearcher = updSession.CreateUpdateSearcher();
                updSearcher.Online = NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchOnline;

                ISearchResult searchResult = updSearcher.Search(NodeCollector.WindowsUpdates.Properties.Settings.Default.WindowsUpdateSearchFilter);
                if (searchResult.Updates.Count > 0)
                {
                    GVars.MyLog.WriteEntry(string.Format("Found {0} missing Windows updates. Updating metric.", searchResult.Updates.Count), EventLogEntryType.Information, 1000);
                }
                else
                {
                    GVars.MyLog.WriteEntry("No missing Windows updates found.", EventLogEntryType.Information, 1000);
                }
                this.WindowsUpdateGauge.Set(searchResult.Updates.Count);

                /*
                foreach (IUpdate update in searchResult.Updates)
                { }
                */

            }
            catch (Exception ex)
            {
                GVars.MyLog.WriteEntry(string.Format("Failed to query for missing Windows updates: {0}", ex.Message.ToString()), EventLogEntryType.Error, 1000);
            }

            return;
        }

    }
}

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
       
        public void RegisterMetrics()
        {
            // Load search interval from properties.
            TimeSpan rumpTime = TimeSpan.FromSeconds(NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchRumpTime);
            TimeSpan searchInterval = TimeSpan.FromSeconds(NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchInvervalSeconds);
            Debug.WriteLine("WindowsUpdates::RegisterMetrics(): Initializing metrics for Windows Updates. Searching all {0} seconds (starting in {1} s).", searchInterval.TotalSeconds, rumpTime.TotalSeconds);

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
            Debug.WriteLine(string.Format("WindowsUpdates::Shutdown(): Stopping timer."));
            this.MetricUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SearchForUpdates(object state)
        {
            Debug.WriteLine(string.Format("NodeCollector.WindowsUpdates::SearchForUpdates(): Searching for new updates ({0}).", DateTime.Now.ToString()));

            this.WindowsUpdateGauge = Metrics.CreateGauge("windows_updates_pending_total", "Number of pending Windows patches");

            UpdateSession updSession = new UpdateSession();
            IUpdateSearcher updSearcher = updSession.CreateUpdateSearcher();
            updSearcher.Online = NodeCollector.WindowsUpdates.Properties.Settings.Default.SearchOnline;
            try
            {
                ISearchResult searchResult = updSearcher.Search(NodeCollector.WindowsUpdates.Properties.Settings.Default.WindowsUpdateSearchFilter);
                this.WindowsUpdateGauge.Set(searchResult.Updates.Count);
                /*
                foreach (IUpdate update in searchResult.Updates)
                { }
                */
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("NodeCollector.WindowsUpdates::SearchForUpdates(): Searching failed: {0}.", ex.Message.ToString()));
                return;
            }

            Debug.WriteLine(string.Format("NodeCollector.WindowsUpdates::SearchForUpdates(): Searching finished ({0}).", DateTime.Now.ToString()));
        }

    }
}

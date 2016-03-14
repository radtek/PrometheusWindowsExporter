using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prometheus;
using NodeCollector;

namespace NodeCollector.WindowsUpdates
{
    public class WindowsUpdates : NodeCollector.Core.INodeCollector
    {
        public void RegisterMetrics()
        {
            Prometheus.Gauge windowsUpdateGauge = Metrics.CreateGauge("windows_updates_pending_total", "Number of pending Windows patches", labelNames: new[] { "name" });
            windowsUpdateGauge.Set(10);
            //CounterEntry entry = new CounterEntry() { PerfCounter = tmpPerfCounter, PrometheusCollector = tmpPrometheusCounter };
            //this.RegisteredCounts.Add(entry);
        }

    }
}

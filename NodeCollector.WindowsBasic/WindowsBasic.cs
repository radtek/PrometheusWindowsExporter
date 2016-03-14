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
using NodeCollector.WindowsBasic.Properties;

namespace NodeCollector.WindowsBasic
{

    public class CounterEntry
    {
        public System.Diagnostics.PerformanceCounter PerfCounter { get; set; }

        public object PrometheusCollector { get; set; }
    }

    public class WindowsCore : NodeCollector.Core.INodeCollector
    {
        private System.Threading.Timer MetricUpdateTimer;
        private List<CounterEntry> RegisteredCounts;

        public WindowsCore()
        {
            this.RegisteredCounts = new List<CounterEntry>();
        }

        public void RegisterMetrics()
        {
            /*
             * CPU
             */
            string categoryName = "Processor";
            PerformanceCounterCategory perfCategory = new PerformanceCounterCategory(categoryName);
            string[] instanceNames = perfCategory.GetInstanceNames().Where(name => name != "_Total").ToArray();
            foreach (string instanceName in instanceNames)
            {
                System.Diagnostics.PerformanceCounter tmpPerfCounter = new System.Diagnostics.PerformanceCounter();
                tmpPerfCounter.CategoryName = categoryName;
                tmpPerfCounter.CounterName = "% Processor Time";
                tmpPerfCounter.InstanceName = instanceName;
                Prometheus.Gauge tmpPrometheusCounter = Metrics.CreateGauge("perfmon_processor_processortime_percent", "help text", labelNames: new[] { "name" });
                CounterEntry entry = new CounterEntry() { PerfCounter = tmpPerfCounter, PrometheusCollector = tmpPrometheusCounter };
                this.RegisteredCounts.Add(entry);
            }

            this.RegisterPrintSpooler();

            // Load search interval from properties.
            TimeSpan rumpTime = TimeSpan.FromSeconds(NodeCollector.WindowsBasic.Properties.Settings.Default.SearchRumpTime);
            TimeSpan searchInterval = TimeSpan.FromSeconds(NodeCollector.WindowsBasic.Properties.Settings.Default.SearchInvervalSeconds);
            Debug.WriteLine("WindowsCore::RegisterMetrics(): Initializing metrics for Windows Code. Searching all {0} seconds (starting in {1} s).", searchInterval.TotalSeconds, rumpTime.TotalSeconds);

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
            Debug.WriteLine(string.Format("WindowsBasic::Shutdown(): Stopping timer."));
            this.MetricUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void UpdateMetrics(object state)
        {
            Debug.WriteLine(string.Format("NodeCollector.WindowsBasic::UpdateMetrics(): Reading perfmon counters ({0}).", DateTime.Now.ToString()));

            foreach (CounterEntry entry in this.RegisteredCounts)
            {
                long rawValue = entry.PerfCounter.RawValue;
                float nextValue = entry.PerfCounter.NextValue();
                //Debug.WriteLine(String.Format(@"<PerfCounter> {0}\{1}\{2}: {3}", entry.PerfCounter.CategoryName, entry.PerfCounter.CounterName, entry.PerfCounter.InstanceName, nextValue));
                Prometheus.Gauge g = (Prometheus.Gauge)entry.PrometheusCollector;
                g.Labels(entry.PerfCounter.InstanceName).Set(nextValue);
            }

        }

        private void RegisterPrintSpooler()
        {
            string categoryName = "Print Queue";
            PerformanceCounterCategory perfCategory = new PerformanceCounterCategory(categoryName);
            string[] instanceNames = perfCategory.GetInstanceNames().Where(name => name != "_Total").ToArray();
            foreach (string instanceName in instanceNames)
            {
                foreach (string counterName in new string[] { "Total Jobs Printed", "Jobs", "Job Errors" })
                {
                    System.Diagnostics.PerformanceCounter tmpPerfCounter = new System.Diagnostics.PerformanceCounter();
                    tmpPerfCounter.CategoryName = categoryName;
                    tmpPerfCounter.CounterName = counterName;
                    tmpPerfCounter.InstanceName = instanceName;

                    string metricName = string.Format("perfmon_{0}_{1}_total", categoryName.Replace(" ", "_").ToLower().Trim(), counterName.Replace(" ", "_").ToLower().Trim());
                    Prometheus.Gauge tmpPrometheusCounter = Metrics.CreateGauge(metricName, "help text", labelNames: new[] { "name" });

                    CounterEntry entry = new CounterEntry() { PerfCounter = tmpPerfCounter, PrometheusCollector = tmpPrometheusCounter };
                    this.RegisteredCounts.Add(entry);
                }

            }

        }
    }
}

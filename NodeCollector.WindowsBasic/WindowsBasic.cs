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

namespace NodeCollector.WindowsBasic
{

    public class CounterEntry
    {
        public System.Diagnostics.PerformanceCounter PerfCounter { get; set; }

        public object PrometheusCollector { get; set; }
    }

    public class WindowsCore : NodeCollector.Core.INodeCollector
    {
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

            System.Timers.Timer metricUpdateTimer = new System.Timers.Timer(1000);
            metricUpdateTimer.Elapsed += UpdateMetrics;
            metricUpdateTimer.Enabled = true;
            metricUpdateTimer.Start();

            /*
            while(true)
            {
                if (!metricUpdateTimer.Enabled)
                {
                    metricUpdateTimer.Enabled = true;
                }
                Thread.Sleep(10); // probing every 10 seconds for timer
            }
            Debug.WriteLine("xx");
            */
        }

        private void UpdateMetrics(object sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("Update metics...");
            //return;
            foreach (CounterEntry entry in this.RegisteredCounts)
            {
                long rawValue = entry.PerfCounter.RawValue;
                float nextValue = entry.PerfCounter.NextValue();
                Debug.WriteLine(String.Format(@"<PerfCounter> {0}\{1}\{2}: {3}", entry.PerfCounter.CategoryName, entry.PerfCounter.CounterName, entry.PerfCounter.InstanceName, nextValue));
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

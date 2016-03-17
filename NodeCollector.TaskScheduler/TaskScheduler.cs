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
using NodeCollector.TaskScheduler.Properties;
using NodeExporterCore;
using Microsoft.Win32.TaskScheduler;

namespace NodeCollector.TaskScheduler
{

    public class CounterEntry
    {
        public System.Diagnostics.PerformanceCounter PerfCounter { get; set; }

        public object PrometheusCollector { get; set; }
    }

    public class TaskScheduler : NodeCollector.Core.INodeCollector
    {
        private System.Threading.Timer MetricUpdateTimer;
        private Prometheus.Gauge TaskLastResultGauge;
        private Prometheus.Gauge TaskLastMissedGauge;
        private Prometheus.Gauge TaskLastRuntimeGauge;
        private Prometheus.Gauge TaskLastSuccessRuntimeGauge;

        public TaskScheduler()
        {
            this.TaskLastResultGauge = Metrics.CreateGauge("taskscheduler_task_result", "Return code from task scheduler.", labelNames: new[] { "taskname", "folder", "state" });
            this.TaskLastMissedGauge = Metrics.CreateGauge("taskscheduler_task_missedruns", "Execution time of the task.", labelNames: new[] { "taskname", "folder", "state" });
            this.TaskLastRuntimeGauge = Metrics.CreateGauge("taskscheduler_task_last_runtime", "Execution time of the task.", labelNames: new[] { "taskname", "folder", "state" });
            this.TaskLastSuccessRuntimeGauge = Metrics.CreateGauge("taskscheduler_task_last_success_runtime", "Last successfull execution of the task.", labelNames: new[] { "taskname", "folder", "state" });
        }

        public string GetName()
        {
            return "TaskScheduler";
        }

        public string GetVersion()
        {
            string version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
            return version;
        }

        public void RegisterMetrics()
        {
            // Load search interval from properties.
            TimeSpan rumpTime = TimeSpan.FromSeconds(NodeCollector.TaskScheduler.Properties.Settings.Default.SearchRumpTime);
            TimeSpan searchInterval = TimeSpan.FromSeconds(NodeCollector.TaskScheduler.Properties.Settings.Default.SearchInvervalSeconds);
            GVars.MyLog.WriteEntry(string.Format("Initializing TaskScheduler collector v{0} (Search interval is {1}s, Rump time is {2}s).", 
                this.GetVersion(), searchInterval.TotalSeconds, rumpTime.TotalSeconds), EventLogEntryType.Information, 1000);

            // Initialize a timer to search all XX minutes for tasks.
            // The process is very time intersive, so please do not lower this value below one hour.
            this.MetricUpdateTimer = new System.Threading.Timer(this.UpdateMetrics,
                this,
                Convert.ToInt32(rumpTime.TotalMilliseconds),
                Convert.ToInt32(searchInterval.TotalMilliseconds)
                );

        }

        public void Shutdown()
        {
            GVars.MyLog.WriteEntry("Shutting down TaskScheduler collector.", EventLogEntryType.Warning, 1000);
            this.MetricUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void UpdateMetrics(object state)
        {
            Debug.WriteLine(string.Format("NodeCollector.TaskScheduler::UpdateMetrics(): Reading entries from TaskScheduler library counters ({0}).", DateTime.Now.ToString()));

            using (Microsoft.Win32.TaskScheduler.TaskService taskService = new Microsoft.Win32.TaskScheduler.TaskService())
            {
                List<Microsoft.Win32.TaskScheduler.Task> tasks = taskService.AllTasks.ToList();
                foreach(Microsoft.Win32.TaskScheduler.Task t in tasks)
                {
                    string lastState = ConvertTaskStateToString(t.State);
                    string folderName = t.Folder.Path.Replace(@"\", "/");

                    this.TaskLastResultGauge.Labels(t.Name, folderName, lastState).Set(t.LastTaskResult);

                    this.TaskLastMissedGauge.Labels(t.Name, folderName, lastState).Set(t.NumberOfMissedRuns);

                    Int32 unixTimestamp = (Int32)(t.LastRunTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    this.TaskLastRuntimeGauge.Labels(t.Name, folderName, lastState).Set(unixTimestamp);

                    // When the task executed successfully, record the runtime as last success runtime. Otherwise return 0
                    if (t.LastTaskResult == 0)
                    {
                        this.TaskLastSuccessRuntimeGauge.Labels(t.Name, folderName, lastState).Set(unixTimestamp);
                    }
                    else
                    {
                        this.TaskLastSuccessRuntimeGauge.Labels(t.Name, folderName, lastState).Set(0);
                    }
                }
            }

        }

        private string ConvertTaskStateToString(Microsoft.Win32.TaskScheduler.TaskState state)
        {
            switch(state)
            {
                case TaskState.Ready:
                    return "ready";
                case TaskState.Disabled:
                    return "disabled";
                case TaskState.Queued:
                    return "queued";
                case TaskState.Running:
                    return "running";
                default:
                    return "unknown";
            }
        }

    }
}

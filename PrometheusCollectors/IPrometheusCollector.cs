using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrometheusCollector
{
    public interface IPrometheusCollector
    {
        void RegisterMetrics();
    }
}

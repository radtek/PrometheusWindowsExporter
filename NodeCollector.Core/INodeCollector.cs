using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeCollector.Core
{
    public interface INodeCollector
    {
        string GetName();

        void RegisterMetrics();
        void Shutdown();
    }
}

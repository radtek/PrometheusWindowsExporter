using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

// Docu: http://blog.nager.at/2012/01/visual-studio-2010-windows-service-erstellen/
// #Install
// InstallUtil.exe "C:\Projects\WindowsService1\bin\Debug\WindowsService1.exe"
// #Uninstall
// InstallUtil.exe /u "C:\Projects\WindowsService1\bin\Debug\WindowsService1.exe"


namespace NodeExporterWindows.Service
{
    public partial class NodeExporterService : ServiceBase
    {
        public NodeExporterService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }
    }
}

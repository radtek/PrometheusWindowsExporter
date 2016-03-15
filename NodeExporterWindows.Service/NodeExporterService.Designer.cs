namespace NodeExporterWindows.Service
{
    partial class NodeExporterService
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.eventLogMaster = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.eventLogMaster)).BeginInit();
            // 
            // eventLogMaster
            // 
            this.eventLogMaster.Log = "Application";
            this.eventLogMaster.Source = "PrometheusNodeExporter";
            // 
            // NodeExporterService
            // 
            this.ServiceName = "PrometheusNodeExporter";
            ((System.ComponentModel.ISupportInitialize)(this.eventLogMaster)).EndInit();

        }

        #endregion

        private System.Diagnostics.EventLog eventLogMaster;
    }
}

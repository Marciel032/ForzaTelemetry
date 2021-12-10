using ForzaTelemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ForzaTelemetryDemo
{
    public partial class MainForm : Form
    {
        private TelemetryReader telemetryReader;

        public MainForm()
        {            
            InitializeComponent();
            telemetryReader = new TelemetryReader(40532);
            telemetryReader.OnTelemetryRead += TelemetryReader_OnTelemetryRead;
        }

        private void TelemetryReader_OnTelemetryRead(Telemetry telemetry)
        {
            var texto = JsonConvert.SerializeObject(telemetry, Formatting.Indented);
            richTextBox1.BeginInvoke((MethodInvoker)delegate ()
            {
                richTextBox1.Text = texto;
                lblLastRefresh.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            });
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartStopTelemetry(true);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StartStopTelemetry(false);
        }

        private void StartStopTelemetry(bool start)
        {
            if (start)
                telemetryReader.Start();
            else
                telemetryReader.Stop();
            btnStart.Enabled = !start;
            btnStop.Enabled = start;
        }        
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NAudio.Wave;

namespace NGuitar
{
    public partial class SelectInputDeviceForm : Form
    {
        private MainForm mainForm;
        public SelectInputDeviceForm(MainForm mainForm)
        {
            InitializeComponent();
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.mainForm = mainForm;
            int waveInDevices = WaveIn.DeviceCount;
            numericUpDown1.Maximum = waveInDevices;
            List<string> lines = new List<string>();
            for (int waveInDeviceIndex = 0; waveInDeviceIndex < waveInDevices; waveInDeviceIndex++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDeviceIndex);
                lines.Add($"Device {waveInDeviceIndex}: {deviceInfo.ProductName}, {deviceInfo.Channels} channels");
            }
            richTextBox1.Lines = lines.ToArray();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mainForm.SelectedInputDeviceIndex = (int) numericUpDown1.Value;
            this.Close();
        }
    }
}

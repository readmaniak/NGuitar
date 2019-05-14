using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.Diagnostics;
using System.Threading;

namespace NGuitar
{    
    public partial class MainForm : Form
    {
        private readonly List<double> Notes;
        public MainForm()
        {
            InitializeComponent();
            richTextBox1.Font = new Font(FontFamily.GenericMonospace, richTextBox1.Font.Size);
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Notes = StaticUsefulStuff.GetNotes();
            tablatureProcessor = new TablatureProcessor();
            tablatureProcessor.OnTabLoaded += TablatureProcessor_OnTabLoaded;
            tablatureProcessor.OnPositionUpdated += TablatureProcessorOnOnPositionUpdated;

            int deviceCount = WaveIn.DeviceCount;
            if (deviceCount == 0)
            {
                MessageBox.Show(
                    "No sound input devices found. Having at least one of them is essential, thus application is closing now",
                    "General error!");
                this.Close();
            }
            else
            {
                if (deviceCount != 1)
                {
                    this.Hide();
                    new SelectInputDeviceForm(this).ShowDialog();
                    this.Show();
                }
                else
                {
                    SelectedInputDeviceIndex = 0;
                }
                waveIn = new WaveIn {DeviceNumber = SelectedInputDeviceIndex};
                sampleAggregator = new SampleAggregator(fftlength, timeScaleFactor);
                sampleAggregator.FftCalculated += sampleAggregator_FftCalculated;
                waveIn.DataAvailable += waveIn_DataAvailable;
                binSize = sampleRate / fftlength;
                int channels = 1; // mono
                var waveFormat = new WaveFormat(sampleRate, channels);
                waveIn.WaveFormat = waveFormat;
                bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
                waveIn.StartRecording();
                //var waveOut = new DirectSoundOut(50);
                WaveOut waveOut = new WaveOut();
                waveOut.Init(bufferedWaveProvider);
                waveOut.Volume = 0;
                waveOut.Play();
            }
        }

        public int SelectedInputDeviceIndex;
        BufferedWaveProvider bufferedWaveProvider;
        WaveIn waveIn;
        SampleAggregator sampleAggregator;
        TablatureProcessor tablatureProcessor;
        const int timeScaleFactor = 8;
        const int fftlength = 2048 * 8;
        int sampleRate = 48000;

        const float scale = 16384f;
        float binSize;

        private int lastIndexOfPosition;
        private void TablatureProcessorOnOnPositionUpdated(int indexOfPosition)
        {
            int stubs = (indexOfPosition) / chordsInLine;
            int lineoffset = (indexOfPosition % chordsInLine) * (repeatSpace + 1) + repeatSpace;
            int offset = stubs * stublength + lineoffset;
            Mark(lastIndexOfPosition, linelength, FontStyle.Regular);
            Mark(offset, linelength, FontStyle.Bold);

            if (lastIndexOfPosition / stublength != stubs)
            {
                if (richTextBox1.InvokeRequired)
                {
                    richTextBox1.Invoke(
                      new ThreadStart(delegate
                      {
                          richTextBox1.ScrollToCaret();
                      }));
                }
                else
                {
                    richTextBox1.ScrollToCaret();
                }
            }
            lastIndexOfPosition = offset;
        }

        private void Mark(int position, int linelength, FontStyle fontStyle)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(
                  new ThreadStart(delegate {
                      _Mark(position, linelength, fontStyle);
                  }));
            }
            else
            {
                _Mark(position, linelength, fontStyle);
            }
        }

        private void _Mark(int position, int lineLength, FontStyle fontStyle)
        {
            Font newFont = new Font(richTextBox1.Font, fontStyle);
            position += 5 * linelength;
            richTextBox1.SelectionLength = 1;
            for (int i = 0; i < 6; i++, position -= linelength)
            {
                richTextBox1.SelectionStart = position;
                richTextBox1.SelectionFont = newFont;
            }
            richTextBox1.SelectionLength = 0;
        }

        private const int repeatSpace = 3;
        private const int chordsInLine = 20;
        private const int emptyLines = 3;

        int linelength;
        int stublength;
        private bool tabLoaded = false;
        private void TablatureProcessor_OnTabLoaded(List<StringRelatedChord> tab)
        {
            if (tab == null)
            {
                tabLoaded = false;
                MessageBox.Show("Error when loading tab", "Error!");
            }

            tabLoaded = true;
            StringBuilder[] stringBuilders = new StringBuilder[6];
            for (int i = 0; i < 6; i++)
                stringBuilders[i] = new StringBuilder();
            AddSpaces(stringBuilders);
            List<string> finalLines = new List<string>();
            for (int i = 0; i < tab.Count; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    stringBuilders[j].Append(tab[i].Frets[j] == null ? "-" : tab[i].Frets[j].Value.ToString());
                    stringBuilders[j].Append('-', repeatSpace);
                }

                if (i % chordsInLine == chordsInLine-1 || i == tab.Count-1)
                {
                    if (i == tab.Count-1)
                    {
                        int delta = chordsInLine * (repeatSpace + 1) + repeatSpace - stringBuilders[0].Length;
                        if (delta!=0)
                            for (int j = 0; j < 6; j++)
                            {
                                stringBuilders[j].Append('-', delta);
                            }
                        
                    }

                    finalLines.AddRange(stringBuilders.Select(builder => builder.ToString()));

                    for (int j = 0; j < 6; j++)
                    {
                        stringBuilders[j].Clear();
                    }

                    for (int j = 0; j < emptyLines; j++)
                        finalLines.Add("");
                    AddSpaces(stringBuilders);
                    
                }
            }

            richTextBox1.Lines = finalLines.ToArray();
            lastIndexOfPosition = 0;
            linelength = (chordsInLine * (repeatSpace + 1) + repeatSpace + 1);
            stublength = linelength * 6 + emptyLines;
        }

        private void AddSpaces(StringBuilder[] stringBuilders)
        {
            for (int i = 0; i < 6; i++)
            {
                stringBuilders[i].Append('-', repeatSpace);
            }
        }

        private float[] prevFFT = null;

        void sampleAggregator_FftCalculated(object sender, FftEventArgs e)
        {
            if (!tabLoaded)
                return;
            var mas = e.Result.Select(x => x.X * x.X + x.Y * x.Y).ToArray();
            if (prevFFT == null)
            {
                prevFFT = new float[e.Result.Length / 2];
                Array.Copy(mas, prevFFT, mas.Length / 2);
            }
            for (int i = 0; i<mas.Length / 2; i++)
            {
                mas[i] = Math.Max(mas[i] - prevFFT[i], 0);
            }
            int maxind = 0;

            for (int i = 0; i<mas.Length/2; i++)
            {
                if (mas[maxind] < mas[i]) maxind = i;
            }

            double[] noteBins = new double[Notes.Count];
            if (mas[maxind] > 0.0001)
            {

                for (int i = 0; i < mas.Length / 2; i++) //нормализуем
                {
                    mas[i] /= mas[maxind];
                }

                noteBins = new double[Notes.Count];
                for (int i = 0; i < mas.Length / 2; i++)
                {
                    double frequency = (double)(i * this.sampleRate) / fftlength;
                    int x = FindClosestIndex(frequency);
                    noteBins[x] += mas[i];
                }
                var maxBinValue = noteBins.Max();
                noteBins = noteBins.Select(a => a / maxBinValue).ToArray();
            }
            /*using (Graphics g = pictureBox1.CreateGraphics())
            {
                g.Clear(Color.White);
                for (int i = 0; i<noteBins.Length; i++)
                {
                    g.DrawLine(p, new Point(i*10, 0), new Point(i*10, (int)(noteBins[i] * 200)));
                }
            }*/
            tablatureProcessor.Compare(noteBins);
        }

        int FindClosestIndex(double frequency)
        {
            var diff = Notes.Select(x => Math.Abs(x - frequency)).ToList();
            int minind = 0;
            for (int i = 0; i < diff.Count; i++)
            {
                if (diff[i]<diff[minind]) minind = i;
            }
            return minind;
        }

        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] output = new byte[e.BytesRecorded];
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                float sample32 = sample / scale;
                sample32 *= 2;
                sampleAggregator.Add(sample32);
                /*min = Math.Min(sample, min);
                max = Math.Max(sample, max);

                float preGain = 1f;//1.5f;
                float postGain = 1f;// 1.5f;
                float clip = 0.02f;// 0.1f;

                sample32 *= preGain;
                if (sample32 > clip)
                    sample32 = clip;
                else
                    if (sample32 < -clip)
                        sample32 = -clip;
                sample32 *= postGain;*/

                short sampleback = (short)(sample32 * scale);
                output[index] = (byte)sampleback;
                output[index + 1] = (byte)(sampleback >> 8);

            }
            bufferedWaveProvider.AddSamples(output, 0, e.BytesRecorded);

        }

        private int FindClosestChord(int index)
        {
            int chordsBefore = index / (repeatSpace + 1);
            int delta = index % (repeatSpace + 1);
            if (chordsBefore == chordsInLine)
                return chordsInLine;
            if (delta > repeatSpace / 2d)
                return chordsBefore + 1;
            if (chordsBefore == 0)
                return 1;
            return chordsBefore;
        }

        private void loadNewTabToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            tablatureProcessor.LoadTab(openFileDialog.FileName);
        }

        private void manuallyUpdatePositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!tabLoaded)
                return;
            int selectionStart = richTextBox1.SelectionStart;
            int stubsBefore = selectionStart / stublength;
            int chordsInStubsBefore = stubsBefore * chordsInLine;
            int offset = selectionStart % stublength;
            offset %= linelength;
            var closestChord = FindClosestChord(offset) + chordsInStubsBefore - 1;
            closestChord = Math.Min(closestChord, tablatureProcessor.Tab.Count - 1);
            tablatureProcessor.ManuallyChangePosition(closestChord);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ChangeSettingsForm(tablatureProcessor).ShowDialog();
        }
    }
}

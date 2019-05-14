using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace NGuitar
{
    public partial class ChangeSettingsForm : Form
    {
        private TablatureProcessor tablatureProcessor;
        public ChangeSettingsForm(TablatureProcessor tablatureProcessor)
        {
            InitializeComponent();
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.tablatureProcessor = tablatureProcessor;

            UpdateText();

            int maximumPositionSkip;
            double missingNotesFraction;
            int maximumWrongNotes;
            int minimumDelay;
            (maximumPositionSkip, missingNotesFraction, maximumWrongNotes, minimumDelay) = tablatureProcessor.GetOtherSettings();
            this.numericUpDown1.Value = maximumPositionSkip;
            this.numericUpDown2.Value = (decimal) missingNotesFraction;
            this.numericUpDown3.Value = maximumWrongNotes;
            this.numericUpDown4.Value = minimumDelay;
        }

        private void UpdateText()
        {
            var array = tablatureProcessor.Tune.StringTunes.Select(index => StaticUsefulStuff.ConvertToNoteName(index));
            this.textBox1.Text = "";
            foreach (var noteName in array)
            {
                textBox1.Text += noteName + " ";
            }

            textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var guitarTune = new GuitarTune(textBox1.Text.Split(' ')
                    .Select<string, int>(noteName => StaticUsefulStuff.ConvertToNoteIndex(noteName))
                    .ToArray());
                var notes = StaticUsefulStuff.GetNotes();
                foreach (var desiredNoteIndex in guitarTune.StringTunes)
                {
                    if (desiredNoteIndex < 0 || desiredNoteIndex >= notes.Count)
                        throw new Exception();
                }
                tablatureProcessor.ChangeTune(guitarTune);
            }
            catch
            {
                MessageBox.Show("Wrong syntax of desired tune, thus tune was not updated", "Error!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tablatureProcessor.ChangeOtherSettings((int) numericUpDown1.Value, (double) numericUpDown2.Value,
                (int) numericUpDown3.Value, (int) numericUpDown4.Value);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            tablatureProcessor.ChangeTune(GuitarTune.GetDefaultGuitarTune());
            UpdateText();
        }
    }
}

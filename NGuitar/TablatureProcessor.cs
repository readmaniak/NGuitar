using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NGuitar
{
    public class TablatureProcessor
    {
        public GuitarTune Tune;
        public List<StringRelatedChord> Tab = new List<StringRelatedChord>();
        public List<StringRelatedChord> TabWithBackground = new List<StringRelatedChord>();
        public TimeSpan MinimumDelay = TimeSpan.FromMilliseconds(200);
        public DateTime LastSuccessfulRecognition = DateTime.MinValue;
        public int CurrentPosition = -1;
        public int MaximumPositionSkip = 3;
        public double MaximumMissingNotesFraction = 0;
        public int MaximumWrongNotes = 0;

        public TablatureProcessor(GuitarTune guitarTune = null)
        {
            this.Tune = guitarTune ?? GuitarTune.GetDefaultGuitarTune();
        }

        public void ChangeTune(GuitarTune guitarTune)
        {
            this.Tune.ChangeTuneToThis(guitarTune);
        }

        public void ChangeOtherSettings(int maximumPositionSkip, double maximumMissingNotesFraction,
            int maximumWrongNotes, int minimumDelay)
        {
            this.MaximumPositionSkip = maximumPositionSkip;
            this.MaximumMissingNotesFraction = maximumMissingNotesFraction;
            this.MaximumWrongNotes = maximumWrongNotes;
            this.MinimumDelay = TimeSpan.FromMilliseconds(minimumDelay);
        }

        public (int maximumPositionSkip, double maximumMissingNotesFraction,
            int maximumWrongNotes, int minimumDelay) GetOtherSettings()
        {
            return (MaximumPositionSkip, MaximumMissingNotesFraction, MaximumWrongNotes, (int)MinimumDelay.TotalMilliseconds);
        }

        public void LoadTab(string path)
        {
            var lines = File.ReadAllLines(path).ToList();
            lines.RemoveAll(line => line.Length == 0 || !line.Contains('-'));

            if (lines.Count % 6 != 0)
            {
                OnTabLoaded?.Invoke(null);
                return;
            }
            var lines6 = new List<StringBuilder>();
            for (int i = 0; i < 6; i++)
                lines6.Add(new StringBuilder());
            for (int i = 0; i < lines.Count; i+=6)
            {
                for (int j = 0; j<6; j++)
                {
                    lines6[j].Append(lines[i + j]);
                }
            }

            LoadTab(lines6.Select(sb => sb.ToString()).ToArray());
            CurrentPosition = -1;
        }

        public void LoadTab(string[] lines)
        {
            Tab.Clear();
            int minCharIndex = lines.Select(line => line.Length).Min();
            for (int charIndex = 0; charIndex < minCharIndex; charIndex++)
            {
                bool noFretsHit = true;
                StringRelatedChord tempChord = new StringRelatedChord(Tune);
                for (int lineIndex = 0; lineIndex<6; lineIndex++)
                {
                    char c = lines[lineIndex][charIndex];
                    if (c >= '0' && c <= '9')
                    {
                        noFretsHit = false;
                        int fret = c - '0';
                        tempChord.Frets.Add(fret);
                    }
                    else
                    {
                        tempChord.Frets.Add(null);
                    }
                }

                if (!noFretsHit)
                {
                    Tab.Add(tempChord);
                }
            }

            TabWithBackground.Add(Tab[0]);
            for (int i = 1; i < Tab.Count; i++)
            {
                TabWithBackground.Add(TabWithBackground[i - 1].UpdateWith(Tab[i]));
            }

            if (Tab.Count == 0)
            {
                OnTabLoaded?.Invoke(null);
                return;
            }
            OnTabLoaded?.Invoke(Tab);
        }

        public void Compare(double[] noteBins)
        {
            DateTime now = DateTime.Now;
            List<int> heardNotes = new List<int>();
            for (int i = 0; i < noteBins.Length; i++)
            {
                if (noteBins[i] > 0.01)
                {
                    heardNotes.Add(i);
                }
            }

            var timeSpanMs = (now - LastSuccessfulRecognition).TotalMilliseconds;
            var delayMs = MinimumDelay.TotalMilliseconds;
            for (int i = 1; i < MaximumPositionSkip && i+CurrentPosition<Tab.Count; i++)
            {
                if (timeSpanMs < delayMs * i)
                    return;
                StringRelatedChord possibleCurrentChord = TabWithBackground[CurrentPosition+i];
                int missing, wrong;
                (missing, wrong) =
                    StringRelatedChord.CalculateDiff(possibleCurrentChord, Tab[CurrentPosition + i], heardNotes);
                int notesCount = Tab[CurrentPosition + i].GetFretsCount();
                int maximumMissing = (int) Math.Round(notesCount * this.MaximumMissingNotesFraction);
                if (missing<=maximumMissing && wrong<=MaximumWrongNotes)
                {
                    CurrentPosition += i;
                    LastSuccessfulRecognition = DateTime.Now;
                    OnPositionUpdated?.Invoke(CurrentPosition);
                    return;
                }
            }
        }

        public void ManuallyChangePosition(int position)
        {
            this.CurrentPosition = position;
            LastSuccessfulRecognition = DateTime.Now;
            OnPositionUpdated?.Invoke(CurrentPosition);
        }

        public delegate void TabLoadedHandler(List<StringRelatedChord> tab);

        public delegate void PositionUpdatedHandler(int indexOfPosition);

        public event TabLoadedHandler OnTabLoaded;
        public event PositionUpdatedHandler OnPositionUpdated;
    }

    public class Chord
    {
        public List<int> Notes;

        public Chord()
        {
            Notes = new List<int>();
        }

        public Chord(List<int> Notes)
        {
            this.Notes = Notes;
        }

        public bool ContainsNoteIncludingHarmonics(int note)
        {
            foreach (var containedNote in Notes)
            {
                if (note < containedNote) continue;
                if ((note - containedNote) % 12 == 0)
                    return true;
            }

            return false;
        }
    }

    public class StringRelatedChord
    {
        private GuitarTune Tune;
        public List<int?> Frets;

        public StringRelatedChord(GuitarTune tune, List<int?> frets = null)
        {
            Tune = tune;
            Frets = frets ?? new List<int?>();
        }

        public Chord ConvertToChord()
        {
            Chord c = new Chord();
            for (int i = 0; i < 6; i++)
            {
                if (Frets[i] is int fret)
                    c.Notes.Add(Tune.GetNoteIndex(i, fret));
            }

            return c;
        }

        public int GetFretsCount()
        {
            return Frets.Count(x => x != null);
        }

        public StringRelatedChord UpdateWith(StringRelatedChord chord)
        {
            StringRelatedChord newStringRelatedChord = new StringRelatedChord(Tune, new List<int?>(Frets));
            for (int i = 0; i < 6; i++)
            {
                if (chord.Frets[i] != null)
                {
                    newStringRelatedChord.Frets[i] = chord.Frets[i];
                }
            }

            return newStringRelatedChord;
        }

        public static (int missing, int wrong) CalculateDiff(StringRelatedChord calculatedWithBackground, StringRelatedChord actualChordToBePlayed, List<int> heardNotes)
        {
            int missing = 0;
            int wrong = 0;
            Chord chord = actualChordToBePlayed.ConvertToChord();
            List<int> wrongNotes = new List<int>();
            foreach (var note in chord.Notes)
            {
                if (!heardNotes.Contains(note))
                {
                    missing++;
                }
            }

            chord = calculatedWithBackground.ConvertToChord();
            foreach (var heardNote in heardNotes)
            {
                if (!chord.ContainsNoteIncludingHarmonics(heardNote))
                {
                    if (wrongNotes.All(wrongNote => heardNote % 12 != wrongNote % 12))
                    {
                        wrong++;
                        wrongNotes.Add(heardNote);
                    }
                }
            }

            return (missing, wrong);
        }
    }

    public class GuitarTune
    {
        public int[] StringTunes;

        public static GuitarTune GetDefaultGuitarTune()
        {
            var notes = StaticUsefulStuff.GetNotes();
            int _1 = notes.FindIndex(a => StaticUsefulStuff.floatingPointEqual(a, 329.63));
            int _2 = notes.FindIndex(a => StaticUsefulStuff.floatingPointEqual(a, 246.94));
            int _3 = notes.FindIndex(a => StaticUsefulStuff.floatingPointEqual(a, 196.00));
            int _4 = notes.FindIndex(a => StaticUsefulStuff.floatingPointEqual(a, 146.83));
            int _5 = notes.FindIndex(a => StaticUsefulStuff.floatingPointEqual(a, 110.00));
            int _6 = notes.FindIndex(a => StaticUsefulStuff.floatingPointEqual(a, 82.41));
            return new GuitarTune(new []{_1, _2, _3, _4, _5, _6});
        }

        public GuitarTune(int[] stringTunes)
        {
            this.StringTunes = stringTunes;
        }

        public int GetNoteIndex(int stringIndex, int fretIndex)
        {
            return StringTunes[stringIndex] + fretIndex;
        }

        public void ChangeTuneToThis(GuitarTune newTune)
        {
            this.StringTunes = newTune.StringTunes.Select(x => x).ToArray();
        }
    }
}

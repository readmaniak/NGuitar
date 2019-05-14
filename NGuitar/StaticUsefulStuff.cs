using System;
using System.Collections.Generic;

namespace NGuitar
{
    static class StaticUsefulStuff
    {
        private static List<string> noteLetters = new List<string>()
        {
            "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
        };
        public static List<double> GetNotes()
        {
            var Notes = new List<double>(){
            //    2             3               4               5
                65.4064,        130.8128,       261.6256,       523.2511,    //C
                69.2957,        138.5913,       277.1826,       554.3653,    //C#
                73.4162,        146.8324,       293.6648,       587.3295,    //D
                77.7817,        155.5635,       311.1270,       622.2540,    //D#
                82.4069,        164.8138,       329.6276,       659.2551,    //E
                87.3071,        174.6141,       349.2282,       698.4565,    //F
                92.4986,        184.9972,       369.9944,       739.9888,    //F#
                97.9989,        195.9977,       391.9954,       783.9909,    //G
                103.8262,       207.6523,       415.3047,       830.6094,    //G#
                110.0000,       220.0000,       440.0000,       880.0000,    //A
                116.5409,       233.0819,       466.1638,       932.3275,    //A#
                123.4708,       246.9417,       493.8833,       987.7666     //B
            };
            Notes.Sort();
            return Notes;
        }

        public static int ConvertToNoteIndex(string noteName)
        {
            int number = noteName[noteName.Length - 1] - '0';
            int letter = noteLetters.FindIndex(s => s.Equals(noteName.Substring(0, noteName.Length - 1)));
            return (number - 2) * 12 + letter;
        }

        public static string ConvertToNoteName(int index)
        {
            int number = index / 12 + 2;
            string letter = noteLetters[index % 12];
            return letter + number;
        }

        public static bool floatingPointEqual(double a, double b, double eps = 0.1)
        {
            return Math.Abs(a - b) < eps;
        }
    }
}

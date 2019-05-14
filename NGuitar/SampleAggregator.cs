using System;

namespace NGuitar
{
    using NAudio.Dsp; // The Complex and FFT are here!
    using System.Threading.Tasks;

    class SampleAggregator
    {
        // FFT
        public event EventHandler<FftEventArgs> FftCalculated;

        // This Complex is NAudio's own! 
        private Complex[] fftBuffer;
        private Complex[] fftBufferDoubled;
        private Complex[] fftBufferPrev;
        private FftEventArgs fftArgs;
        private int fftPos;
        private int fftLength;
        private int m;
        private int timeScaleFactor;

        public SampleAggregator(int fftLength, int timeScaleFactor = 1)
        {
            if (!IsPowerOfTwo(fftLength))
            {
                throw new ArgumentException("FFT Length must be a power of two");
            }
            this.timeScaleFactor = timeScaleFactor;
            this.m = (int)Math.Log(fftLength, 2.0);
            this.fftLength = fftLength;
            this.fftBuffer = new Complex[fftLength];
            this.fftBufferPrev = new Complex[fftLength];
            this.fftBufferDoubled = new Complex[fftLength * 2];
            this.fftArgs = new FftEventArgs(fftBuffer);
        }

        bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0 && (x > 1);
        }

        public void Add(float value)
        {
            //Console.WriteLine("added");
            if (FftCalculated != null)
            {
                // Remember the window function! There are many others as well.
                fftBufferDoubled[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
                fftBufferDoubled[fftPos].Y = 0; // This is always zero with audio.
                fftPos++;
                if (fftPos >= fftLength && fftPos % (fftLength / timeScaleFactor) == 0)
                {
                    for(int i = fftPos - fftLength, j = 0; i < fftPos; i++, j++)
                    {
                        fftBuffer[j] = fftBufferDoubled[i];
                    }
                    
                    FastFourierTransform.FFT(true, m, fftBuffer);
                    FftCalculated(this, fftArgs);


                    //fftPos = 0;
                }
                if (fftPos == 2*fftLength)
                {
                    for (int i = fftLength, j = 0; j<fftLength; i++, j++)
                    {
                        fftBufferDoubled[j] = fftBufferDoubled[i];
                    }
                    fftPos = fftLength;
                }
            }
        }

        public void Add(float[] values, int numberOfValues)
        {
            for (int i = 0; i < numberOfValues; i++)
            {
                this.Add(values[i]);
            }
        }
    }

    public class FftEventArgs : EventArgs
    {
        public FftEventArgs(Complex[] result)
        {
            this.Result = result;
        }
        public Complex[] Result { get; private set; }
    }
}
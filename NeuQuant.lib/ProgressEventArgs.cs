using System;

namespace NeuQuant
{
    public class ProgressEventArgs : EventArgs
    {
        public double Percent { get; private set; }

        public ProgressEventArgs(double percent)
        {
            Percent = percent;
        }
    }
}

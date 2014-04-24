using CSMSL;
using CSMSL.Chemistry;
using NeuQuant.IO;
using System;
using System.Linq;

namespace NeuQuant.Processing
{
    public partial class Processor : IDisposable
    {
        protected NeuQuantFile NqFile;
        protected int NumberOfIsotopesToQuantify;
        protected double MinimumRtDelta;
        protected double MaximumRtDelta;
        protected Tolerance MS2Tolerance;

        protected double MinimumSN = 3;
        protected double MaximumSN = double.MaxValue;
        protected double MinimumResolution = 0;
        protected double MaximumRtRangePerFeature = 3;

        protected double IsotopicDistributionPercentError = 0.25;

        protected bool UseIsotopicDistribution = true;

        protected Func<double, int, double> TheoreticalSpacing;
        protected Predicate<NeuQuantPeptide> Resolvable;
        
        public bool IsOpen { get; private set; }

        public Processor(NeuQuantFile nqFile, int isotopesToQuantify = 3, double minRtDelta = 0.25, double maxRtDelta = 0.25, double resolution = 240000, double resolutionAt = 400, double quantAtPeakHeight = 10, bool checkIsotopicDistribution = true)
        {
            NqFile = nqFile;
            NumberOfIsotopesToQuantify = isotopesToQuantify;
            MinimumRtDelta = minRtDelta;
            MaximumRtDelta = maxRtDelta;
            MS2Tolerance = new Tolerance(ToleranceType.PPM, 10);
            MinimumResolution = resolution/2 + 1;

            UseIsotopicDistribution = checkIsotopicDistribution;

            double coefficient = Math.Sqrt(Math.Log(100.0 / quantAtPeakHeight)) / Math.Sqrt(Math.Log(2));
            TheoreticalSpacing = (mass, charge) =>
            {
                double mz = Mass.MzFromMass(mass, charge);
                return coefficient * (mz / (resolution * Math.Sqrt(resolutionAt / mz)));
            };

            Resolvable = peptide =>
            {
                if (!peptide.ContainsQuantitativeChannel)
                    return false;

                // Get the lightest mass
                double mass = peptide.QuantifiableChannels.Values[0].MonoisotopicMass;
                int bestCharge = peptide.IdentifiedChargeStates.Min();
                double theoSpacing = TheoreticalSpacing(mass, bestCharge);
                double spacing = peptide.BiggestThSpacing;
                return spacing > theoSpacing;
            };
        }

        public void Open()
        {
            if (NqFile == null)
                throw new ArgumentNullException("No NeuQuant File Specified");
            
            IsOpen = NqFile.Open();

           
            //TODO Save Analysis Parameters in NeuQuant File
        }

        #region Close/Dispose

        void IDisposable.Dispose()
        {
            Close();
        }

        public void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (NqFile != null) 
                    NqFile.Dispose();
                _allSpectra = null;
                _times = null;
            }
        }

        #endregion

        #region Events

        public event EventHandler<ProgressEventArgs> Progress;
        public event EventHandler<MessageEventArgs> Message; 

        private void OnProgress(double percent)
        {
            var handler = Progress;
            if (handler != null)
            {
                handler(this, new ProgressEventArgs(percent));
            }
        }

        private void OnMessage(string message)
        {
            var handler = Message;
            if (handler != null)
            {
                handler(this, new MessageEventArgs(message));
            }
        }

        #endregion
    }
}

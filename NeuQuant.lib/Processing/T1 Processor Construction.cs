using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        public int NumberOfIsotopesToQuantify { get; set; }
        public double MinimumRtDelta { get; set; }
        public double MaximumRtDelta { get; set; }
        public Tolerance MS2Tolerance { get; set; }

        public double MinimumSN { get; set; }
        public double MaximumSN { get; set; }
        public double MinimumResolution { get; set; }
        public double MaximumRtRangePerFeature { get; set; }

        public double IsotopicDistributionPercentError { get; set; }

        public bool NoiseBandCap { get; set; }
        public bool UseIsotopicDistribution { get; set; }

        protected Func<double, int, double> TheoreticalSpacing;
        protected Predicate<NeuQuantPeptide> Resolvable;

        private List<NeuQuantSample> _samples;

        public string Name { get; internal set; }
        public long ID { get; internal set; }

        private bool IsOpen { get; set; }

        public string BaseDirectory { get { return Path.GetDirectoryName(NqFile.FilePath); } }

        public Processor(NeuQuantFile nqFile)
        {
            NqFile = nqFile;
        }

        public Processor(NeuQuantFile nqFile, string name = "Analysis", int isotopesToQuantify = 3, double minRtDelta = 0.25, double maxRtDelta = 0.25, 
            double resolution = 240000, double resolutionAt = 400, double quantAtPeakHeight = 10, bool checkIsotopicDistribution = true,
            bool noiseBandCap = true)
        {
            NqFile = nqFile;
            NumberOfIsotopesToQuantify = isotopesToQuantify;
            MinimumRtDelta = minRtDelta;
            MaximumRtDelta = maxRtDelta;
            MS2Tolerance = new Tolerance(ToleranceType.PPM, 10);
            MinimumResolution = resolution/2 + 1;

            NoiseBandCap = noiseBandCap;
            UseIsotopicDistribution = checkIsotopicDistribution;

            Name = name;
            MinimumSN = 3;
            MaximumSN = double.MaxValue;
            IsotopicDistributionPercentError = 0.25;
            MaximumRtRangePerFeature = 3;

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

            // Save this analysis
            ID = SaveAnalysisParameters(Name);

            _samples = NqFile.GetSamples().ToList();
        }

        public long SaveAnalysisParameters(string analysisName = "")
        {
            NqFile.BeginTransaction();

            long analysisID = NqFile.SaveAnalysis(analysisName);

            // handy: http://stackoverflow.com/questions/737151/how-to-get-the-list-of-properties-of-a-class
            foreach(var prop in GetType().GetProperties())
            {
                NqFile.SaveAnalysisParameter(analysisID, prop.Name, prop.GetValue(this, null));
            }

            NqFile.EndTranscation();

            return analysisID;
        }

        public void SetValue(string propertyName, object value)
        {

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

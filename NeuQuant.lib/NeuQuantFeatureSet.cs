using CSMSL;
using CSMSL.Chemistry;
using CSMSL.IO.Thermo;
using CSMSL.Proteomics;
using CSMSL.Spectral;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuQuant
{
    public class NeuQuantFeatureSet : IEnumerable<NeuQuantFeature>
    {
        public NeuQuantPeptide Peptide { get; private set; }
        public ThermoRawFile RawFile { get; private set; }
        public int ChargeState { get; private set; }
        public int NumberOfIsotopes { get; private set; }
        public List<NeuQuantSpectrum> Spectra { get; private set; }
        public List<PeptideSpectrumMatch> PSMs { get; private set; }
        public PeptideSpectrumMatch BestPSM { get; private set; }
        private List<NeuQuantFeature> _features;
        private Dictionary<Peptide, IRange<int>> _featureBounds;

        public double MinimumRetentionTime { get { return _features[0].RetentionTime; } }
        public double MaximumRetentionTime { get { return _features[_features.Count-1].RetentionTime; } }
        
        public int NumberOfFeatures { get { return _features.Count;} }

        public NeuQuantFeature this[int index]
        {
            get { return _features[index]; }
        }

        public NeuQuantFeatureSet(NeuQuantPeptide peptide, int chargeState, IEnumerable<PeptideSpectrumMatch> psms, ThermoRawFile rawFile, List<NeuQuantSpectrum> spectra, int numberOfIsotopes = 3)
        {
            Peptide = peptide;
            Peptide.AddFeatureSet(this);
            ChargeState = chargeState;
            RawFile = rawFile;
            Spectra = spectra;
            PSMs = psms.ToList();
            NumberOfIsotopes = numberOfIsotopes;

            // Find best psm
            double bestScore = double.MaxValue;
            BestPSM = null;
            foreach (var psm in PSMs)
            {
                if (psm.MatchScore < bestScore)
                {
                    bestScore = psm.MatchScore;
                    BestPSM = psm;
                }
            }

        }

        public double GetIntegratedIntensity(Peptide peptide)
        {
            return GetIntegratedIntensity(peptide, 0, double.MaxValue);
        }

        public double GetIntegratedIntensity(Peptide peptide, double minRT, double maxRT)
        {
            return GetIntensities(peptide, minRT, maxRT).Sum();
        }

        public IEnumerable<double> GetIntensities(Peptide peptide)
        {
            return GetIntensities(peptide, 0, double.MaxValue);
        }

        public IEnumerable<double> GetIntensities(Peptide peptide, double minRT, double maxRT)
        {
            foreach (var feature in _features)
            {
                if (feature.RetentionTime < minRT)
                    continue;

                if (feature.RetentionTime > maxRT)
                    break;
                yield return feature.GetChannelIntensity(peptide);
            }
        }

        public Dictionary<Peptide, double> Quantify(bool noiseBandCap = true, double noiseLevel = 3)
        {
            Dictionary<Peptide, double> quant = new Dictionary<Peptide, double>();
            foreach (var channel in Peptide.QuantifiableChannels.Values)
            {
             
                IRange<int> range;
                if (!_featureBounds.TryGetValue(channel, out range))
                {
                    // Noise band cap this channel if requested
                    quant[channel] = noiseBandCap ? noiseLevel : 0;
                    continue;
                }

                List<double> ppmErrors = new List<double>();
                List<double> intensities = new List<double>();
              
                for (int i = range.Minimum; i < range.Maximum; i++)
                {
                    var feature = _features[i];
                    foreach (var ppmAndIntensity in feature.GetChannelMassErrorAndItensity(channel, noiseBandCap, noiseLevel))
                    {
                        ppmErrors.Add(ppmAndIntensity.Item1);
                        intensities.Add(ppmAndIntensity.Item2);
                    }
                }

                if (intensities.Count == 0)
                {
                    // Noise band cap this channel if requested
                    quant[channel] = noiseBandCap ? noiseLevel : 0;
                    continue;
                }

                double meanPPM = ppmErrors.Average();
                double stdDevPPM = ppmErrors.StdDev();

                DoubleRange ppmTolerance = new DoubleRange(meanPPM - stdDevPPM*2, meanPPM + stdDevPPM*2);

                double maxIntensity = 0;
                for (int i = 0; i < ppmErrors.Count; i++)
                {
                    double ppm = ppmErrors[i];
                    if (!ppmTolerance.Contains(ppm)) 
                        continue;
                    double intensity = intensities[i];
                    if (intensity > maxIntensity)
                    {
                        maxIntensity = intensity;
                    }
                }

                double threshold = maxIntensity/(Math.E*Math.E);
                double channelIntensity = 0;
                int avg = 0;
                for (int i = 0; i < ppmErrors.Count; i++)
                {
                    double ppm = ppmErrors[i];
                    double intensity = intensities[i];
                    if ((ppmTolerance.Contains(ppm) || double.IsNaN(ppm)) && intensity > threshold)
                    {
                        channelIntensity += intensity;
                        avg++;
                    }
                }

                //double maxIntensity = intensities.Max();
                //double threshold = maxIntensity/(2*Math.E);
                //double channelIntensity = intensities.Where(v => v >= threshold).Sum();

                //if (channelIntensity < noiseLevel)
                //{
                //    channelIntensity = noiseLevel;
                //}

                quant[channel] = channelIntensity;
            }
            return quant;
        }

        private Range<int> FindBounds(Peptide peptide, double rt, int smoothingPts = 3)
        {
            // Constructor a chromatogram for the current channel
            double[] times = new double[NumberOfFeatures];
            double[] intensities = new double[NumberOfFeatures];

            int i = 0;
            foreach (var feature in _features)
            {
                times[i] = feature.RetentionTime;

                // Ignore isotope distribution checks
                intensities[i] = feature.GetChannelIntensity(peptide, false);

                i++;
            }

            var chrom = new Chromatogram(times, intensities).Smooth(SmoothingType.BoxCar, smoothingPts);

            var apex = chrom.FindNearestApex(rt, 2);
            //var apex = chrom.GetApex(rt - 0.2, rt + 0.2);

            Range<double> width = chrom.GetPeakWidth(apex.Time, 0.1);

            double minRT = width.Minimum;
            double maxRT = width.Maximum;
            
            for(i = 0; i < _features.Count; i++)
            {
                double spacing = minRT - _features[i].RetentionTime;
                if(spacing < 0)
                    break;
            }

            int minIndex = i;
            for (i = minIndex+1; i < _features.Count; i++)
            {
                double spacing = maxRT - _features[i].RetentionTime;
                if (spacing < 0)
                    break;
            }
            int maxIndex = i - 1;
            
            return new Range<int>(minIndex, maxIndex);
            
        }

        public DoubleRange GetBounds(Peptide peptide)
        {
            IRange<int> intRange = null;
            if (!_featureBounds.TryGetValue(peptide, out intRange))
                return new DoubleRange();

            return new DoubleRange(_features[intRange.Minimum].RetentionTime, _features[intRange.Maximum].RetentionTime);
        }

        public void FindElutionProfile(int smoothingPts = 3)
        {
            // Storage for the feature bounds per channel (separate channels since deuterium can cause chromatographic shifts)
            _featureBounds = new Dictionary<Peptide, IRange<int>>();
            
            // If no features, there is no profile to find
            if (_features == null || _features.Count == 0)
                return;
            
            // The best psm Retention Time
            double bestRT = BestPSM.RetentionTime;

            // Process each channel separately
            foreach (Peptide channel in Peptide.QuantifiableChannels.Values)
            { 
                // Find the peak bounds
                Range<int> bounds = FindBounds(channel, bestRT, smoothingPts);

                // Store the bounds per channel
                _featureBounds.Add(channel, bounds);
            }
        }

        public void FindPeaks(Tolerance peakTolerance, int numberOfIsotopes = 3, double systematicThError = 0.0, bool checkIsotopicDistribuition = true, double isotopicPercentError = 0.25)
        {
            // Private store of all the features in this feature set
            _features = new List<NeuQuantFeature>();

            if (Peptide.ContainsIsotopologue)
            {
                // NeuCode (with or without clusters)
                NeuCodeFindPeaks(peakTolerance, numberOfIsotopes, systematicThError, checkIsotopicDistribuition, isotopicPercentError);
            }
            else if (Peptide.ContainsMultipleClusters)
            {
                // SILAC (only clusters, no isotopologues to worry about)
                SilacFindPeaks(peakTolerance, numberOfIsotopes, systematicThError, checkIsotopicDistribuition, isotopicPercentError);
            }
            else if (Peptide.ContainsQuantitativeChannel)
            {
                throw new ArgumentException("The peptide contains quantitative channels, but no isotopologues or clusters, should not be here!");
            }
            else
            {
                throw new ArgumentException("Peptide doesn't contain quantitative channels");
            }
        }

        private void SilacFindPeaks(Tolerance peakTolerance, int numberOfIsotopes, double systematicThError, bool checkIsotopicDistribuition, double isotopicPercentError)
        {
             // Loop over all the spectra in the feature
            foreach (var miniSpectrum in Spectra)
            {
                // Create a new feature for this spectrum
                NeuQuantFeature feature = new NeuQuantFeature(this, miniSpectrum);
                
                // Loop over each peptide (should be in its own cluster)
                foreach (var peptide in Peptide.QuantifiableChannels.Values)
                {
                    // Loop over each isotope
                    for (int isotope = 0; isotope < numberOfIsotopes; isotope++)
                    {
                        // Storage for the smallest and largest m/z in this cluster for this isotope
                        double mz = peptide.ToMz(ChargeState, isotope);
           
                        // Construct the m/z range of interest
                        MzRange mzRange = new MzRange(mz, peakTolerance);

                        // Extract the m/z range from the mini spectrum into its own, even smaller, tiny spectrum
                        // The mini spectrum may contain multiple clusters, so this step further divides that spectrum
                        // into a smaller spectrum, a memory optimization
                        var tinySpectrum = miniSpectrum.Extract(mzRange, systematicThError);

                        // Try to assign the peaks to the correct channel
                        feature.AssignPeaks(tinySpectrum, peptide, isotope, mzRange);
                    }

                    // Once all the isotopes peaks are assigned, perform the isotopic distribution check
                    feature.CheckIsotopicDistribution(1, isotopicPercentError, checkIsotopicDistribuition);
                }

                // Store the feature
                _features.Add(feature);
            }
        }

        private void NeuCodeFindPeaks(Tolerance peakTolerance, int numberOfIsotopes = 3, double systematicThError = 0.0, bool checkIsotopicDistribuition = true, double isotopicPercentError = 0.25)
        {
            // Loop over all the spectra in the feature
            foreach (var miniSpectrum in Spectra)
            {
                // Create a new feature for this spectrum
                NeuQuantFeature feature = new NeuQuantFeature(this, miniSpectrum);

                // Process each cluster separately, but we can store in the same feature since each cluster contains unique channels
                // We process clusters together in NeuCode
                foreach (var peptidesInCluster in Peptide.Clusters)
                {
                    // The number of isotopologues in this cluster
                    int numberOfPeptidesInCluster = peptidesInCluster.Count;

                    // Get the expected spacings for all the channels in this cluster, in Th (Lenght should be # of Peptides - 1)
                    double[] expectedSpacings = Isotopologue.GetExpectedSpacings(peptidesInCluster.Values, ChargeState);

                    // Loop over each isotope
                    for (int isotope = 0; isotope < numberOfIsotopes; isotope++)
                    {
                        // Storage for the smallest and largest m/z in this cluster for this isotope
                        double smallestMZ = peptidesInCluster.Values[0].ToMz(ChargeState, isotope);
                        double largestMZ = peptidesInCluster.Values[numberOfPeptidesInCluster - 1].ToMz(ChargeState, isotope);
                        
                        // Add a little wiggle room for both the smallest and largest MZ
                        double minMz = DoubleRange.FromPPM(smallestMZ, 15).Minimum;
                        double maxMZ = DoubleRange.FromPPM(largestMZ, 15).Maximum;

                        // Construct the m/z range of interest
                        MzRange mzRange = new MzRange(minMz, maxMZ);

                        // Extract the m/z range from the mini spectrum into its own, even smaller, tiny spectrum
                        // The mini spectrum may contain multiple clusters, so this step further divides that spectrum
                        // into a smaller spectrum, a memory optimization
                        var tinySpectrum = miniSpectrum.Extract(mzRange, systematicThError);

                        // Try to assign the peaks to the correct channels
                        feature.AssignPeaks(tinySpectrum, peptidesInCluster.Values, expectedSpacings, isotope, peakTolerance, new Tolerance(ToleranceType.PPM, 5));
                    }
                    
                    // Once all the isotopes peaks are assigned, perform the isotopic distribution check
                    feature.CheckIsotopicDistribution(numberOfPeptidesInCluster, isotopicPercentError, checkIsotopicDistribuition);
                    
                }
                // Store the feature
                _features.Add(feature);
            }
        }

        public int GetFeatureIndex(double retentionTime)
        {
            int bestFeature = 0;
            double minTime = double.MaxValue;
            for (int i = 0; i < _features.Count; i++)
            {
                var feature = _features[i];
                double difference = feature.RetentionTime - retentionTime;
                double absDiffernce = Math.Abs(difference);
                if (Math.Abs(difference) < minTime)
                {
                    minTime = absDiffernce;
                    bestFeature = i;
                }
            }

            return bestFeature;
        }

        public NeuQuantFeature GetFeature(double retentionTime)
        {
            return _features[GetFeatureIndex(retentionTime)];
        }

        public IEnumerable<double> PrecursorMassError(Peptide channel, int isotopes = 3, double ppmRange = 15)
        {
            for (int i = 0; i < isotopes; i++)
            {
                double mz = channel.ToMz(ChargeState, i);
                DoubleRange range = DoubleRange.FromPPM(mz, ppmRange);

                // Loop over all the spectra in the feature
                foreach (var miniSpectrum in Spectra)
                {
                    IPeak peak = miniSpectrum.GetClosestPeak(range);
                    if (peak != null)
                    {
                        yield return Tolerance.GetTolerance(peak.X, mz, ToleranceType.PPM);
                    }
                }
            }
        }

        public IEnumerable<double> PrecursorMassError(int isotopes = 3, double ppmRange = 15)
        {
            foreach (var peptide in Peptide.QuantifiableChannels.Values)
            {
                foreach (double error in PrecursorMassError(peptide, isotopes, ppmRange))
                    yield return error;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} (z = {1}, {2} Spectra)", Peptide, ChargeState, Spectra.Count);
        }
        
        public IEnumerator<NeuQuantFeature> GetEnumerator()
        {
            return _features.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _features.GetEnumerator();
        }
    }
}

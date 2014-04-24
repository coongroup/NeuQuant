using CSMSL;
using CSMSL.Chemistry;
using CSMSL.IO.Thermo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuQuant.Processing
{
    public partial class Processor
    {
        public List<NeuQuantFeatureSet> FeatureSets;
        
        private NeuQuantSpectrum[] _allSpectra;
        private double[] _times;
        private long _previousFileID = -1;

        public IEnumerable<NeuQuantFeatureSet> ExtractFeatureSets(NeuQuantPeptide peptide, int numberOfIsotopes, double minimumSN = 0, double maximumSN = double.MaxValue)
        {
            // Grab the smallest and biggest mass for this peptide (clusters included)
            // QuantifiableChannels is a sorted list, so index 0 is the lightest and index Count-1 is the heaviest.
            double smallestMass = peptide.QuantifiableChannels.Values[0].MonoisotopicMass;
            double biggestMass = peptide.QuantifiableChannels.Values[peptide.QuantifiableChannels.Count - 1].MonoisotopicMass + Constants.C13C12Difference * NumberOfIsotopesToQuantify;

            // Group the psms by which rawfile it was ID in, this is a memory optimization
            var fileGroups = peptide.PeptideSpectrumMatches.GroupBy(psm => psm.RawFile);

            // Iterate over each rawfile group of psms
            foreach (var fileGroup in fileGroups)
            {
                // Get the rawfile for the group
                ThermoRawFile rawFile = fileGroup.Key;

                // Read the fileID from the NeuQuant file
                long fileID = NqFile.SelectFile(rawFile.FilePath);

                // Was this rawfile the last one loaded? if not, load it in memory
                if (fileID != _previousFileID)
                {
                    // Save the ID number
                    _previousFileID = fileID;

                    // Grab all the spectra (RT between 0 and double.Max) for the fileID
                    var tempSpectra = NqFile.GetSpectra(0, double.MaxValue, fileID, 1, MinimumResolution);

                    // Filter the spectra to contain peaks within a S/N bounds
                    _allSpectra = Filter(tempSpectra, minimumSN, maximumSN).ToArray();

                    // Extract all the RT from the spectra and save to an array, optimization for searching
                    _times = _allSpectra.Select(p => p.RetentionTime).ToArray();
                }

                // Group all the PSMs in this rawfile by charge state
                var chargeGroups = fileGroup.GroupBy(psm => psm.Charge);

                // Iterate over each charge state
                foreach (var chargeGroup in chargeGroups)
                {
                    // Get the charge state for this group of PSMs
                    int chargeState = chargeGroup.Key;

                    // Construct a m/z range for the given charge state based on the smallest/biggest masses, plus a light wiggle room
                    MzRange range = new MzRange(Mass.MzFromMass(smallestMass, chargeState) - 0.1, Mass.MzFromMass(biggestMass, chargeState) + 0.1);

                    // Storege for the min and max RTs;
                    double minTime = double.MaxValue;
                    double maxTime = 0;
                    double bestScore = double.MaxValue;
                    PeptideSpectrumMatch bestPSM = null;

                    // Loop over each PSMs in this group
                    foreach (var psm in chargeGroup)
                    {
                        // Find the biggest and smallest RT
                        double rt = psm.RetentionTime;
                        if (rt < minTime)
                        {
                            minTime = rt;
                        }

                        if (rt > maxTime)
                        {
                            maxTime = rt;
                        }

                        double score = psm.MatchScore;
                        if (score < bestScore)
                        {
                            bestPSM = psm;
                            bestScore = score;
                        }
                    }

                    // Check if the range of times is larger than is permitted
                    if (maxTime - minTime > MaximumRtRangePerFeature)
                    {
                        minTime = bestPSM.RetentionTime - (MaximumRtRangePerFeature / 2) + MinimumRtDelta;
                        maxTime = bestPSM.RetentionTime + (MaximumRtRangePerFeature / 2) - MaximumRtDelta;
                    }

                    // Grab all the spectra within the RT bounds, +/- the RT deltas
                    var spectraInTime = GetSpectra(_allSpectra, _times, minTime - MinimumRtDelta, maxTime + MaximumRtDelta);

                    // Shrink the spectra to only contain the m/z range of possible interest, memory optimization
                    List<NeuQuantSpectrum> spectra = ShrinkSpectra(spectraInTime, range).ToList();

                    // Contruct a new feature set that groups the peptide, charge state, PSMs in the charge state, the raw file, MS spectra together
                    NeuQuantFeatureSet featureSet = new NeuQuantFeatureSet(peptide, chargeState, chargeGroup, rawFile, spectra, numberOfIsotopes);

                    // Save the new feature set to be analyze in the next step
                    yield return featureSet;
                }
            }
        }

        public List<NeuQuantFeatureSet> ExtractFeatureSets()
        {
            return ExtractFeatureSets(NumberOfIsotopesToQuantify, MinimumSN, MaximumSN);
        }

        public List<NeuQuantFeatureSet> ExtractFeatureSets(int numberOfIsotopes, double minimumSN = 0, double maximumSN = double.MaxValue)
        {
            OnMessage("Extracting Features...");
            OnProgress(0);
            FeatureSets = new List<NeuQuantFeatureSet>();

            int count = 0;

            // Loop over each quantifiable petpide
            foreach (var peptide in QuantifiablePeptides)
            {
                FeatureSets.AddRange(ExtractFeatureSets(peptide, numberOfIsotopes, minimumSN, maximumSN));

                // For progress feedback
                count++;
                if (count % 100 == 0)
                {
                    OnProgress((double) count/QuantifiablePeptides.Count);
                }
            }
            return FeatureSets;
        }

        private static IEnumerable<NeuQuantSpectrum> GetSpectra(NeuQuantSpectrum[] spectra, double[] times, double mintime, double maxTime)
        {
            int index = Array.BinarySearch(times, mintime);
            if (index < 0)
                index = ~index;

            int upperIndex = Array.BinarySearch(times, maxTime);
            if (upperIndex < 0)
                upperIndex = ~upperIndex;

            for (int i = index; i < upperIndex; i++)
            {
                yield return spectra[i];
            }
        }
        
        private static IEnumerable<NeuQuantSpectrum> ShrinkSpectra(IEnumerable<NeuQuantSpectrum> spectra, MzRange mzRange)
        {
            return spectra.Select(spectrum => spectrum.Extract(mzRange)).Where(nqSpectrum => nqSpectrum != null);
        }

        private static IEnumerable<NeuQuantSpectrum> Filter(IEnumerable<NeuQuantSpectrum> spectra, double minSN = 0, double maxSN = double.MaxValue)
        {
            return spectra.Select(spectrum => spectrum.Filter(minSN, maxSN));
        }
    }
}

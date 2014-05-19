using System;
using System.Collections.Generic;
using CSMSL;
using CSMSL.Chemistry;
using CSMSL.Proteomics;
using CSMSL.Spectral;
using System.Linq;

namespace NeuQuant
{
    public class NeuQuantFeature : IComparable<NeuQuantFeature>
    {
        // The set this feature belongs too
        public NeuQuantFeatureSet ParentSet { get; private set; }

        // The spectrum associated with this feature (mini spectrum, not full mass range)
        public NeuQuantSpectrum Spectrum { get; private set; }

        // Each isotope to this feature (monoisotopic = 0, m+1 = 1, etc..)
        private readonly Dictionary<Peptide, IPeak[]> _isotopes;

        // Flag enum for which isotopes are valid
        private int _usedIsotopes = 0;

        // Flag enum for which isotopes have correct spacings
        private int _spacingIsotopes = 0;

        public int ChargeState { get { return ParentSet.ChargeState; } }
        public int NumberOfIsotopes {get { return ParentSet.NumberOfIsotopes; } }
        public NeuQuantPeptide Peptide { get { return ParentSet.Peptide; } }
        public double RetentionTime { get { return Spectrum.RetentionTime; } }
        public bool ContainsData { get { return _isotopes.Count > 0; } }
        public int ChannelsPresent { get { return _isotopes.Count; } }
       
        public int ValidIsotopes { 
            get
            {
                int count = 0;
                for (int i = 0; i < NumberOfIsotopes; i++)
                {
                    if (IsIsotopeValid(i))
                        count++;
                }
                return count;
            } 
        }

        public int TotalIsotopesDetected
        {
            get { return _isotopes.Values.Sum(peaks => peaks.Count(p => p != null)); }
        }

        public NeuQuantFeature(NeuQuantFeatureSet parent, NeuQuantSpectrum spectrum)
        {
            ParentSet = parent;
            Spectrum = spectrum;
            _isotopes = new Dictionary<Peptide, IPeak[]>();
        }

        public bool AddChannel(Peptide peptide, IPeak peak, int isotope = 0)
        {
            if (peak == null) 
                return false;

            IPeak[] isotopes = null;

            // Get the array for this channel, if not create it and store it
            if (!_isotopes.TryGetValue(peptide, out isotopes))
            {
                isotopes = new IPeak[NumberOfIsotopes];
                _isotopes.Add(peptide, isotopes);
            }

            // Save the peak in the correct spot for the isotopes
            isotopes[isotope] = peak;
            return true;
        }

        public bool IsChannelPresent(Peptide channel)
        {
            return _isotopes.ContainsKey(channel);
        }

        public int IsotopesDetected(Peptide channel)
        {
            IPeak[] isotopes;
            return !_isotopes.TryGetValue(channel, out isotopes) ? 0 : isotopes.Count(p => p != null);
        }
       
        public double GetTotalIntensity()
        {
            return _isotopes.Values.SelectMany(iso => iso).Sum(peak => (peak != null) ? peak.Y : 0);
        }

        public double GetTotalIntensity(int isotope)
        {
            return _isotopes.Values.Select(iso => iso[isotope]).Sum(peak => (peak != null) ? peak.Y : 0);
        }

        public double GetChannelIntensity(Peptide peptide, bool ignoreChecks = false)
        {
            IPeak[] peaks;
            if (!_isotopes.TryGetValue(peptide, out peaks))
                return 0;

            double totalIntensity = 0;
            for (int i = 0; i < NumberOfIsotopes; i++)
            {
                if (peaks[i] == null)
                    continue;

                if (ignoreChecks || IsIsotopeValid(i))
                    totalIntensity += peaks[i].Y;
            }

            return totalIntensity;
        }

        public IEnumerable<Tuple<double, double>> GetChannelMassErrorAndItensity(Peptide peptide, bool completeOnly = true, bool noiseBandCap = true, double noiseLevel = 3)
        {
            IPeak[] peaks;
            if (!_isotopes.TryGetValue(peptide, out peaks))
            {
                 yield break;
            }

            for (int i = 0; i < NumberOfIsotopes; i++)
            {
                // Skip if the isotope is not complete (i.e. has missing channels)
                if (completeOnly && !IsIsotopeComplete(i))
                {
                    continue;
                }
                
                // Skip if the isotope is not valid (i.e. == 0)
                if (!IsIsotopeValid(i))
                {
                    continue;
                }

                IPeak peak = peaks[i];
                if (peak == null)
                {
                    //if (noiseBandCap)
                    //    yield return new Tuple<double, double>(double.NaN, noiseLevel);
                }
                else
                {
                    double mz = peptide.ToMz(ChargeState, i);
                    double ppm = Tolerance.GetTolerance(peak.X, mz, ToleranceType.PPM);
                    yield return new Tuple<double, double>(ppm, peak.Y);
                }
            }          
        }

        public IEnumerable<Tuple<double, double, int>> GetChannelMassErrorAndItensityAndIsotope(Peptide peptide, bool completeOnly = true, bool noiseBandCap = true, double noiseLevel = 3)
        {
            IPeak[] peaks;
            if (!_isotopes.TryGetValue(peptide, out peaks))
            {
                yield break;
            }

            for (int i = 0; i < NumberOfIsotopes; i++)
            {
                // Skip if the isotope is not complete (i.e. has missing channels)
                if (completeOnly && !IsIsotopeComplete(i))
                {
                    continue;
                }

                // Skip if the isotope is not valid (i.e. == 0)
                if (!IsIsotopeValid(i))
                {
                    continue;
                }

                IPeak peak = peaks[i];
                if (peak == null)
                {
                    //if (noiseBandCap)
                    //    yield return new Tuple<double, double>(double.NaN, noiseLevel);
                }
                else
                {
                    double mz = peptide.ToMz(ChargeState, i);
                    double ppm = Tolerance.GetTolerance(peak.X, mz, ToleranceType.PPM);
                    yield return new Tuple<double, double, int>(ppm, peak.Y, i);
                }
            }
        }

        public double GetChannelIntensity(Peptide peptide, int isotope)
        {
            return _isotopes[peptide][isotope].Y;
        }

        public IPeak GetChannelpeak(Peptide peptide, int isotope)
        {
            if (_isotopes == null || isotope >= ParentSet.NumberOfIsotopes)
                return null;

            IPeak[] peaks;
            if (!_isotopes.TryGetValue(peptide, out peaks))
                return null;
            
            return peaks[isotope];
        }

        public void CheckIsotopicDistribution(int numberOfExpectedChannels, double percentError = 0.25, bool performCheck = true)
        {
            // If only one isotope, or skipping check, mark all isotopes as valid
            if (NumberOfIsotopes == 1 || !performCheck)
            {
                // Handy way to mark all isotopes valid
                _usedIsotopes = (1 << NumberOfIsotopes + 1) - 1;
                return;
            }

            // No isotopes contain any data
            if (!ContainsData)
            {
                return;
            }
            
            // Get the maximum signal for each isotope
            double[] maxSignals = new double[NumberOfIsotopes];

            // Find the maximum signal among all channels for each isotope
            for (int i = 0; i < NumberOfIsotopes; i++)
            {
                double maxValue = 0;
                int nonNull = 0;
                foreach (IPeak[] array in _isotopes.Values)
                {
                    IPeak peak = array[i];
                    if (peak == null) 
                        continue;

                    nonNull++;
                    if(peak.Y > maxValue)
                        maxValue = peak.Y;
                }
                maxSignals[i] = maxValue;

                if (i == 0 && nonNull == numberOfExpectedChannels)
                {
                    // Use the monoisotopic channel if all channels have it
                    _usedIsotopes |= 1;
                }
            }

            // Get the monoisotopic intensity
            double monoIntensity = maxSignals[0];

            if (monoIntensity <= 0)
            {
                // Monoisotopic peak not found, assume distributiion is incorrect
                // TODO add logic here in case the monoisotopic peak is below s/n threshold
                return;
            }

            // Get the chemical formula of one of the channels (should be close enough)
            ChemicalFormula formula = Peptide.QuantifiableChannels.Values[0].GetChemicalFormula();

            // Adjust the percent errors
            double minPercent = 1 - percentError;
            double maxPercent = 1 + percentError;

            // Get the c13 isotopic distribution, good approximation
            double[] distribution = formula.GetIsotopicDistribution(NumberOfIsotopes);

            // Loop over each other isotope
            for (int c13 = 1; c13 < NumberOfIsotopes; c13++)
            {
                // The signal from this isotope
                double signal = maxSignals[c13];

                // Not present, don't use
                if (signal <= 0)
                {
                    continue;
                }

                // Calculate the realtive abundance based on the monoisotopic intensity
                double relativeAbundance = monoIntensity * distribution[c13];
                double min = relativeAbundance*minPercent;
                double max = relativeAbundance*maxPercent;

                if (signal >= min && signal <= max)
                {
                    // Within the bounds, use for quantitation (also include monoisotopic)
                    _usedIsotopes |= (1 << c13);
                    _usedIsotopes |= 1;
                }
            }
        }

        private bool IsIsotopeValid(int isotope)
        {
            int mask = (1 << isotope);
            return (_usedIsotopes & mask) != 0 && (_spacingIsotopes & mask) != 0;
        }

        private bool IsIsotopeComplete(int isotope)
        {
            if (ChannelsPresent != ParentSet.Peptide.NumberOfChannels) return false;
            int count = 0;
            foreach (IPeak[] peaks in _isotopes.Values)
            {
                if (peaks[isotope] != null) count++;
            }
            return count == ParentSet.Peptide.NumberOfChannels;
        }

        public int AssignPeaks(NeuQuantSpectrum spectrum, Peptide channel, int isotope, DoubleRange mzRange)
        {
            // Cannot assign what does not exist
            if (spectrum == null || spectrum.Count == 0)
                return 0;
            
            IPeak peak = spectrum.GetClosestPeak(mzRange);

            if (AddChannel(channel, peak, isotope))
            {
                _spacingIsotopes |= (1 << isotope);
            }

            return 1;
        }

        /// <summary>
        /// Tries to assigns the peaks in the spectrum to the correct channels.
        /// </summary>
        /// <param name="spectrum">The collection of peaks to assign</param>
        /// <param name="channels">The quantitative channels (aka peptides) to assign peaks too</param>
        /// <param name="expectedSpacings">The expected spacing in Th</param>
        /// <param name="isotope">The isotope of consideration</param>
        /// <param name="maxPPM">The maximum ppm allowance for assigning a peak</param>
        /// <returns>The number of channels that were mapped to peaks</returns>
        public int AssignPeaks(NeuQuantSpectrum spectrum, IList<Peptide> channels, double[] expectedSpacings, int isotope, Tolerance maxTolerance, Tolerance correctSpacingTolerance, double lowerPercent, double higherPercent)
        {
            // Cannot assign what does not exist
            if (spectrum == null || spectrum.Count == 0)
                return 0;

            // Only one peak, assigned based on smallest ppm error
            // TODO How to handle 2 out of 3 peaks found for 3plex or 4plex
            if (spectrum.Count == 1)
            {
                // Try to assign the only peak by ppm
                return AssignPeakByPPM(spectrum.GetPeak(0), channels, isotope, maxTolerance, correctSpacingTolerance) ? 1 : 0;
            }
            
            // Check peaks on spacing
            int channelsAssigned = CheckPeakSpacing(spectrum, channels, expectedSpacings, isotope, maxTolerance, correctSpacingTolerance, lowerPercent, higherPercent);

            // Mark that the spacing for this isotope is correct if all the channels were assigned
            if (channelsAssigned == channels.Count)
            {
                _spacingIsotopes |= (1 << isotope);
            }

            // TODO How to mark isotopes with incomplete sets of peaks that have correct spacing

            return channelsAssigned;
        }

        private bool AssignPeakByPPM(IPeak peak, IEnumerable<Peptide> channels, int isotope, Tolerance maxTolerance, Tolerance correctSpacingTolerance)
        {
            // The m/z of the peak
            double mz = peak.X;

            // Find the smallest Th difference for this peak
            double bestTh = double.MaxValue;
            Peptide bestPeptide = null;
            foreach (Peptide channel in channels)
            {
                double channelMZ = channel.ToMz(ChargeState, isotope);
                
                // Calculate the absolute Th error
                double th = Math.Abs(channelMZ - mz);

                // Not the smallest error, so continue
                if (th > bestTh)
                    continue;
                
                // Is the smallest error, so save this Th error as the best
                bestTh = th;

                // Save this channel as the best channel
                bestPeptide = channel;
            }

            // Check if the peaks are within the specified tolerances
            if (!maxTolerance.Within(mz + bestTh, mz))
                return false;

            if (correctSpacingTolerance.Within(mz + bestTh, mz))
                _spacingIsotopes |= (1 << isotope);

            // Try to assign the channel (bestPeptide) to this peak
            return AddChannel(bestPeptide, peak, isotope);
        }

        private int CheckPeakSpacing(NeuQuantSpectrum spectrum, IList<Peptide> channels, double[] expectedSpacings, int isotope, Tolerance maxTolerance, Tolerance correctSpacingTolerance, double lowerPercent = 0.25, double higherPercent = 0.15)
        {
            // Adjust spacing percents, this enables asymmetrical bounds, if wanted
            lowerPercent = 1 - lowerPercent;
            higherPercent = 1 + higherPercent;

            #region Check all peaks for proper spacing
            
            // The peak indices that pass spacing checks
            HashSet<int> spacingPeaksPassed = new HashSet<int>();

            // Loop over all the expected spacings
            foreach (double expectedSpacing in expectedSpacings)
            {
                // TODO figure out multiple spacings

                // Calculate the spacing tolerances
                double minSpacing = expectedSpacing * lowerPercent;
                double maxSpacing = expectedSpacing * higherPercent;

                // Loop over each peak in the spectrum, save the last one
                for (int i = 0; i < spectrum.Count - 1; i++)
                {
                    // Get the m/z of the peak
                    double mzi = spectrum.GetMass(i);

                    // Loop over every other peak in the spectrum, skipping the one we are currently on
                    for (int j = i + 1; j < spectrum.Count; j++)
                    {
                        // Get the m/z of the other peak
                        double mzj = spectrum.GetMass(j);

                        // Calculate the spacing between the two m/z (should always be positive)
                        double spacing = mzj - mzi;

                        // Check the spacing for the allowed spacing bounds
                        if (spacing < minSpacing || spacing > maxSpacing)
                            continue;

                        // Add the two peaks if the spacings are within the bounds
                        spacingPeaksPassed.Add(i);
                        spacingPeaksPassed.Add(j);
                    }
                }
            }

            #endregion

            // Number of peaks that pass spacings
            int count = spacingPeaksPassed.Count();

            // No peaks passed the spacing, try to save the most intense
            if (count == 0)
            {
                // TODO How to flag these for later to make sure they are not used for quant

                return AssignPeakByPPM(spectrum.GetBasePeak(), channels, isotope, maxTolerance, correctSpacingTolerance) ? 1 : 0;
            }

            // Counter for the number of channels that were assigned a peak
            int numberOfChannelsAssigned = 0;

            // Yay! the right number of peaks passed spacing, 
            if (count == channels.Count)
            {
                int channelIndex = 0;
              
                // This assumes a 1-to-1 mapping of peak to channel, which should usually be correct
                numberOfChannelsAssigned += spacingPeaksPassed.OrderBy(k => k).Count(index => AddChannel(channels[channelIndex++], spectrum.GetPeak(index), isotope));
            }
            // filter on PPM error
            else
            {
                // Loop over each channel and try to find the best ppm
                foreach (Peptide channel in channels)
                {
                    double channelMZ = channel.ToMz(ChargeState, isotope);
                    double minTh = double.MaxValue;
                    int bestIndex = -1;
                    foreach (int index in spacingPeaksPassed)
                    {
                        double expmz = spectrum.GetMass(index);
                        double spacing = Math.Abs(expmz - channelMZ);
                        if (spacing < minTh)
                        {
                            minTh = spacing;
                            bestIndex = index;
                        }
                    }
                    
                    // Check if the peaks are within the specified tolerances
                    if (!maxTolerance.Within(channelMZ + minTh, channelMZ))
                        continue;
                   
                    // Remove this peak from being assigned to another channel
                    spacingPeaksPassed.Remove(bestIndex);

                    // Add the peak to the channel for this isotope
                    if (AddChannel(channel, spectrum.GetPeak(bestIndex), isotope))
                        numberOfChannelsAssigned++;
                }
            }

            // Return the number of channels that were assigned
            return numberOfChannelsAssigned;
        }

        public void EliminatePeak(Peptide channel, int isotope)
        {
            _isotopes[channel][isotope] = null;
        }
        
        public override string ToString()
        {
            return string.Format("RT: {0:F4} Total Intensity: {1:F4} Channels Detected: {2} Isotopes Detected: {3} Usuable Isotopes: {4}", RetentionTime, GetTotalIntensity(), ChannelsPresent, TotalIsotopesDetected, ValidIsotopes);
        }

        public int CompareTo(NeuQuantFeature other)
        {
            return RetentionTime.CompareTo(other.RetentionTime);
        }
    }
}

using CSMSL.Analysis.ExperimentalDesign;
using CSMSL.Proteomics;
using System;
using System.Collections.Generic;

namespace NeuQuant
{
    public class NeuQuantPeptide
    {
        /// <summary>
        /// The spacing between two peptides that define a cluster
        /// </summary>
        private const int DaSpacingToDefineCluster = 1;

        /// <summary>
        /// The Peptide Spectral Matches of this Peptide
        /// </summary>
        public List<PeptideSpectrumMatch> PeptideSpectrumMatches;

        /// <summary>
        /// The best PSM of this Peptide
        /// </summary>
        public PeptideSpectrumMatch BestPeptideSpectrumMatch;
        
        /// <summary>
        /// All the idenfied charge states of this peptide
        /// </summary>
        public HashSet<int> IdentifiedChargeStates { get; private set; }

        /// <summary>
        /// The amino acid polymer of this peptide
        /// </summary>
        public Peptide Peptide { get; private set; }

        /// <summary>
        /// All of the quantifiable channels of this peptide, sorted on monoisotopic mass
        /// </summary>
        public SortedList<double, Peptide> QuantifiableChannels;

        /// <summary>
        /// All the clusters of this peptide, sorted on monoisotopic mass, index by cluster
        /// </summary>
        public SortedList<double, Peptide>[] Clusters;

        /// <summary>
        /// All the features of this peptide, broken down by charge state and raw file
        /// </summary>
        public List<NeuQuantFeatureSet> FeatureSets;

        /// <summary>
        /// The smallest spacing between any two quantifiable channels of this peptide (in Da).
        /// This defines the minimum resolution needed to resolve all the channels of this peptide.
        /// </summary>
        public double SmallestTheorecticalMassSpacing { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public double BiggestThSpacing { get; private set; }

        /// <summary>
        /// The number of Peptide Spectrum Matches this Peptide has
        /// </summary>
        public int NumberOfPeptideSpectrumMatches { get { return PeptideSpectrumMatches.Count; } }

        /// <summary>
        /// The Amino Acid Sequence of this Peptide
        /// </summary>
        public string Sequence { get { return (Peptide == null) ? "" : Peptide.Sequence; } }

        /// <summary>
        /// Indicates if this Peptide contains a quantitative channel
        /// </summary>
        public bool ContainsQuantitativeChannel { get { return NumberOfChannels > 1; } }

        /// <summary>
        /// Indicates if this Peptide contains more than one cluster
        /// </summary>
        public bool ContainsMultipleClusters { get { return NumberOfClusters > 1; } }

        /// <summary>
        /// Indicates if this Peptide contains isotopologues of small spacing
        /// </summary>
        public bool ContainsIsotopologue { get { return SmallestTheorecticalMassSpacing < DaSpacingToDefineCluster; } }

        /// <summary>
        /// The number of quantitatifable channels
        /// </summary>
        public int NumberOfChannels { get { return QuantifiableChannels.Count; } }

        /// <summary>
        /// The number of clusters
        /// </summary>
        public int NumberOfClusters { get { return Clusters.Length; } }

        private Dictionary<Peptide, NeuQuantSample> channelsToSamples; 

        public NeuQuantPeptide()
        {
            IdentifiedChargeStates = new HashSet<int>();
            PeptideSpectrumMatches = new List<PeptideSpectrumMatch>();
        }
        
        /// <summary>
        /// Attempts to add a peptide spectrum match to this peptide.
        /// </summary>
        /// <param name="psm">The psm to add to this peptide</param>
        /// <returns>True if the psm was added, false otherwise</returns>
        public bool AddPeptideSpectrumMatch(PeptideSpectrumMatch psm, IList<NeuQuantSample> samples)
        {
            if (QuantifiableChannels == null)
                QuantifiableChannels = new SortedList<double, Peptide>(samples.Count);

            if(channelsToSamples == null)
                channelsToSamples = new Dictionary<Peptide, NeuQuantSample>(samples.Count);

            if (Peptide != null && !Peptide.Equals(psm.Peptide))
                return false;
            
            // Record the PSM and Charge
            PeptideSpectrumMatches.Add(psm);
            IdentifiedChargeStates.Add(psm.Charge);

            // Is this the first psm? if so, set up the quantifiable channels
            if (PeptideSpectrumMatches.Count == 1)
            {
                Peptide = psm.Peptide;
                SetQuantChannels(Peptide, samples);
            }

            // Find the best PSM
            if (BestPeptideSpectrumMatch == null)
            {
                BestPeptideSpectrumMatch = psm;
                BiggestThSpacing = SmallestTheorecticalMassSpacing / Math.Abs(psm.Charge);
            }
            else
            {
                if (psm.Charge <= BestPeptideSpectrumMatch.Charge)
                {
                    BiggestThSpacing = SmallestTheorecticalMassSpacing / Math.Abs(psm.Charge);

                    if (psm.MatchType != BestPeptideSpectrumMatch.MatchType)
                    {
                        throw new ArgumentException("Cannot compare peptide spectral matches of different score types!");
                    }

                    // What way to compare
                    int direction = Math.Sign((int)BestPeptideSpectrumMatch.MatchType);

                    // Make the comparsion
                    int comp = psm.MatchScore.CompareTo(BestPeptideSpectrumMatch.MatchScore);

                    // If the direction and comparison are the same sign, then the psm is a better match
                    if (direction == comp)
                        BestPeptideSpectrumMatch = psm;
                }
            }

            return true;
        }

        private void SetQuantChannels(Peptide peptide, IEnumerable<NeuQuantSample> samples)
        {
            foreach (NeuQuantSample sample in samples)
            {
                Peptide channel = new Peptide(peptide, true);
                foreach (Modification mod in sample.Condition)
                {
                    channel.AddModification(mod);
                }

                if (!QuantifiableChannels.ContainsKey(channel.MonoisotopicMass))
                {
                    QuantifiableChannels.Add(channel.MonoisotopicMass, channel);
                    channelsToSamples.Add(channel, sample);
                }
            }

            // Generate each isotopologue from the peptide and add it to the list
            //foreach (var isotopologue in peptide.GenerateIsotopologues())
            //{
            //    QuantifiableChannels.Add(isotopologue.MonoisotopicMass, isotopologue);
            //}

            // No quantitative labels, i.e. only one isoform for this peptide
            if (QuantifiableChannels.Count == 1)
            {
                SmallestTheorecticalMassSpacing = 0;
                return;
            }

            var clusters = new List<SortedList<double, Peptide>>();
            var peptidesPerCluster = new SortedList<double, Peptide>();
            clusters.Add(peptidesPerCluster);
            double minSpacing = double.MaxValue;
        
            double previousMass = QuantifiableChannels.Keys[0];
            peptidesPerCluster.Add(previousMass, QuantifiableChannels.Values[0]);
            for (int i = 1; i < QuantifiableChannels.Count; i++)
            {
                double currentMass = QuantifiableChannels.Keys[i];

                // Find the smallest spacing between any two isotopologues
                double spacing = currentMass - previousMass;
                if (spacing < minSpacing)
                {
                    minSpacing = spacing;
                }

                // Check for clusters (defined as bigger than 1 Da in mass difference)
                if (spacing > DaSpacingToDefineCluster)
                {
                    // Need a new container for the next cluster
                    peptidesPerCluster = new SortedList<double, Peptide>();

                    // Save the new cluster
                    clusters.Add(peptidesPerCluster);
                }

                //Add the channel to the current cluster
                peptidesPerCluster.Add(currentMass, QuantifiableChannels.Values[i]);
                previousMass = currentMass;
            }

            // Convert the list of clusters into an array
            Clusters = clusters.ToArray();
            SmallestTheorecticalMassSpacing = minSpacing;
        }

        public void AddFeatureSet(NeuQuantFeatureSet featureSet)
        {
            if (FeatureSets == null)
            {
                FeatureSets = new List<NeuQuantFeatureSet>();
            }

            FeatureSets.Add(featureSet);
        }
        
        public override string ToString()
        {
            return Peptide.ToString();
        }

        public NeuQuantQuantitation Quantify(bool noiseBandCap, double noiseLevel)
        {
            NeuQuantQuantitation quant = new NeuQuantQuantitation(this, channelsToSamples.Count);

            foreach (var featureSet in FeatureSets)
            {
                Dictionary<Peptide, double> featureSetQuant = featureSet.Quantify(noiseBandCap, noiseLevel);
                foreach (KeyValuePair<Peptide, double> channelQuant in featureSetQuant)
                {
                    var channel = channelQuant.Key;
                    double quantitationValue = channelQuant.Value;

                    if (noiseBandCap)
                        quantitationValue = Math.Max(noiseLevel, quantitationValue);

                    NeuQuantSample sample = channelsToSamples[channel];
                    quant.AddQuantitation(sample, quantitationValue);
                }
            }

            return quant;
        }
       
    }
}

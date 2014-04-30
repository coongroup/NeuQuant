using CSMSL.Proteomics;
using System;
using System.Collections.Generic;

namespace NeuQuant
{
    public class NeuQuantPeptide
    {
        private const int DaSpacingToDefineCluster = 1;

        public List<PeptideSpectrumMatch> PeptideSpectrumMatches;
        public PeptideSpectrumMatch BestPeptideSpectrumMatch;
        public HashSet<int> IdentifiedChargeStates;
        public Peptide Peptide { get; private set; }
        public SortedList<double, Peptide> QuantifiableChannels;
        public SortedList<double, Peptide>[] Clusters;
        public List<NeuQuantFeatureSet> FeatureSets;

        public double SmallestTheorecticalMassSpacing { get; private set; }
        public double BiggestThSpacing { get; private set; }

        public int NumberOfPeptideSpectrumMatches { get { return PeptideSpectrumMatches.Count; } }
        public string Sequence { get { return Peptide.Sequence; } }
        public bool ContainsQuantitativeChannel { get { return NumberOfChannels > 1; } }
        public bool ContainsClusters { get { return NumberOfClusters > 1; } }
        public bool ContainsIsotopologue { get { return SmallestTheorecticalMassSpacing < DaSpacingToDefineCluster; } }
        public int NumberOfChannels { get { return QuantifiableChannels.Count; } }
        public int NumberOfClusters { get { return Clusters.Length; } }
        
        public NeuQuantPeptide()
        {
            IdentifiedChargeStates = new HashSet<int>();
            PeptideSpectrumMatches = new List<PeptideSpectrumMatch>();
            QuantifiableChannels = new SortedList<double, Peptide>();
        }

        public NeuQuantPeptide(PeptideSpectrumMatch psm)
            : this()
        {
            AddPeptideSpectrumMatch(psm);
        }

        public bool AddPeptideSpectrumMatch(PeptideSpectrumMatch psm)
        {
            if (Peptide != null && !Peptide.Equals(psm.Peptide))
                return false;
            
            if (PeptideSpectrumMatches.Count == 0)
            {
                Peptide = psm.Peptide;
                SetQuantChannels(Peptide);
            }
            
            // Record the PSM and Charge
            PeptideSpectrumMatches.Add(psm);
            IdentifiedChargeStates.Add(psm.Charge);

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

                    //TODO add logic for increasing match score
                    if (psm.MatchScore < BestPeptideSpectrumMatch.MatchScore)
                        BestPeptideSpectrumMatch = psm;
                }
            }

            return true;
        }

        private void SetQuantChannels(Peptide peptide)
        {
            // Generate each isotopologue from the peptide and add it to the list
            foreach (var isotopologue in peptide.GenerateIsotopologues())
            {
                QuantifiableChannels.Add(isotopologue.MonoisotopicMass, isotopologue);
            }

            // No quantitative labels
            if (QuantifiableChannels.Count == 0)
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
    }
}

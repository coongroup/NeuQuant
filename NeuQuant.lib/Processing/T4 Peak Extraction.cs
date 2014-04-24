using System.Linq;
using CSMSL;

namespace NeuQuant.Processing
{
    public partial class Processor
    {
        public void FindPeaks()
        {
            OnMessage("Finding Peaks...");
            OnProgress(0);
            int count = 0;

            double systematicError = 0;
            //foreach (var peptide in AllPeptides.Where(peptide => !peptide.ContainsQuantitativeChannel))
            //{
               
            //}

            foreach (NeuQuantFeatureSet featureSet in FeatureSets)
            {
                featureSet.FindPeaks(MS2Tolerance, NumberOfIsotopesToQuantify, systematicError, UseIsotopicDistribution, IsotopicDistributionPercentError);
                
                featureSet.FindElutionProfile(3);

                count++;
                if (count % 100 == 0)
                {
                    OnProgress((double)count / FeatureSets.Count);
                }
            }
        }

    }
}

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

            foreach (NeuQuantFeatureSet featureSet in FeatureSets)
            {
                FindPeaks(featureSet);
                
                if (++count % 100 == 0)
                {
                    OnProgress((double)count / FeatureSets.Count);
                }
            }
        }

        public void FindPeaks(NeuQuantFeatureSet featureSet)
        {
            featureSet.FindPeaks(MS2Tolerance, NumberOfIsotopesToQuantify, SystematicPPMError, UseIsotopicDistribution, IsotopicDistributionPercentError, LowerSpacingPercent, UpperSpacingPercent);
            featureSet.FindElutionProfile(1);
        }
        
    }
}

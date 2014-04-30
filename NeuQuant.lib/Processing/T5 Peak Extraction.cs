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
                featureSet.FindPeaks(MS2Tolerance, NumberOfIsotopesToQuantify, SystematicError, UseIsotopicDistribution, IsotopicDistributionPercentError);
                
                featureSet.FindElutionProfile(3);
                
                if (++count % 100 == 0)
                {
                    OnProgress((double)count / FeatureSets.Count);
                }
            }
        }
        
    }
}

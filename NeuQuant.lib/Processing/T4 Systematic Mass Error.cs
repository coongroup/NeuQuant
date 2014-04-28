using System.Collections.Generic;
using System.Linq;
using CSMSL;

namespace NeuQuant.Processing
{
    public partial class Processor
    {
        public enum SystematicErrorType
        {
            Mean,
            Median
        }

        public double SystematicError { get; private set; }

        public double CalculateSystematicError()
        {
            OnMessage("Calculating Systematic Error...");
            SystematicError = CalculateSystematicError(FeatureSets);
            return SystematicError;
        }

        public static double CalculateSystematicError(IEnumerable<NeuQuantFeatureSet> features, SystematicErrorType type = SystematicErrorType.Median)
        {
            List<double> massErrors = new List<double>();
            foreach (var feature in features)
            {
                // Only use features without labels
                //if(feature.Peptide.ContainsQuantitativeChannel)
                //   continue;

                massErrors.AddRange(feature.PrecursorMassError(feature.Peptide.QuantifiableChannels.Values[0]));
            }

            switch (type)
            {
                case SystematicErrorType.Mean:
                    return massErrors.Average();
                default:
                case SystematicErrorType.Median:
                    return massErrors.Median();
            }
        }

    }
}

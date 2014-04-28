using System.Collections.Generic;
using System.IO;
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

        public double SystematicPPMError { get; private set; }

        public double CalculateSystematicError()
        {
            OnMessage("Calculating Systematic Error...");
            SystematicPPMError = CalculateSystematicError(FeatureSets);
            return SystematicPPMError;
        }

        public static double CalculateSystematicError(IEnumerable<NeuQuantFeatureSet> features, int numberOfIsotopes = 1, SystematicErrorType type = SystematicErrorType.Median)
        {
            List<double> massErrors = new List<double>();
            foreach (var feature in features)
            {
                massErrors.AddRange(feature.PrecursorMassError(numberOfIsotopes));
            }

            //using (StreamWriter writer = new StreamWriter(@"E:\Desktop\NeuQuant\2plex NeuCode Charger\ppmErrors.csv"))
            //{
            //    foreach (double massError in massErrors)
            //    {
            //        writer.WriteLine(massError);
            //    }
            //}

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

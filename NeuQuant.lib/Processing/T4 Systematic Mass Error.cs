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
            SystematicError = CalculateSystematicError(AllPeptides.Where(peptide => !peptide.ContainsQuantitativeChannel));
        }

        public static double CalculateSystematicError(IEnumerable<NeuQuantPeptide> peptides, SystematicErrorType type = SystematicErrorType.Median)
        {
            List<double> massErrors = new List<double>();
            foreach (var peptide in peptides)
            {
                
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

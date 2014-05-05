using System.Collections.Generic;

namespace NeuQuant
{
    public class NeuQuantQuantitation
    {
        public NeuQuantPeptide Peptide { get; set; }
       
        public Dictionary<NeuQuantSample, double> Quantitation;

        public NeuQuantQuantitation(NeuQuantPeptide peptide, int numberOfSamples = 2)
        {
            Peptide = peptide;
            Quantitation = new Dictionary<NeuQuantSample, double>(numberOfSamples);
        }

        public void AddQuantitation(NeuQuantSample sample, double value)
        {
            double previousValue = 0;
            if (!Quantitation.TryGetValue(sample, out previousValue))
            {
                Quantitation.Add(sample, value);
            }
            else
            {
                Quantitation[sample] = value + previousValue;
            }
        }

    }
}

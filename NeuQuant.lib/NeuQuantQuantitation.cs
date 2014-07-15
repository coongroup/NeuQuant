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

        public int SamplesQuantified(double minimumValue = 3.0, int numMeasurements = 2)
        {
            int count = 0;
            double minTotalIntensity = minimumValue * numMeasurements * Peptide.FeatureSets.Count;

            foreach (double quant in Quantitation.Values)
            {
                if (quant > minTotalIntensity) count++;
            }

            return count;
        }

    }
}

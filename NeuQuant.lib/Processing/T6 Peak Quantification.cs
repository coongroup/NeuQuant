using System.Collections.Generic;

namespace NeuQuant.Processing
{
    public partial class Processor
    {
        public void QuantifyPeptides()
        {
            QuantifyPeptides(QuantifiablePeptides, NoiseBandCap, MinimumSN);
        }

        public void QuantifyPeptides(IEnumerable<NeuQuantPeptide> peptides, bool noiseBandCap, double noiseLevel)
        {
            OnMessage("Quantifying Peptides...");
            OnProgress(0);

            int count = 0;

            NqFile.BeginTransaction();

            foreach (var peptide in peptides)
            {
                var quant = peptide.Quantify(noiseBandCap, noiseLevel);

                NqFile.InsertQuantitation(this, quant);
                count++;
                if (count % 100 == 0)
                {
                    OnProgress((double)count / FeatureSets.Count);
                }
            }

            NqFile.EndTranscation();
           
            OnProgress(0);
        }

    }
}
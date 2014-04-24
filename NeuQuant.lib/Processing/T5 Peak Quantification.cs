using System;
using System.IO;

namespace NeuQuant.Processing
{
    public partial class Processor
    {
        public void QuantifyPeaks()
        {
            OnMessage("Quantifying Peaks...");
            OnProgress(0);
            int count = 0;
            string baseDir = Path.GetDirectoryName(NqFile.FilePath);

            using (StreamWriter peptideWriter = new StreamWriter(Path.Combine(baseDir, "peptides.csv")))
            using (StreamWriter featureWriter = new StreamWriter(Path.Combine(baseDir, "features.csv")))
            {
                featureWriter.WriteLine("Peptide,Sequence,Z,#PSMS,Channel 1,Channel 2,Log2");
                foreach (NeuQuantFeatureSet featureSet in FeatureSets)
                {
                    var quant = featureSet.Quantify(3);
                    double one = quant[featureSet.Peptide.QuantifiableChannels.Values[0]];
                    double two = quant[featureSet.Peptide.QuantifiableChannels.Values[1]];
                    featureWriter.WriteLine("{0},{1},{2},{3},{4},{5},{6}", featureSet.Peptide.Peptide,featureSet.Peptide.Peptide.Sequence, featureSet.ChargeState,featureSet.PSMs.Count, one, two, Math.Log(two/one, 2));

                    count++;
                    if (count%100 == 0)
                    {
                        OnProgress((double) count/FeatureSets.Count);
                    }
                }

                peptideWriter.WriteLine("Peptide,Sequence,#Features,Channel 1,Channel 2,Log2");
                foreach (var peptide in QuantifiablePeptides)
                {
                    double sumone = 0;
                    double sumtwo = 0;
                    foreach (var featureSet in peptide.FeatureSets)
                    {
                        var quant = featureSet.Quantify(3);
                        sumone += quant[featureSet.Peptide.QuantifiableChannels.Values[0]];
                        sumtwo += quant[featureSet.Peptide.QuantifiableChannels.Values[1]];
                    }
                    peptideWriter.WriteLine("{0},{1},{2},{3},{4},{5}", peptide.Peptide,peptide.Peptide.Sequence, peptide.FeatureSets.Count, sumone, sumtwo, Math.Log(sumtwo / sumone, 2));

                }
            }
           
            OnProgress(0);
        }

    }
}
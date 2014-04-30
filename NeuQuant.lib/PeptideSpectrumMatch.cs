using CSMSL.IO.Thermo;
using CSMSL.Proteomics;

namespace NeuQuant
{
    public class PeptideSpectrumMatch
    {
        public ThermoRawFile RawFile { get; private set; }
        public int SpectrumNumber { get; private set; }
        public double RetentionTime { get; set; }
        public Peptide Peptide { get; set; }
        public string Sequence { get { return Peptide.Sequence; } }
        public string LeucineSequence { get { return Peptide.GetLeucineSequence(); } }
        public double MonoisotopicMass { get { return Peptide.MonoisotopicMass; } }
        public int Charge { get; private set; }
        public double IsolationMZ { get; private set; }
        public double MatchScore { get; private set; }  
        public PeptideSpectrumMatchScoreType MatchType { get; private set; }
        public long PeptideID { get; internal set; }

        public PeptideSpectrumMatch(ThermoRawFile rawFile, int spectrumNumber,double retentionTime, Peptide peptide, int charge, double isoMZ, double score, PeptideSpectrumMatchScoreType scoreType)
        {
            RawFile = rawFile;
            SpectrumNumber = spectrumNumber;
            RetentionTime = retentionTime;
            Peptide = peptide;
            Charge = charge;
            IsolationMZ = isoMZ;
            MatchScore = score;
            MatchType = scoreType;
        }

        public override string ToString()
        {
            return string.Format("{0} RT: {1:F2} Z: {2:N0} Score: {3:G4}", Peptide, RetentionTime, Charge, MatchScore);
        }
    }
}

using CSMSL.IO.Thermo;
using CSMSL.Proteomics;
using CSMSL.Spectral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public PeptideSpectrumMatch(ThermoRawFile rawFile, int spectrumNumber,double retentionTime, Peptide peptide, int charge, double isoMZ, double score)
        {
            RawFile = rawFile;
            SpectrumNumber = spectrumNumber;
            RetentionTime = retentionTime;
            Peptide = peptide;
            Charge = charge;
            IsolationMZ = isoMZ;
            MatchScore = score;
        }
      
    }
}

using CSMSL.IO.Thermo;
using CSMSL.Proteomics;
using NeuQuant.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuQuant
{
    public abstract class PeptideSpectralMatchFile : IDisposable
    {   
        public string FilePath { get; protected set; }
        public string DataDirectory { get; protected set; }
        public virtual string Type { get { return "PSM File"; } } 
                 
        public Dictionary<ThermoRawFile, HashSet<int>> UsedRawFiles;
        public HashSet<Modification> FixedModifications;
        public Dictionary<string, Modification> VariableModifications;
        public Dictionary<string, NeuQuantSample> Samples;

        public int PSMCount { get; protected set; }

        public PeptideSpectrumMatchScoreType ScoreType { get; private set; }

        public bool StoreMS2Spectra { get; set; }

        protected PeptideSpectralMatchFile(string filePath, bool storeMS2Spectra = false, PeptideSpectrumMatchScoreType scoreType = PeptideSpectrumMatchScoreType.Unknown)
        {
            FilePath = filePath;           
            UsedRawFiles = new Dictionary<ThermoRawFile, HashSet<int>>();
            FixedModifications = new HashSet<Modification>();
            VariableModifications = new Dictionary<string, Modification>();
            Samples = new Dictionary<string, NeuQuantSample>();
            ScoreType = scoreType;
            StoreMS2Spectra = storeMS2Spectra;
        }

        public virtual void SetDataDirectory(string directory)
        {
            DataDirectory = directory;
        }

        public virtual void AddFixedModification(Modification modification)
        {
            FixedModifications.Add(modification);
        }

        public virtual void AddVariableModification(string key, Modification modification)
        {
            if (!VariableModifications.ContainsKey(key))
            {
                VariableModifications.Add(key, modification);
            }           
        }

        public void SetChannel(string sampleName, string description, params Modification[] modifications)
        {
            if (Samples.ContainsKey(sampleName))
            {
                throw new ArgumentException("Cannot add two channels with the same name, they must be unique");
            }

            NeuQuantSample sample = new NeuQuantSample(sampleName, description);
            foreach (Modification modification in modifications)
            {
                // TODO add check to make sure the modification was previously added to the fixed/variable mods, will be challenging with isotopologues

                sample.AddModification(modification);
            }
            Samples.Add(sample.Name, sample);
        }

        public void SetChannel(string sampleName, string description, Modification modification)
        {
            SetChannel(sampleName, description, new[] {modification});
        }
        
        public abstract void Open();
        public abstract IEnumerable<PeptideSpectrumMatch> ReadPSMs();
                
        public virtual void Dispose()
        {          
           
        }

    }
}

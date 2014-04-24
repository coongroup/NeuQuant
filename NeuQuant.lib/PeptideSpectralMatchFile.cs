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
        public int PSMCount { get; protected set; }

        protected PeptideSpectralMatchFile(string filePath)
        {
            FilePath = filePath;           
            UsedRawFiles = new Dictionary<ThermoRawFile, HashSet<int>>();
            FixedModifications = new HashSet<Modification>();
            VariableModifications = new Dictionary<string, Modification>();           
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

        public abstract void Open();
        public abstract IEnumerable<PeptideSpectrumMatch> ReadPSMs();
                
        public virtual void Dispose()
        {          
           
        }

    }
}

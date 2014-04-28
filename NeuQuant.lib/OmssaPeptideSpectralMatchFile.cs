using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuQuant.IO;
using LumenWorks.Framework.IO.Csv;
using System.IO;
using CSMSL.IO.Thermo;
using CSMSL.IO.OMSSA;
using CSMSL.Proteomics;
using System.Text.RegularExpressions;

namespace NeuQuant
{
    public class OmssaPeptideSpectralMatchFile : PeptideSpectralMatchFile
    {
        private static Regex _rtRegex = new Regex("RT_(.+)_min", RegexOptions.Compiled);

        private List<PeptideSpectrumMatch> _psms;

        public override string Type
        {
            get
            {
                return "Omssa PSM File";
            }
        }
      
        public OmssaPeptideSpectralMatchFile(string filePath)
            : base(filePath) { }
          

        public void LoadUserMods(string xmlFilePath)
        {
            OmssaModification.LoadOmssaModifications(xmlFilePath);
        }

        public override void Open()
        {
            // Get all the raw files in the data directory
            Dictionary<string, ThermoRawFile> rawFiles = Directory.EnumerateFiles(DataDirectory, "*.raw", SearchOption.TopDirectoryOnly).ToDictionary(file => Path.GetFileNameWithoutExtension(file), file => new ThermoRawFile(file));

            _psms = new List<PeptideSpectrumMatch>();
            // Read all variable mods and store
            using (var reader = new CsvReader(new StreamReader(FilePath), true))
            {
                while (reader.ReadNextRecord())
                {   
                    // Get the data from the file
                    double score = double.Parse(reader["E-value"]);
                    int charge = int.Parse(reader["Charge"]);
                    double isoMZ = double.Parse(reader["Precursor Isolation m/z (Th)"]);
                    string mods = reader["Mods"];
                    string sequence = reader["Peptide"].ToUpper();
                    string rawfileID = reader["Filename/id"];
                    string rawfileName = rawfileID.Split('.')[0];
                    int spectrumNumber = int.Parse(reader["Spectrum number"]);
                    Match m = _rtRegex.Match(rawfileID);
                    double rt = double.Parse(m.Groups[1].Value);

                    Peptide peptide = new Peptide(sequence);
                    peptide.SetModifications(mods); // Important to set variable mods first, as they will get overwritten by the fixed mods
                    peptide.SetModifications(FixedModifications);
                       
                    var allMods = peptide.GetUniqueModifications<Modification>();
                    allMods.ExceptWith(FixedModifications);
                    foreach (var mod in allMods)
                    {
                        AddVariableModification(mod.ToString(), mod);
                    }
             
                    ThermoRawFile rawFile;
                    if (rawFiles.TryGetValue(rawfileName, out rawFile))
                    {                        
                        var psm = new PeptideSpectrumMatch(rawFile, spectrumNumber,rt, peptide, charge, isoMZ, score, PeptideSpectrumMatchScoreType.OmssaEValue);
                        _psms.Add(psm);
                        HashSet<int> spectra;
                        if (!UsedRawFiles.TryGetValue(rawFile, out spectra))
                        {
                            spectra = new HashSet<int>();
                            UsedRawFiles.Add(rawFile, spectra);
                        }
                        spectra.Add(spectrumNumber);
                    }                   
                }
            }
            PSMCount = _psms.Count;
        }

        public override IEnumerable<PeptideSpectrumMatch> ReadPSMs()
        {
            return _psms;          
        }

        public override void Dispose()
        {
            if (_psms != null)
                _psms = null;

            base.Dispose();
        }

    }

}

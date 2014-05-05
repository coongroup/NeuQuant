using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSMSL.IO.Thermo;
using CSMSL.Proteomics;
using LumenWorks.Framework.IO.Csv;

namespace NeuQuant
{
    public class ProteomeDiscovererPeptideSpectralMatchFile : PeptideSpectralMatchFile
    {
        private static Regex _modRegex = new Regex(@"([A-Z])(\d+)\((.+)\)", RegexOptions.Compiled);
     
        public override string Type
        {
            get
            {
                return "Proteome Discoverer PSM File";
            }
        }

        private List<PeptideSpectrumMatch> _psms;
        private ThermoRawFile _rawFile;

        public ProteomeDiscovererPeptideSpectralMatchFile(string filePath, bool storeMS2Spectra = true)
            : base(filePath, storeMS2Spectra, PeptideSpectrumMatchScoreType.XCorr)
        {

        }
        
        public void SetRawFile(ThermoRawFile rawFile)
        {
            _rawFile = rawFile;
        }

        public override void Open()
        {
            _psms = new List<PeptideSpectrumMatch>();

            using (_rawFile)
            {
                _rawFile.Open();

                int sequenceCol = 2;
                int modsCol = 7;
                int scoreCol = 9;
                int chargeCol = 11;
                int rtCol = 14;

                // Read all variable mods and store
                using (var reader = new CsvReader(new StreamReader(FilePath), true))
                {
                    string currentProteinID = "";
                    bool first = true;
                    while (reader.ReadNextRecord())
                    {
                        string proteinID = reader[0];
                        if (string.IsNullOrEmpty(proteinID))
                        {
                            // Skip the second header
                            if (first)
                            {
                                first = false;

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string value = reader[i];
                                    switch (value)
                                    {
                                        case "Sequence":
                                            sequenceCol = i;
                                            break;
                                        case "Modifications":
                                            modsCol = i;
                                            break;
                                        case "XCorr":
                                            scoreCol = i;
                                            break;
                                        case "Charge":
                                            chargeCol = i;
                                            break;
                                        case "RT [min]":
                                            rtCol = i;
                                            break;
                                    }
                                }

                                continue;
                            }

                            string sequence = reader[sequenceCol].ToUpper();
                            string mods = reader[modsCol];
                            double score = double.Parse(reader[scoreCol]);
                            int charge = int.Parse(reader[chargeCol]);
                            double rt = double.Parse(reader[rtCol]);
                            double isoMZ = 0; // TODO
                            int spectrumNumber = _rawFile.GetSpectrumNumber(rt);

                            Peptide peptide = new Peptide(sequence);

                            // Important to set variable mods first, as they will get overwritten by the fixed mods
                            if (!string.IsNullOrEmpty(mods))
                            {
                                foreach (string mod in mods.Split(';'))
                                {
                                    Match m = _modRegex.Match(mod);
                                    if (m.Success)
                                    {
                                        int residue = int.Parse(m.Groups[2].Value);
                                        string modName = m.Groups[3].Value;
                                        Modification modification;
                                        if (VariableModifications.TryGetValue(modName, out modification))
                                        {
                                            peptide.SetModification(modification, residue);
                                        }
                                        else
                                        {
                                            modification = Reagents.GetModification(modName);
                                            if (modification != null)
                                                peptide.SetModification(modification, residue);
                                        }


                                    }
                                }
                               
                            }


                            peptide.SetModifications(FixedModifications);

                            peptide.ClearModifications(SitesToIgnore);

                            var allMods = peptide.GetUniqueModifications<CSMSL.Proteomics.Modification>();
                            allMods.ExceptWith(FixedModifications);
                            foreach (var mod in allMods)
                            {
                                AddVariableModification(mod.ToString(), mod);
                            }
                            
                            var psm = new PeptideSpectrumMatch(_rawFile, spectrumNumber, rt, peptide, charge, isoMZ, score, ScoreType);
                            _psms.Add(psm);

                            // Store the spectrum Numbers
                            HashSet<int> spectra;
                            if (!UsedRawFiles.TryGetValue(_rawFile, out spectra))
                            {
                                spectra = new HashSet<int>();
                                UsedRawFiles.Add(_rawFile, spectra);
                            }

                            if(StoreMS2Spectra)
                                spectra.Add(spectrumNumber);

                        }
                        else
                        {
                            currentProteinID = proteinID;
                            first = true;
                        }
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

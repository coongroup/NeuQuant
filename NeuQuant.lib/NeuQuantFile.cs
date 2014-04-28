using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using CSMSL.IO.Thermo;
using CSMSL.Spectral;
using CSMSL.Proteomics;
using CSMSL;
using CSMSL.Chemistry;
using NeuQuant.Processing;

namespace NeuQuant.IO
{
    public partial class NeuQuantFile : IDisposable
    {
        public string FilePath { get; private set; }
       
        private SQLiteConnection _dbConnection;
        private SQLiteTransaction _currentTranscation;

        private SQLiteCommand _insertFile;
        private SQLiteCommand _selectFile;

        private SQLiteCommand _insertSpectrum;
        private SQLiteCommand _selectSpectrum;
        private SQLiteCommand _selectIndividualSpectrum;
        private SQLiteCommand _selectPrecursorSpectrum;

        private SQLiteCommand _insertPSM;
        private SQLiteCommand _insertMod;

        private SQLiteCommand _selectPeptide;
        
        private long _currentSpectrumID = 0;
        
        public NeuQuantFile(string filePath)
        {
            FilePath = filePath;
            _dbConnection = new SQLiteConnection(@"Data Source=" + filePath);
            _insertFile = new SQLiteCommand(@"INSERT INTO files (filePath, description) VALUES (@filePath, @description)", _dbConnection);
            _selectFile = new SQLiteCommand("SELECT id FROM files WHERE filePath = @filePath LIMIT 1", _dbConnection);
            _insertSpectrum = new SQLiteCommand(@"INSERT INTO spectra (fileID, scannumber, retentionTime, msnOrder, resolution, injectionTime, spectrum) VALUES (@fileID, @scannumber,@retentionTime,@msnOrder,@resolution,@injectionTime,@spectrum)", _dbConnection);
            _selectSpectrum = new SQLiteCommand(@"SELECT * FROM spectra WHERE msnOrder = @msnOrder AND fileID = @fileID AND retentionTime BETWEEN @minRT AND @maxRT AND resolution >= @minResolution", _dbConnection);
            _insertPSM = new SQLiteCommand(@"INSERT INTO psm_peptide_view (sequence, monoMass, charge, isoMZ, matchScore, scannumber, retentionTime, filePath, scoreType) VALUES (@sequence, @monoMass, @charge, @isoMZ, @matchScore, @scannumber, @retentionTime, @filePath, @scoreType)", _dbConnection);
            _insertMod = new SQLiteCommand(@"INSERT INTO modifications (name, sites, deltaMass, isVariable, type) VALUES (@name, @sites, @deltaMass, @isVariable, @type)", _dbConnection);
            _selectIndividualSpectrum = new SQLiteCommand(@"SELECT * FROM spectra WHERE scannumber = @scannumber AND fileID = (SELECT id FROM files WHERE filePath = @filePath)", _dbConnection);
            _selectPrecursorSpectrum = new SQLiteCommand(@"SELECT * FROM spectra WHERE msnOrder = 1 AND retentionTime BETWEEN @minRT AND @maxRT AND fileID = (SELECT id FROM files WHERE filePath = @filePath) AND resolution >= @minResolution ORDER BY ABS(retentionTIme - @rt) LIMIT 1", _dbConnection);
            _selectPeptide = new SQLiteCommand(@"SELECT id FROM peptides WHERE sequence = @sequence AND monoMass = @monoMass", _dbConnection);
            _selectModification = new SQLiteCommand(@"SELECT id FROM modifications WHERE name = @name" , _dbConnection);
        }        

        public bool Open()
        {
            if (_dbConnection.State == System.Data.ConnectionState.Open)
                return true;
            try
            {
                _dbConnection.Open();             
            }
            catch (SQLiteException e)
            {
                return false;
            }
            return true;
        }
          
        public long SelectFile(string filePath)
        {
            _selectFile.Parameters.AddWithValue("@filePath", filePath);
            return (long)_selectFile.ExecuteScalar();
        }

        public long InsertFile(string filePath, string description = "")
        {
            _insertFile.Parameters.AddWithValue("@filePath", filePath);         
            _insertFile.Parameters.AddWithValue("@description", description);
            _insertFile.ExecuteScalar();

            return SelectFile(filePath);
        }
              
        private Dictionary<long, Modification> GetModifications()
        {
            // Read non isotopologues
            var selectMods = new SQLiteCommand(@"SELECT id, name, sites, deltaMass, isVariable
                                                FROM modifications m
                                                WHERE type <> 'Isotopologue'", _dbConnection);

            Dictionary<long, Modification> mods = new Dictionary<long, Modification>();

            using (var reader = selectMods.ExecuteReader())
            {
                while (reader.Read())
                {
                    long id = (long)reader["id"];
                    string name = reader["name"].ToString();
                    ModificationSites sites = (ModificationSites)reader["sites"];
                    double mass = (double)reader["deltaMass"];
                    bool isVariable = (bool)reader["isVariable"];

                    Modification mod = new Modification(mass, name, sites);
                    mods.Add(id, mod);
                }
            }

            // Read isotopologues
            var selectIsotopologues = new SQLiteCommand(@"SELECT id, name,sites, modificationID
                                                        FROM modifications m
                                                        INNER JOIN mods_to_isotopologue mti
                                                        ON mti.isotopologueID = m.id", _dbConnection);

            using (var reader = selectIsotopologues.ExecuteReader())
            {
                long lastID = -1;
                Isotopologue isotopologue = null;
                while (reader.Read())
                {
                    long currentID = (long)reader["id"];
                    if (currentID != lastID)
                    {
                        lastID = currentID;
                        string name = reader["name"].ToString();
                        ModificationSites sites = (ModificationSites)reader["sites"];
                        isotopologue = new Isotopologue(name, sites);
                        mods.Add(currentID, isotopologue);
                    }
                    long modId = (int)reader["modificationID"];
                    isotopologue.AddModification(mods[modId]);
                }
            }

            return mods;
        }

        public IEnumerable<NeuQuantPeptide> GetPeptides()
        {
            NeuQuantPeptide currentPeptide = new NeuQuantPeptide();
            foreach (PeptideSpectrumMatch psm in GetPsms().OrderBy(psm => psm.LeucineSequence).ThenBy(psm => psm.MonoisotopicMass))
            {
                if (currentPeptide.AddPeptideSpectrumMatch(psm))
                    continue;

                yield return currentPeptide;
                currentPeptide = new NeuQuantPeptide(psm);
            }
            yield return currentPeptide;
        } 

        public IEnumerable<PeptideSpectrumMatch> GetPsms()
        {
            Dictionary<long, Modification> modifications = GetModifications();

            var selectPSMs = new SQLiteCommand(@"SELECT psms.id AS psmID, m.id AS modID, sequence, charge, isoMZ, matchScore, position, scannumber, filePath, psms.retentionTime, scoreType
                                                FROM psms 
                                                INNER JOIN peptides pep
                                                ON psms.peptideID = pep.id
                                                LEFT JOIN mods_to_peptides mtp
                                                ON mtp.peptideID = pep.id
                                                LEFT JOIN modifications m
                                                ON mtp.modificationID = m.id
                                                INNER JOIN spectra s
                                                ON psms.spectrumID = s.id
                                                INNER JOIN files f
                                                ON s.fileID = f.id
                                                ORDER BY psms.id", _dbConnection);

            Dictionary<string, ThermoRawFile> rawFiles = new Dictionary<string, ThermoRawFile>();
            using (var reader = selectPSMs.ExecuteReader())
            {
                long lastPSMid = -1;
                Peptide peptide = null;
                ThermoRawFile rawFile = null;
                int spectrumNumber = 0;
                int charge = 0;
                double isoMZ = 0;
                double score = 0;
                double rt = 0;
                PeptideSpectrumMatchScoreType scoreType = PeptideSpectrumMatchScoreType.Unknown;;
                while (reader.Read())
                {
                    long currentPSMid = (long)reader["psmID"];

                    if (lastPSMid != currentPSMid)
                    {
                        if (peptide != null)
                        {
                            yield return new PeptideSpectrumMatch(rawFile, spectrumNumber, rt, peptide, charge, isoMZ, score, scoreType);
                        }
                        lastPSMid = currentPSMid;
                        string sequence = reader["sequence"].ToString();
                        peptide = new Peptide(sequence);
                        charge = (int)reader["charge"];
                        isoMZ = (double)reader["isoMZ"];
                        score = (double)reader["matchScore"];
                        spectrumNumber = (int)reader["scannumber"];
                        rt = (double)reader["retentionTime"];
                        scoreType = (PeptideSpectrumMatchScoreType) reader["scoreType"];
                        string rawFilePath = reader["filePath"].ToString();
                        if (!rawFiles.TryGetValue(rawFilePath, out rawFile))
                        {
                            rawFile = new ThermoRawFile(rawFilePath);
                            rawFiles.Add(rawFilePath, rawFile);
                        }                       
                    }
                    
                    // Some peptides might not have mods, so check here
                    object modIdObj = reader["modID"];
                    if (modIdObj != DBNull.Value)
                    {
                        long modId = (long)modIdObj;
                        Modification mod = modifications[modId];
                        int position = (int)reader["position"];
                        peptide.SetModification(mod, position);
                    }
                }
                // Return the last psm
                if (peptide != null)
                {
                    yield return new PeptideSpectrumMatch(rawFile, spectrumNumber, rt, peptide, charge, isoMZ, score, scoreType);
                }
            }       
        }

        private long InsertSpectrum(NeuQuantSpectrum spectrum, long fileID, bool compress = true)
        {
            //if (spectrum.MsnOrder == 2)
            //    compress = true;
            _insertSpectrum.Parameters.AddWithValue("@fileID", fileID);
            _insertSpectrum.Parameters.AddWithValue("@scannumber", spectrum.ScanNumber);
            _insertSpectrum.Parameters.AddWithValue("@retentionTime", spectrum.RetentionTime.ToString("F3"));
            _insertSpectrum.Parameters.AddWithValue("@msnOrder", spectrum.MsnOrder);
            _insertSpectrum.Parameters.AddWithValue("@resolution", spectrum.Resolution);
            _insertSpectrum.Parameters.AddWithValue("@injectionTime", spectrum.InjectionTime.ToString("F3"));          
            _insertSpectrum.Parameters.AddWithValue("@spectrum", spectrum.ToBytes(compress));          
            _insertSpectrum.ExecuteNonQuery();
           
            return _dbConnection.LastInsertRowId;
        }        

        private void LoadPSMFile(PeptideSpectralMatchFile psmFile)
        {
            OnMessageUpdate(this, "Reading PSMs from " + psmFile.FilePath + "...");
            using (var transcation = _dbConnection.BeginTransaction())
            {
                int count = 0;
                foreach (PeptideSpectrumMatch psm in psmFile.ReadPSMs())
                {
                    InsertPSM(psm);
                    count++;
                    if (count % 100 == 0)
                    {
                        OnProgressUpdate(this, (double)count / psmFile.PSMCount);
                    }
                }               
                transcation.Commit();
            }         
        }
               
        public void LoadData(PeptideSpectralMatchFile psmFile, bool compressSpectra = false)
        {
            if (psmFile == null)
                return;
          
            using (psmFile)
            {
                psmFile.Open();
                
                InsertFile(psmFile.FilePath, psmFile.Type);
               
                LoadModifications(psmFile);

                foreach (var rawFileTuple in psmFile.UsedRawFiles)
                {
                    LoadSpectra(rawFileTuple.Key, rawFileTuple.Value, compressSpectra);
                }

                LoadPSMFile(psmFile);              
            }
          
            OnMessageUpdate(this, "Finished Loading Data");
            OnProgressUpdate(this, 0);
        }  

        private void LoadModifications(PeptideSpectralMatchFile psmFile)
        {
            using (var transaction = _dbConnection.BeginTransaction())
            {
                foreach (Modification mod in psmFile.FixedModifications)
                {
                    InsertModification(mod, false);                   
                }

                foreach (Modification mod in psmFile.VariableModifications.Values)
                {
                    InsertModification(mod, true);
                }
                transaction.Commit();
            }
        }

        private SQLiteCommand _selectModification;

        private long InsertModification(Modification mod, bool isVariable)
        {         
            long id;
            List<long> subIds = null;
            Isotopologue isotopologue = mod as Isotopologue;
            if (isotopologue != null)
            {
                subIds = new List<long>();
                foreach (Modification mod2 in isotopologue.GetModifications())
                {
                    subIds.Add(InsertModification(mod2, isVariable));
                }        
            }
            
            _insertMod.Parameters.AddWithValue("@name", mod.Name);
            _insertMod.Parameters.AddWithValue("@sites", mod.Sites);       
            _insertMod.Parameters.AddWithValue("@deltaMass", mod.MonoisotopicMass);
            _insertMod.Parameters.AddWithValue("@isVariable", isVariable);
            _insertMod.Parameters.AddWithValue("@type", mod.GetType().Name);
            _insertMod.ExecuteNonQuery();
            
            _selectModification.Parameters.AddWithValue("@name", mod.Name);
            id = (long)_selectModification.ExecuteScalar();

            if (subIds == null) 
                return id;

            foreach (long subId in subIds)
            {
                new SQLiteCommand(@"INSERT INTO mods_to_isotopologue VALUES (" + id + "," + subId + ")", _dbConnection).ExecuteNonQuery();
            }

            return id;
        }
        
        private void LoadSpectra(ThermoRawFile rawFile, ISet<int> includeSpectrumNumber, bool compressSpectra = false)
        {
            using (rawFile)
            {
                OnMessageUpdate(this, "Reading spectra from " + rawFile.FilePath + "...");
                rawFile.Open();
                using (var transcation = _dbConnection.BeginTransaction())
                {
                    long fileID = InsertFile(rawFile.FilePath, "Thermo Raw File");

                    // Loop over every spectra
                    for (int i = rawFile.FirstSpectrumNumber; i <= rawFile.LastSpectrumNumber; i++)
                    {
                        // Insert it if it is an MS1 or a PSM id
                        int msnOrder = rawFile.GetMsnOrder(i);
                        if (msnOrder == 1 || includeSpectrumNumber.Contains(i))
                        {
                            NeuQuantSpectrum spectrum = NeuQuantSpectrum.Load(rawFile, i);                        
                            InsertSpectrum(spectrum, fileID, compressSpectra);                            
                        }

                        if (i % 1000 == 0)
                        {
                            OnProgressUpdate(this, (double)i / rawFile.LastSpectrumNumber);
                        }
                    }
                    transcation.Commit();
                }
                 
                // Create Indices for faster lookup           
                new SQLiteCommand(@"CREATE INDEX IF NOT EXISTS retentionTime ON spectra (fileID, retentionTime)", _dbConnection).ExecuteNonQuery();
                new SQLiteCommand(@"CREATE INDEX IF NOT EXISTS spectraNumber ON spectra (scannumber)", _dbConnection).ExecuteNonQuery();
            }
            OnProgressUpdate(this, 0);       
        }

        private void InsertPSM(PeptideSpectrumMatch psm)
        {
            _insertPSM.Parameters.AddWithValue("@sequence", psm.LeucineSequence);
            _insertPSM.Parameters.AddWithValue("@monoMass", psm.MonoisotopicMass);
            _insertPSM.Parameters.AddWithValue("@charge", psm.Charge);
            _insertPSM.Parameters.AddWithValue("@isoMZ", psm.IsolationMZ);
            _insertPSM.Parameters.AddWithValue("@matchScore", psm.MatchScore);
            _insertPSM.Parameters.AddWithValue("@scoreType", psm.MatchType);
            _insertPSM.Parameters.AddWithValue("@scannumber", psm.SpectrumNumber);
            _insertPSM.Parameters.AddWithValue("@retentionTime", psm.RetentionTime);
            _insertPSM.Parameters.AddWithValue("@filePath", psm.RawFile.FilePath);
            _insertPSM.ExecuteNonQuery();

            // Get Peptide
            _selectPeptide.Parameters.AddWithValue("@sequence", psm.LeucineSequence);
            _selectPeptide.Parameters.AddWithValue("@monoMass", psm.MonoisotopicMass);
            long peptideID = (long)_selectPeptide.ExecuteScalar();

            IMass[] mods = psm.Peptide.GetModifications();
            for (int i = 0; i < mods.Length; i++)
            {
                Modification mod = mods[i] as Modification;
                if (mod != null)
                {
                    new SQLiteCommand(@"INSERT INTO mods_to_peptides 
                                    VALUES ("+peptideID+",(SELECT id FROM modifications WHERE name = '"+mod.Name+"'),"+i+")", _dbConnection).ExecuteNonQuery();
                }
            }
        }

        public SQLiteTransaction BeginTransaction()
        {
            _currentTranscation = _dbConnection.BeginTransaction();
            return _currentTranscation;
        }

        public void EndTranscation()
        {
            EndTranscation(_currentTranscation);
        }

        public void EndTranscation(SQLiteTransaction transaction)
        {
            if (transaction == null)
                return;
            transaction.Commit();
            transaction.Dispose();           
        }

        #region Spectra

        public NeuQuantSpectrum GetPrecursorSpectrum(PeptideSpectrumMatch psm, double retentionTime, double minResolution = 0, int direction = 0)
        {           
            if(direction == 0) {
                _selectPrecursorSpectrum.Parameters.AddWithValue("@minRT", retentionTime - 1);
                _selectPrecursorSpectrum.Parameters.AddWithValue("@maxRT", retentionTime + 1);
            }
            else if (direction > 0)
            {
                _selectPrecursorSpectrum.Parameters.AddWithValue("@minRT", retentionTime + 0.000000001);
                _selectPrecursorSpectrum.Parameters.AddWithValue("@maxRT", retentionTime + 1);
            }
            else
            {
                _selectPrecursorSpectrum.Parameters.AddWithValue("@minRT", retentionTime - 1);
                _selectPrecursorSpectrum.Parameters.AddWithValue("@maxRT", retentionTime - 0.0000000001);
            }
            _selectPrecursorSpectrum.Parameters.AddWithValue("@minResolution", minResolution);
            _selectPrecursorSpectrum.Parameters.AddWithValue("@rt", retentionTime);
            _selectPrecursorSpectrum.Parameters.AddWithValue("@filePath", psm.RawFile.FilePath);
            using (var reader = _selectPrecursorSpectrum.ExecuteReader())
            {
                if (reader.Read())
                {
                    byte[] bytes = reader["spectrum"] as byte[];
                    if (bytes == null)
                    {
                        return null;
                    }
                    NeuQuantSpectrum nqSpectrum = new NeuQuantSpectrum(bytes);
                    nqSpectrum.ScanNumber = (int)reader["scannumber"];
                    nqSpectrum.RetentionTime = (double)reader["retentionTime"];
                    nqSpectrum.MsnOrder = (int)reader["msnOrder"];
                    nqSpectrum.InjectionTime = (double)reader["injectionTime"];
                    nqSpectrum.Resolution = (double)reader["resolution"];
                    return nqSpectrum;
                }
            }
            return null;
        }

        public NeuQuantSpectrum GetSpectrum(PeptideSpectrumMatch psm)
        {
            _selectIndividualSpectrum.Parameters.AddWithValue("@scannumber", psm.SpectrumNumber);
            _selectIndividualSpectrum.Parameters.AddWithValue("@filePath", psm.RawFile.FilePath);
            using (var reader = _selectIndividualSpectrum.ExecuteReader())
            {
                if (reader.Read())
                {
                    byte[] bytes = reader["spectrum"] as byte[];
                    if (bytes == null)
                    {
                        return null;
                    }
                    NeuQuantSpectrum nqSpectrum = new NeuQuantSpectrum(bytes);
                    nqSpectrum.ScanNumber = (int)reader["scannumber"];
                    nqSpectrum.RetentionTime = (double)reader["retentionTime"];
                    nqSpectrum.MsnOrder = (int)reader["msnOrder"];
                    nqSpectrum.InjectionTime = (double)reader["injectionTime"];
                    nqSpectrum.Resolution = (double)reader["resolution"];
                    return nqSpectrum;
                }
            }
            return null;
        }

        public IEnumerable<NeuQuantSpectrum> GetSpectra(double minRT, double maxRT, long fileID, int msnOrder = 1, double minResolution = 0)
        {
            _selectSpectrum.Parameters.AddWithValue("@minRT", minRT);
            _selectSpectrum.Parameters.AddWithValue("@maxRT", maxRT);
            _selectSpectrum.Parameters.AddWithValue("@msnOrder", msnOrder);
            _selectSpectrum.Parameters.AddWithValue("@fileID", fileID);
            _selectSpectrum.Parameters.AddWithValue("@minResolution", minResolution);
            using (var reader = _selectSpectrum.ExecuteReader())
            {
                while (reader.Read())
                {
                    byte[] bytes = reader["spectrum"] as byte[];
                    if (bytes == null)
                    {
                        yield return null;
                    }
                    NeuQuantSpectrum nqSpectrum = new NeuQuantSpectrum(bytes);
                    nqSpectrum.ScanNumber = (int)reader["scannumber"];
                    nqSpectrum.RetentionTime = (double)reader["retentionTime"];                    
                    nqSpectrum.MsnOrder = (int)reader["msnOrder"];
                    nqSpectrum.InjectionTime = (double)reader["injectionTime"];
                    nqSpectrum.Resolution = (double) reader["resolution"];
                    yield return nqSpectrum;
                }
            }
        }

        #endregion

        public void Dispose()
        {

        }

        public bool TryGetLastProcessor(out Processor processor)
        {
            processor = null;

            // TODO write code for generating processors
            processor = new Processor(this, 3, 0.75, 0.75, 480000, checkIsotopicDistribution: true);

            return processor != null;
        }
    }
}

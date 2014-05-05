using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using CSMSL.Analysis.ExperimentalDesign;
using CSMSL.IO;
using CSMSL.IO.Thermo;
using CSMSL.Proteomics;
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

        private SQLiteCommand _insertSample;
        private SQLiteCommand _insertAnalysis;
        private SQLiteCommand _insertAnalysisParameter;

        private SQLiteCommand _insertQuantitation;

        private SQLiteCommand _selectPeptide;

        private Dictionary<long, CSMSL.Proteomics.Modification> _modifications;

        private Dictionary<long, CSMSL.Proteomics.Modification> Modifications
        {
            get
            {
                if (_modifications == null)
                {
                    _modifications = GetModifications();
                }
                return _modifications;
            }
        }

        private List<NeuQuantSample> _samples;
        public List<NeuQuantSample> Samples
        {
            get
            {
                if (_samples == null)
                {
                    _samples = GetSamples().ToList();
                }
                return _samples;
            }
        }

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
            _selectModification = new SQLiteCommand(@"SELECT id FROM modifications WHERE name = @name", _dbConnection);
            _insertAnalysis = new SQLiteCommand(@"INSERT INTO analyses (createDate, name) VALUES (DateTime('now'), @name)", _dbConnection);
            _insertAnalysisParameter = new SQLiteCommand(@"INSERT INTO analysisParameters (analysisID, key, value) VALUES (@analysisID, @key, @value)", _dbConnection);
            _insertSample = new SQLiteCommand(@"INSERT INTO samples (conditionName, sampleName, sampleDescription) VALUES (@conditionName, @sampleName, @sampleDescription)", _dbConnection);


            _insertQuantitation = new SQLiteCommand(@"INSERT INTO quantitation (analysisID, peptideID, sampleID, quantitation) VALUES (@analysisID, @peptideID, @sampleID, @quantitation)", _dbConnection);
        }

        public bool Open()
        {
            if (_dbConnection.State == System.Data.ConnectionState.Open)
                return true;

            if (!IsSqliteDatabase(FilePath))
            {
                return false;
            }

            try
            {
                _dbConnection.Open();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

       

        public long SelectFile(string filePath)
        {
            _selectFile.Parameters.AddWithValue("@filePath", filePath);
            object o = _selectFile.ExecuteScalar();
            return (long)o;
        }

        public long InsertFile(string filePath, string description = "")
        {
            _insertFile.Parameters.AddWithValue("@filePath", filePath);
            _insertFile.Parameters.AddWithValue("@description", description);
            _insertFile.ExecuteScalar();

            return SelectFile(filePath);
        }

        public IEnumerable<double> GetUniqueResolutions()
        {
            var selectResolutions = new SQLiteCommand(@"SELECT DISTINCT resolution FROM spectra WHERE resolution > 0 ORDER BY resolution ASC", _dbConnection);
            using (var reader = selectResolutions.ExecuteReader())
            {
                while (reader.Read())
                {
                    double resolution = (double) reader["resolution"];
                    yield return resolution;
                }
            }
        }

        public IEnumerable<NeuQuantSample> GetSamples()
        {
            var selectSamples = new SQLiteCommand(@"SELECT * FROM samples s
                                                    LEFT JOIN samples_to_mods stm
                                                    ON stm.sampleID = s.id 
                                                    ORDER BY s.id", _dbConnection);
            using (var reader = selectSamples.ExecuteReader())
            {
                long lastId = -1;
                ExperimentalCondition condition = null;
                NeuQuantSample sample = null;
                while (reader.Read())
                {
                    long id = (long)reader["id"];
                    if (id != lastId)
                    {
                        lastId = id;
                        if (condition != null)
                            yield return sample;
                        string coniditonName = (string)reader["conditionName"];
                        string sampleName = (string) reader["sampleName"];
                        string description = (string)reader["sampleDescription"];
                        condition = new ExperimentalCondition(coniditonName);
                        sample = new NeuQuantSample(sampleName, description, condition) { ID = id };
                    }
                    object modIdObj = reader["modificationID"];
                    if (modIdObj != DBNull.Value)
                    {
                        long modID = (int)modIdObj;

                        Modification mod = Modifications[modID];
                        condition.Modifications.Add(mod);
                    }

                }
                yield return sample;
            }
        }

        private Dictionary<long, Modification> GetModifications()
        {
            // Read non isotopologues
            var selectMods = new SQLiteCommand(@"SELECT id, name, sites, deltaMass, isVariable
                                                FROM modifications m
                                                WHERE type <> 'Isotopologue'", _dbConnection);

            Dictionary<long, CSMSL.Proteomics.Modification> mods = new Dictionary<long, CSMSL.Proteomics.Modification>();

            using (var reader = selectMods.ExecuteReader())
            {
                while (reader.Read())
                {
                    long id = (long) reader["id"];
                    string name = reader["name"].ToString();
                    ModificationSites sites = (ModificationSites) reader["sites"];
                    double mass = (double) reader["deltaMass"];
                    bool isVariable = (bool) reader["isVariable"];

                    CSMSL.Proteomics.Modification mod = new CSMSL.Proteomics.Modification(mass, name, sites);
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
                    long currentID = (long) reader["id"];
                    if (currentID != lastID)
                    {
                        lastID = currentID;
                        string name = reader["name"].ToString();
                        ModificationSites sites = (ModificationSites) reader["sites"];
                        isotopologue = new Isotopologue(name, sites);
                        mods.Add(currentID, isotopologue);
                    }
                    long modId = (int) reader["modificationID"];
                    isotopologue.AddModification(mods[modId]);
                }
            }

            return mods;
        }

        public IEnumerable<Tuple<string, Dictionary<NeuQuantSample, double>>> GetQuantitation(long analysisID)
        {
            var samples = GetSamples().ToDictionary(s => s.Name);

            var selectQuantitation = new SQLiteCommand(@"SELECT s.sampleName AS sampleName, sequence, quantitation
                                                            FROM quantitation q
                                                            JOIN samples s
                                                            ON q.sampleID = s.id
                                                            JOIN peptides p
                                                            ON q.peptideID = p.id
                                                            WHERE analysisID = '"+analysisID+"' ORDER BY peptideID", _dbConnection);

            using (var reader = selectQuantitation.ExecuteReader())
            {
                string lastSequence = "";
                var quant = new Dictionary<NeuQuantSample, double>();
                while (reader.Read())
                {
                    string sequence = (string) reader["sequence"];
                    if(!sequence.Equals(lastSequence))
                    {
                        if(lastSequence != "")
                            yield return new Tuple<string, Dictionary<NeuQuantSample, double>> (lastSequence, quant);
                        quant = new Dictionary<NeuQuantSample, double>();
                        lastSequence = sequence;
                    }
                    
                    string sampleName = (string)reader["sampleName"];
                    var sample = samples[sampleName];
                    double quantitation = (double) reader["quantitation"];
                    quant[sample] =quantitation;
                }
                if(lastSequence != "")
                    yield return new Tuple<string, Dictionary<NeuQuantSample, double>> (lastSequence, quant);
            }
        
        }
        
        public IEnumerable<NeuQuantPeptide> GetPeptides()
        { 
            NeuQuantPeptide currentPeptide = new NeuQuantPeptide();
            foreach (PeptideSpectrumMatch psm in GetPsms().OrderBy(psm => psm.PeptideID))
            {
                if (currentPeptide.AddPeptideSpectrumMatch(psm, Samples))
                    continue;

                yield return currentPeptide;
                currentPeptide = new NeuQuantPeptide();
                currentPeptide.AddPeptideSpectrumMatch(psm, Samples);
            }
            yield return currentPeptide;
        }

        private IEnumerable<PeptideSpectrumMatch> GetPsms()
        {
            var selectPSMs = new SQLiteCommand(@"SELECT psms.id AS psmID, m.id AS modID, sequence, charge, isoMZ, matchScore, position, scannumber, filePath, psms.retentionTime, scoreType, pep.id AS pepID
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
                long peptideID = -1;
                PeptideSpectrumMatchScoreType scoreType = PeptideSpectrumMatchScoreType.Unknown;
                while (reader.Read())
                {
                    long currentPSMid = (long) reader["psmID"];

                    if (lastPSMid != currentPSMid)
                    {
                        if (peptide != null)
                        {
                            yield return new PeptideSpectrumMatch(rawFile, spectrumNumber, rt, peptide, charge, isoMZ, score, scoreType) { PeptideID = peptideID };
                        }
                        lastPSMid = currentPSMid;
                        string sequence = reader["sequence"].ToString();
                        peptideID = (long) reader["pepID"];
                        peptide = new Peptide(sequence);
                        charge = (int) reader["charge"];
                        isoMZ = (double) reader["isoMZ"];
                        score = (double) reader["matchScore"];
                        spectrumNumber = (int) reader["scannumber"];
                        rt = (double) reader["retentionTime"];
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
                        long modId = (long) modIdObj;
                        Modification mod = Modifications[modId];
                        int position = (int) reader["position"];
                        peptide.SetModification(mod, position);
                    }
                }
                // Return the last psm
                if (peptide != null)
                {
                    yield return new PeptideSpectrumMatch(rawFile, spectrumNumber, rt, peptide, charge, isoMZ, score, scoreType) { PeptideID = peptideID };
                }
            }
        }

        public IEnumerable<NeuQuantAnalysis> GetAnalyses()
        {
            var selectAnalyses = new SQLiteCommand(@"SELECT * FROM analyses ORDER BY NAME", _dbConnection);

            Dictionary<string, ThermoRawFile> rawFiles = new Dictionary<string, ThermoRawFile>();
            using (var reader = selectAnalyses.ExecuteReader())
            {
                string lastID = "fasdfasd";
                NeuQuantAnalysis analysis = null;
                while (reader.Read())
                {
                    string name = (string) reader["name"];
                    if (name != lastID)
                    {
                        if (analysis != null)
                            yield return analysis;

                        analysis = new NeuQuantAnalysis(name);
                        lastID = name;
                    }

                    long id = (long) reader["id"];
                    string date = (string) reader["createDate"];
                    analysis.AddAnalysis(date, id);
                }
                if (analysis != null)
                    yield return analysis;
            }
        }

        public void InsertQuantitation(Processor processor, NeuQuantQuantitation quantitation)
        {
            foreach (var blah in quantitation.Quantitation)
            {
                NeuQuantSample sample = blah.Key;
                double quant = blah.Value;

                _insertQuantitation.Parameters.AddWithValue("@analysisID", processor.ID);
                _insertQuantitation.Parameters.AddWithValue("@sampleID", sample.ID);
                _insertQuantitation.Parameters.AddWithValue("@peptideID", quantitation.Peptide.BestPeptideSpectrumMatch.PeptideID);
                _insertQuantitation.Parameters.AddWithValue("@quantitation", quant);
                _insertQuantitation.ExecuteNonQuery();
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
                    if (count%100 == 0)
                    {
                        OnProgressUpdate(this, (double) count/psmFile.PSMCount);
                    }
                }
                transcation.Commit();
            }
        }

        private void LoadSamples(PeptideSpectralMatchFile psmFile)
        {
            using (var trans = _dbConnection.BeginTransaction())
            {
                foreach (var sample in psmFile.Samples.Values)
                {
                    LoadSample(sample);
                }
                trans.Commit();
            }
        }

        private void LoadSample(NeuQuantSample sample)
        {
            _insertSample.Parameters.AddWithValue("@conditionName", sample.Condition.Name);
            _insertSample.Parameters.AddWithValue("@sampleName", sample.Name);
            _insertSample.Parameters.AddWithValue("@sampleDescription", sample.Description);
            _insertSample.ExecuteNonQuery();
            long sampleID = _dbConnection.LastInsertRowId;

            foreach (var modification in sample.Condition.Modifications)
            {
                new SQLiteCommand(@"INSERT INTO samples_to_mods (sampleID, modificationID) VALUES ('" + sampleID + "', (SELECT id FROM modifications WHERE name = '" + modification.Name + "' AND sites = '" + (int)modification.Sites + "' ) )", _dbConnection).ExecuteNonQuery();
            }
        }

        public void LoadData(PeptideSpectralMatchFile psmFile, bool compressSpectra = false)
        {
            if (psmFile == null)
                return;

            OnMessageUpdate(this, "Loading Data From " + psmFile.FilePath + "...");
            using (psmFile)
            {
                OnMessageUpdate(this, "Opening File " + psmFile.FilePath + "...");
                psmFile.Open();
                
                InsertFile(psmFile.FilePath, psmFile.Type);
               
                LoadModifications(psmFile);

                LoadSamples(psmFile);

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
                foreach (Modification mod in psmFile.Experiment.GetAllModifications())
                {
                    InsertModification(mod, false);
                }

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
                foreach (Modification mod2 in isotopologue)
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
                CSMSL.Proteomics.Modification mod = mods[i] as CSMSL.Proteomics.Modification;
                if (mod != null)
                {
                    new SQLiteCommand(@"INSERT INTO mods_to_peptides 
                                    VALUES ("+peptideID+",(SELECT id FROM modifications WHERE name = '"+mod.Name+"'),"+i+")", _dbConnection).ExecuteNonQuery();
                }
            }
        }

        public void SaveAnalysisParameter(long id, string key, string value)
        {
            _insertAnalysisParameter.Parameters.AddWithValue("@analysisID", id);
            _insertAnalysisParameter.Parameters.AddWithValue("@key", key);
            _insertAnalysisParameter.Parameters.AddWithValue("@value", value);
            _insertAnalysisParameter.ExecuteNonQuery();
        }

        public long SaveAnalysis(string analysisName = "")
        {
            _insertAnalysis.Parameters.AddWithValue("@name", analysisName);
            _insertAnalysis.ExecuteNonQuery();
            long analysisID = _dbConnection.LastInsertRowId;
            return analysisID;
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

        public IEnumerable<NeuQuantSpectrum> GetSpectra(double minRT, double maxRT, long fileID,  int msnOrder = 1, double minResolution = 0)
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

        public Processor GetProcessor(string name)
        {
            return _getProcessor("WHERE a.name = " + name);
        }

        public Processor GetProcessor(long id)
        {
            return _getProcessor("WHERE a.id = " + id);
        }

        private Processor _getProcessor(string whereSql)
        {
            Processor processor = new Processor(this);

            const string baseSql = @"SELECT name, createDate, a.id as ID, key, value
                                    FROM analyses a
                                    JOIN analysisParameters ap
                                    ON ap.analysisID = a.id";

            var selectProcessor = new SQLiteCommand(string.Join(" ", baseSql, whereSql), _dbConnection);
            
            using (var reader = selectProcessor.ExecuteReader())
            {
                bool first = true;
                while (reader.Read())
                {
                    if (first)
                    {
                        string name = (string)reader["name"];
                        long id = (long)reader["ID"];
                        processor.Name = name;
                        processor.ID = id;
                        first = false;
                    }

                    string key = (string) reader["key"];
                    string value = (string) reader["value"];

                    processor.SetValue(key, value);
                }
            }
           
            return processor;
        }

        public bool TryGetLastProcessor(out Processor processor)
        {
            processor = _getProcessor("WHERE a.id = (SELECT id FROM analyses a ORDER BY createDate DESC LIMIT 1)");
          
            return !string.IsNullOrEmpty(processor.Name);
        }



      
    }
}

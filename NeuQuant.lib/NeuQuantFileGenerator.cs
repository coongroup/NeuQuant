#define TESTING

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuQuant.IO
{
    public partial class NeuQuantFile
    {
        private const long VERSIONNUMBER = 1;

        #region Call Backs

        public static event EventHandler<ProgressEventArgs> OnProgess;

        public static event EventHandler<MessageEventArgs> OnMessage;

        private static void OnProgressUpdate(double percent)
        {
            var handler = OnProgess;
            if (handler != null)
            {
                handler(null, new ProgressEventArgs(percent));
            }
        }

        private static void OnProgressUpdate(object sender, double percent)
        {
            var handler = OnProgess;
            if (handler != null)
            {
                handler(sender, new ProgressEventArgs(percent));
            }
        }

        private static void OnMessageUpdate(object sender, string message)
        {
            var handler = OnMessage;
            if (handler != null)
            {
                handler(sender, new MessageEventArgs(message));
            }
        }

        public static event EventHandler OnFinished;

        #endregion
               
        public static NeuQuantFile Create(string outputFilePath)
        {
            string sql;
        
            using (SQLiteConnection conn = new SQLiteConnection(@"Data Source=" + outputFilePath))
            {
                conn.Open();

#if TESTING
                new SQLiteCommand("PRAGMA writable_schema = 1;delete from sqlite_master where type = 'table';delete from sqlite_master where type = 'index'; PRAGMA writable_schema = 0; VACUUM;PRAGMA auto_vacuum = 2;", conn).ExecuteNonQuery();
#endif
                // Set the Version Number
                new SQLiteCommand("PRAGMA user_version = " + VERSIONNUMBER, conn).ExecuteNonQuery();
                
                // File Information
                sql = @"CREATE TABLE IF NOT EXISTS files (
                        id INTEGER PRIMARY KEY ASC, 
                        filePath TEXT UNIQUE ON CONFLICT IGNORE, 
                        description TEXT)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // SPECTRUM
                sql = @"CREATE TABLE IF NOT EXISTS spectra (
                        id INTEGER PRIMARY KEY ASC, 
                        fileID INTEGER,
                        scannumber INT, 
                        retentionTime REAL,
                        msnOrder INT,
                        resolution REAL,
                        injectionTime REAL,                        
                        spectrum BLOB,
                        FOREIGN KEY(fileID) REFERENCES files(id),
                        UNIQUE (fileID, scannumber) ON CONFLICT IGNORE)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
                                
                // PEPTIDE
                sql = @"CREATE TABLE IF NOT EXISTS peptides (
                        id INTEGER PRIMARY KEY ASC, 
                        sequence TEXT,
                        monoMass REAL,
                        UNIQUE (sequence, monoMass) ON CONFLICT IGNORE)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // Modified PEPTIDE
                sql = @"CREATE TABLE IF NOT EXISTS mods_to_peptides (
                        peptideID INTEGER,
                        modificationID INTEGER,
                        position INT,
                        PRIMARY KEY (peptideID, modificationID, position) ON CONFLICT IGNORE)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
                           
                // Peptide Spectrum Match
                sql = @"CREATE TABLE IF NOT EXISTS psms (
                        id INTEGER PRIMARY KEY ASC, 
                        peptideID INTEGER,
                        spectrumID INTEGER,     
                        retentionTime REAL,                  
                        charge INT,
                        isoMZ REAL,
                        matchScore REAL)";
                        //FOREIGN KEY(peptideID) REFERENCES peptides(id),
                        //FOREIGN KEY(spectrumID) REFERENCES spectra(id),                        
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // PSM-Peptide View
                sql = @"CREATE VIEW IF NOT EXISTS psm_peptide_view AS
                        SELECT * FROM psms
                        INNER JOIN peptides pep ON pep.id = psms.peptideID
                        INNER JOIN spectra s ON s.id = psms.spectrumID
                        INNER JOIN files f ON f.id = s.fileID";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // PSM-Peptide View
                sql = @"CREATE TRIGGER IF NOT EXISTS insert_psm INSTEAD OF INSERT ON psm_peptide_view
                        BEGIN

                        INSERT INTO peptides (sequence, monoMass)
                        SELECT NEW.sequence, NEW.monoMass
                        WHERE NOT EXISTS
                            (SELECT 1 FROM peptides
                            WHERE sequence = NEW.sequence AND monoMass = NEW.monoMass);

                        INSERT INTO psms (peptideID, charge, isoMZ, matchScore, spectrumID, retentionTime) VALUES
                        ((SELECT peptides.id FROM peptides WHERE peptides.sequence = NEW.sequence AND monoMass = NEW.monoMass), 
                        NEW.charge, NEW.isoMZ, NEW.matchScore, 
                        (SELECT spectra.id FROM spectra INNER JOIN files f ON f.id = spectra.fileID WHERE f.filePath = NEW.filePath AND spectra.scannumber = NEW.scannumber),
                        NEW.retentionTime);                                

                        END";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // Create PEPETIDE INDEX
                new SQLiteCommand(@"CREATE INDEX IF NOT EXISTS peptideSequence ON peptides (sequence)", conn).ExecuteNonQuery();

                // Modifications
                sql = @"CREATE TABLE IF NOT EXISTS modifications (
                        id INTEGER PRIMARY KEY ASC, 
                        name TEXT,
                        sites INT,                  
                        deltaMass REAL,                  
                        isVariable BOOL,
                        type TEXT,
                        UNIQUE (name, sites) ON CONFLICT IGNORE)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // Mods to Isotopologues
                sql = @"CREATE TABLE IF NOT EXISTS mods_to_isotopologue (
                        isotopologueID INT, 
                        modificationID INT,
                        FOREIGN KEY(isotopologueID) REFERENCES modifications(id),
                        FOREIGN KEY(modificationID) REFERENCES modifications(id),
                        UNIQUE (isotopologueID, modificationID) ON CONFLICT IGNORE)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // Samples
                sql = @"CREATE TABLE IF NOT EXISTS samples (
                        id INTEGER PRIMARY KEY ASC, 
                        name TEXT,
                        description TEXT,
                        UNIQUE (name) ON CONFLICT IGNORE)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // Samples_to_mods
                sql = @"CREATE TABLE IF NOT EXISTS samples_to_mods (
                        sampleID INT,
                        modificationID INT,                 
                        PRIMARY KEY (sampleID, modificationID) ON CONFLICT IGNORE)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();


                sql = @"CREATE TABLE IF NOT EXISTS analyses (
                        id INTEGER PRIMARY KEY ASC,
                        createDate TEXT)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

//                // XICs
//                sql = @"CREATE TABLE IF NOT EXISTS xics (
//                        id INTEGER PRIMARY KEY ASC,
//                        peptideID INTEGER,
//                        spectraID INTEGER
//                        intensity REAL,
//                        
//                        modificationID INT,                 
//                        PRIMARY KEY (sampleID, modificationID) ON CONFLICT IGNORE)";
//                new SQLiteCommand(sql, conn).ExecuteNonQuery();
            }

            return new NeuQuantFile(outputFilePath);
        }

        public static NeuQuantFile LoadData(string outputFile, PeptideSpectralMatchFile psmFile, bool compressSpectra = false)
        {
            var file = NeuQuantFile.Create(outputFile);
            file.Open();
            file.LoadData(psmFile, compressSpectra);
            return file;
        }

    }
}

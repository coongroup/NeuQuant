using System.ComponentModel;
using CSMSL.Analysis.ExperimentalDesign;
using CSMSL.Proteomics;
using CSMSL;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;

namespace NeuQuant
{
    /// <summary>
    /// A set of quantitative labels
    /// </summary>
    public static class Reagents
    {
        private static readonly Dictionary<string, NeuQuantModification> Modifications;
        private static readonly Dictionary<string, ExperimentalSet> Experiments;
        
        private static readonly string DeafaultModificationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"NeuQuant\Modifications.xml");

        static Reagents()
        {
            Modifications = new Dictionary<string, NeuQuantModification>();
            Experiments = new Dictionary<string, ExperimentalSet>();
          
            // Load the default modification file
            Load();
        }

        public static IEnumerable<NeuQuantModification> GetAllModifications()
        {
            return Modifications.Values;
        }

        public static IEnumerable<ExperimentalSet> GetAllExperiments()
        {
            return Experiments.Values;
        }

        public static NeuQuantModification GetModification(string name)
        {
            NeuQuantModification mod = null;
            Modifications.TryGetValue(name, out mod);
            return mod;
        }

        public static void AddModification(NeuQuantModification neuQuantModification)
        {
            // Add Modification
            Modifications[neuQuantModification.Name] = neuQuantModification;
            
            // Alert others
            OnModificationsChanged();
        }

        public static bool RemoveModification(string name)
        {
            if (!Modifications.Remove(name)) 
                return false;
            

            OnModificationsChanged();
            return true;
        }

        public static ExperimentalSet GetExperiment(string name)
        {
            return Experiments[name];
        }

        public static void AddExperiment(ExperimentalSet experiment)
        {
            // Add Modification
            Experiments[experiment.Name] = experiment;
            
            // Alert others
            OnExperimentsChanged();
        }

        public static bool RemoveExperiment(string name)
        {
            if (!Experiments.Remove(name))
                return false;

            OnExperimentsChanged();
            return true;
        }

        /// <summary>
        /// Load the default modification file
        /// If the default modification is missing or corrupted, it will autogenerate it
        /// </summary>
        public static void Load()
        {
            // Create file if it doesn't exist
            if (!File.Exists(DeafaultModificationPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DeafaultModificationPath));
                File.Copy(@"Resources/DefaultModifications.xml", DeafaultModificationPath, true);
            }

            Load(DeafaultModificationPath);
        }

        /// <summary>
        /// Load a modification file
        /// </summary>
        /// <param name="filePath">The path to the modification file</param>
        public static void Load(string filePath)
        {
            try
            {
                var modsXml = new XmlDocument();
                modsXml.Load(filePath);
                //new XmlNamespaceManager(modsXml.NameTable);
                foreach (XmlNode node in modsXml.SelectNodes("//Modifications/Modification"))
                {
                    string name = node.Attributes["name"].Value;
                    bool isDefault = bool.Parse(node.Attributes["isDefault"].Value);
                    bool isAminoAcid = bool.Parse(node.Attributes["isAminoAcid"].Value);
                    string chemicalFormula = node.SelectSingleNode("ChemicalFormula").InnerText;

                    ModificationSites sites = ModificationSites.None;
                    foreach (XmlNode siteNode in node.SelectNodes("ModificationSite"))
                    {
                        string modSite = siteNode.InnerText;
                        var site = (ModificationSites) Enum.Parse(typeof (ModificationSites), modSite);
                        sites |= site;
                    }

                    var chemFormMod = new NeuQuantModification(chemicalFormula, name, sites, isAminoAcid, isDefault);
                    Modifications.Add(name, chemFormMod);
                }
                OnModificationsChanged(false);

                foreach (XmlNode node in modsXml.SelectNodes("//ExperimentalSets/ExperimentalSet"))
                {
                    string name = node.Attributes["name"].Value;
                    //bool isDefault = bool.Parse(node.Attributes["isDefault"].Value);

                    ExperimentalSet experiment = new ExperimentalSet(name);
                    
                    foreach (XmlNode conditionNode in node.SelectNodes("ExperimentalCondition"))
                    {
                        string conditionName = conditionNode.Attributes["name"].Value;
                        ExperimentalCondition condition = new ExperimentalCondition(conditionName);
                        foreach (XmlNode modNode in conditionNode.SelectNodes("ModificationID"))
                        {
                            string modID = modNode.InnerText;
                            condition.AddModification(Modifications[modID]);
                        }
                        experiment.Add(condition);
                    }

                    Experiments.Add(name, experiment);
                }
                OnExperimentsChanged(false);
            }
            catch (XmlException)
            {
                RestoreDefaults();
            }
        }

        /// <summary>
        /// Saves the current modifications and isotopologues to the default modification file
        /// </summary>
        public static void Save()
        {
            SaveTo(DeafaultModificationPath);
        }

        /// <summary>
        /// Saves the current modifications and isotopologues
        /// </summary>
        public static void SaveTo(string filePath)
        {
            using (XmlWriter writer = XmlWriter.Create(filePath, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("NeuQuantModifications");
                writer.WriteStartElement("Modifications");
                foreach (NeuQuantModification mod in Modifications.Values)
                {
                    writer.WriteStartElement("Modification");
                    writer.WriteAttributeString("name", mod.Name);
                    writer.WriteAttributeString("isDefault", mod.IsDefault.ToString());
                    writer.WriteAttributeString("isAminoAcid", mod.IsAminoAcid.ToString());
                    writer.WriteElementString("ChemicalFormula", mod.ChemicalFormula.ToString());
                    foreach (ModificationSites site in mod.Sites.GetActiveSites())
                    {
                        writer.WriteElementString("ModificationSite", site.ToString());
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteStartElement("ExperimentalSets");
                foreach (ExperimentalSet experiment in Experiments.Values)
                {
                    writer.WriteStartElement("ExperimentalSet");
                    writer.WriteAttributeString("name", experiment.Name);

                    foreach (ExperimentalCondition condition in experiment)
                    {
                        writer.WriteStartElement("ExperimentalCondition");
                        writer.WriteAttributeString("name", condition.Name);
                        foreach (Modification mod in condition)
                        {
                            writer.WriteElementString("ModificationID", mod.Name);
                        }
                        writer.WriteEndElement(); // end ExperimentalCondition
                    }
                    writer.WriteEndElement();  // end ExperimentalSet
                }
                writer.WriteEndElement(); // end EpxerimentalSets
                writer.WriteEndElement(); // end NeuQuantModifications
                writer.WriteEndDocument();
            }
        }

        public static void RestoreDefaults()
        {
            Modifications.Clear();
            Experiments.Clear();
            Directory.CreateDirectory(Path.GetDirectoryName(DeafaultModificationPath));
            File.Copy(@"Resources/DefaultModifications.xml", DeafaultModificationPath, true);
            Load();
        }

        private static void OnModificationsChanged(bool saveToDisk = true)
        {
            // Flush to disk
            if (saveToDisk)
                Save();

            var handler = ModificationsChanged;
            if(handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        private static void OnExperimentsChanged(bool saveToDisk = true)
        {
            // Flush to disk
            if (saveToDisk)
                Save();

            var handler = ExperimentsChanged;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        public static event EventHandler ModificationsChanged;
        public static event EventHandler ExperimentsChanged;
    }
}


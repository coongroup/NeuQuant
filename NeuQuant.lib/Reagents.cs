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
        private static readonly Dictionary<string, ChemicalFormulaModification> Modifications;
        private static readonly Dictionary<string, Isotopologue> Isotopologues;
        
        private static readonly string DeafaultModificationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"NeuQuant\Modifications.xml");

        static Reagents()
        {
            Modifications = new Dictionary<string, ChemicalFormulaModification>();
            Isotopologues = new Dictionary<string, Isotopologue>();
            
            // Load the default modification file
            Load();
        }

        public static IEnumerable<ChemicalFormulaModification> GetAllModifications()
        {
            return Modifications.Values;
        }

        public static IEnumerable<Isotopologue> GetAllIsotopologue()
        {
            return Isotopologues.Values;
        }

        public static ChemicalFormulaModification GetModification(string name)
        {
            return Modifications[name];
        }

        public static void AddModification(ChemicalFormulaModification modification)
        {
            // Add Modification
            Modifications[modification.Name] = modification;
            
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

        public static Isotopologue GetIsotopologue(string name)
        {
            return Isotopologues[name];
        }

        public static void AddIsotopologue(Isotopologue isotopologue)
        {
            // Add Modification
            Isotopologues[isotopologue.Name] = isotopologue;
            
            // Alert others
            OnIsotopologuesChanged();
        }

        public static bool RemoveIsotopologue(string name)
        {
            if (!Isotopologues.Remove(name))
                return false;

            OnIsotopologuesChanged();
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

                    var chemFormMod = new ChemicalFormulaModification(chemicalFormula, name, sites, isAminoAcid, isDefault);
                    Modifications.Add(name, chemFormMod);
                }
                OnModificationsChanged(false);

                foreach (XmlNode node in modsXml.SelectNodes("//Isotopologues/Isotopologue"))
                {
                    string name = node.Attributes["name"].Value;
                    //bool isDefault = bool.Parse(node.Attributes["isDefault"].Value);
                    ModificationSites sites = ModificationSites.None;
                    foreach (XmlNode siteNode in node.SelectNodes("ModificationSite"))
                    {
                        string modSite = siteNode.InnerText;
                        var site = (ModificationSites) Enum.Parse(typeof (ModificationSites), modSite);
                        sites |= site;
                    }
                    var isotopologue = new Isotopologue(name, sites);

                    foreach (XmlNode idNode in node.SelectNodes("ModificationID"))
                    {
                        string modID = idNode.InnerText;
                        isotopologue.AddModification(Modifications[modID]);
                    }

                    Isotopologues.Add(name, isotopologue);
                }
                OnIsotopologuesChanged(false);
            }
            catch (XmlException e)
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
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
      
            using (XmlWriter writer = XmlWriter.Create(filePath, xmlWriterSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("NeuQuantModifications");
                writer.WriteStartElement("Modifications");
                foreach (ChemicalFormulaModification mod in Modifications.Values)
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
                writer.WriteStartElement("Isotopologues");
                foreach (Isotopologue isotopologue in Isotopologues.Values)
                {
                    writer.WriteStartElement("Isotopologue");
                    writer.WriteAttributeString("name", isotopologue.Name);
                    foreach (ModificationSites site in isotopologue.Sites.GetActiveSites())
                    {
                        writer.WriteElementString("ModificationSite", site.ToString());
                    }
                    foreach (Modification mod in isotopologue.GetModifications())
                    {
                        writer.WriteElementString("ModificationID", mod.Name);
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public static void RestoreDefaults()
        {
            Modifications.Clear();
            Isotopologues.Clear();
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

        private static void OnIsotopologuesChanged(bool saveToDisk = true)
        {
            // Flush to disk
            if (saveToDisk)
                Save();

            var handler = IsotopologuesChanged;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        public static event EventHandler ModificationsChanged;
        public static event EventHandler IsotopologuesChanged;
    }
}


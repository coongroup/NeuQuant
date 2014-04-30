﻿using CSMSL.Proteomics;
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
            OnChanged();
        }

        public static bool RemoveModification(string name)
        {
            if (!Modifications.Remove(name)) 
                return false;

            OnChanged();
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
            OnChanged();
        }

        public static bool RemoveIsotopologue(string name)
        {
            if (!Isotopologues.Remove(name))
                return false;

            OnChanged();
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
                new XmlNamespaceManager(modsXml.NameTable);
                XmlNodeList modificationsNode = modsXml.SelectNodes("//Modifications/Modification");
                if (modificationsNode != null)
                    foreach (XmlNode node in modificationsNode)
                    {
                        string name = node.Attributes["name"].Value;
                        bool isDefault = bool.Parse(node.Attributes["isDefault"].Value);
                        bool isAminoAcid = bool.Parse(node.Attributes["isAminoAcid"].Value);
                        XmlNode chemFormNode = node.SelectSingleNode("ChemicalFormula");
                        string chemicalFormula = chemFormNode.InnerText;
                        XmlNode modSiteNode = node.SelectSingleNode("ModificationSite");
                        string modSite = modSiteNode.InnerText;
                        var site = (ModificationSites) Enum.Parse(typeof (ModificationSites), modSite);
                        var chemFormMod = new ChemicalFormulaModification(chemicalFormula, name, site, isAminoAcid, isDefault);
                        Modifications.Add(name, chemFormMod);
                    }
                XmlNodeList isotopologuesNode = modsXml.SelectNodes("//Isotopologues/Isotopologue");
                foreach (XmlNode node in isotopologuesNode)
                {
                    string name = node.Attributes["name"].Value;
                    // bool isDefault = bool.Parse(node.Attributes["isDefault"].Value);
                    XmlNode modSiteNode = node.SelectSingleNode("ModificationSite");
                    string modSite = modSiteNode.InnerText;
                    var site = (ModificationSites) Enum.Parse(typeof (ModificationSites), modSite);
                    var isotopologue = new Isotopologue(name, site);
                    XmlNodeList modList = node.SelectNodes("ModificationID");
                    ChemicalFormulaModification currentMod;
                    foreach (XmlNode idNode in modList)
                    {
                        string modID = idNode.InnerText;
                        currentMod = Modifications[modID];
                        isotopologue.AddModification(currentMod);
                    }
                    Isotopologues.Add(name, isotopologue);
                }

                OnChanged(false);
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

        private static void OnChanged(bool saveToDisk = false)
        {
            // Flush to disk
            if (saveToDisk)
                Save();

            var handler = Changed;
            if(handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        public static event EventHandler Changed;
    }
}


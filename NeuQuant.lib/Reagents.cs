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
        public static Dictionary<string, ChemicalFormulaModification> Modifications;
        public static Dictionary<string, Isotopologue> Isotopologues;

        //Need logic here to read in user specified modifications
        //Every time the program starts we read in the modifications
        #region Stand-alone modifications

        // Lysine +1
        public static Modification K100 = new Modification("C{13}1 C-1", "K100", ModificationSites.K);

        // Lysine +2
        public static Modification K002 = new Modification("N{15}2 N-2", "K002", ModificationSites.K);

        // Lysine +4
        public static Modification K040 = new Modification("D4 H-4", "K040", ModificationSites.K);

        // Lysine +8
        public static Modification K080 = new Modification("D8 H-8", "K080", ModificationSites.K);
        public static Modification K422 = new Modification("C{13}4 C-4 D2 H-2 N{15}2 N-2", "K422", ModificationSites.K);
        public static Modification K440 = new Modification("C{13}4 C-4 D4 H-4", "K440", ModificationSites.K);
        public static Modification K521 = new Modification("C{13}5 C-5 D2 H-2 N{15}1 N-1", "K521", ModificationSites.K);
        public static Modification K341 = new Modification("C{13}3 C-3 D4 H-4 N{15}1 N-1", "K341", ModificationSites.K);
        public static Modification K602 = new Modification("C{13}6 C-6 N{15}2 N-2", "K602", ModificationSites.K);

        // Arginine +2
        public static Modification R200 = new Modification("C{13}2 C-2", "R200", ModificationSites.R);
        public static Modification R002 = new Modification("N{15}2 N-2", "R002", ModificationSites.R);

        //Null Mod used for SILAC Isotopologues
        public static Modification NullMod = new Modification("", "NullMod", ModificationSites.K);

        #endregion

        public static Isotopologue K8Plex2 = new Isotopologue("K8_2", ModificationSites.K);
        public static Isotopologue K8Plex3 = new Isotopologue("K8_3", ModificationSites.K);

        public static Isotopologue K8SilacPlex2 = new Isotopologue("K8SilacPlex2", ModificationSites.K);

        static Reagents()
        {
            Modifications = new Dictionary<string, ChemicalFormulaModification>();
            Isotopologues = new Dictionary<string, Isotopologue>();
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Modifications.xml");
            ReadInMods(filePath);
        }

        private static void ReadInMods(string filePath)
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
                    var site = (ModificationSites)Enum.Parse(typeof(ModificationSites), modSite);
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
                var site = (ModificationSites)Enum.Parse(typeof(ModificationSites), modSite);
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
        }

        public static void WriteXmlOutput()
        {
            WriteXmlOutput(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Modifications.xml"));
        }

        public static void WriteXmlOutput(string filePath)
        {
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(filePath))
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
    }
}


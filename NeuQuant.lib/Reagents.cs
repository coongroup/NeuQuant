using System.Collections.Generic;
using CSMSL.Proteomics;

namespace NeuQuant
{
    /// <summary>
    /// A set of quantitative labels
    /// </summary>
    public static class Reagents
    {
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

        #endregion

        public static Isotopologue K8Plex2 = new Isotopologue("K8_2", ModificationSites.K);
        public static Isotopologue K8Plex3 = new Isotopologue("K8_3", ModificationSites.K);

        public static Isotopologue K8SilacPlex2 = new Isotopologue("K8SilacPlex2", ModificationSites.K);
        
        static Reagents()
        {
            // Standard Duplex
            K8Plex2.AddModification(K080);
            K8Plex2.AddModification(K602);

            // Standard Triplex
            K8Plex3.AddModification(K080);
            K8Plex3.AddModification(K341);
            K8Plex3.AddModification(K602);

            K8SilacPlex2.AddModification(K602);
            K8SilacPlex2.AddModification("", "No Mod");

        }
        
    }
}

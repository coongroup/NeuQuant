using CSMSL.Chemistry;
using CSMSL.Proteomics;

namespace NeuQuant
{
    public class NeuQuantModification : ChemicalFormulaModification
    {
        public bool IsAminoAcid { get; private set; }
        public bool IsDefault { get; private set; }
        
        public NeuQuantModification(string chemicalFormula, string name, ModificationSites sites, bool isAminoAcid = false, bool isDefault = false)
            : base(chemicalFormula, name, sites)
        {
            IsDefault = isDefault;
            IsAminoAcid = isAminoAcid;
        }

        public NeuQuantModification(ChemicalFormula chemicalFormula, string name, ModificationSites sites, bool isAminoAcid = false, bool isDefault = false)
            : base(chemicalFormula, name, sites)
        {
            IsDefault = isDefault;
            IsAminoAcid = isAminoAcid;
        }

       
    }
}

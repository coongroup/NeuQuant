using CSMSL.Chemistry;
using CSMSL.Proteomics;

namespace NeuQuant
{
    public class ChemicalFormulaModification : Modification
    {
        public ChemicalFormula ChemicalFormula { get; private set; }
        public bool IsAminoAcid { get; private set; }
        public bool IsDefault { get; private set; }

        public ChemicalFormulaModification(string chemicalFormula, string name, ModificationSites sites, bool isAminoAcid = false, bool isDefault = false)
            : base(chemicalFormula, name, sites)
        {
            IsDefault = isDefault;
            ChemicalFormula = new ChemicalFormula(chemicalFormula);
            IsAminoAcid = isAminoAcid;
        }

        public ChemicalFormulaModification(ChemicalFormula chemicalFormula, string name, ModificationSites sites, bool isAminoAcid = false, bool isDefault = false)
            : base(chemicalFormula.MonoisotopicMass, name, sites)
        {
            IsDefault = isDefault;
            ChemicalFormula = chemicalFormula;
            IsAminoAcid = isAminoAcid;
        }
    }
}

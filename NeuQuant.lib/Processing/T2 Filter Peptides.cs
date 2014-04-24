using System.Collections.Generic;
using System.Linq;

namespace NeuQuant.Processing
{
    public partial class Processor
    {
        public List<NeuQuantPeptide> AllPeptides;
        public List<NeuQuantPeptide> QuantifiablePeptides;

        public List<NeuQuantPeptide> GetPeptides()
        {
            OnMessage("Loading Peptides...");
            OnProgress(0);
            AllPeptides = NqFile.GetPeptides().ToList();
            return AllPeptides;
        }

        public List<NeuQuantPeptide> FilterPeptides()
        {
            OnMessage("Filtering Peptides...");
            QuantifiablePeptides = AllPeptides.Where(pep => Resolvable(pep)).ToList();
            return QuantifiablePeptides;
        }

    }
}

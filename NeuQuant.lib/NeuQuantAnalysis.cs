using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuQuant
{
    public class NeuQuantAnalysis
    {
        public string Name { get; private set; }

        public Dictionary<DateTime, long> Analyses; 

        public NeuQuantAnalysis(string name)
        {
            Name = name;
            Analyses = new Dictionary<DateTime, long>();
        }

        public void AddAnalysis(string datetime, long analysisID)
        {
            DateTime dt = DateTime.Parse(datetime);
            Analyses.Add(dt, analysisID);
        }
    }
}

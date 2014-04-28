using System.Collections.Generic;
using CSMSL.Chemistry;

namespace NeuQuant
{
    public class NeuQuantSample
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public HashSet<IMass> Modifications { get; private set; }
        public int ID { get; private set; }

        public int NumberOfModifications { get { return Modifications.Count;} }

        public NeuQuantSample(string name, string description)
        {
            Name = name;
            Description = description;
            Modifications = new HashSet<IMass>();
        }

        public void AddModification(IMass modification)
        {
            if (Modifications.Add(modification))
            {

            }
        }





    }
}

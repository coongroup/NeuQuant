using System;
using System.Collections.Generic;
using System.Linq;
using CSMSL.Proteomics;

namespace NeuQuant
{
    public class NeuQuantSample : IEquatable<NeuQuantSample>
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public HashSet<Modification> Modifications { get; private set; }
        public long ID { get; internal set; }

        public int NumberOfModifications { get { return Modifications.Count;} }

        public NeuQuantSample(string name, string description)
        {
            Name = name;
            Description = description;
            Modifications = new HashSet<Modification>();
        }

        public void AddModification(Modification modification)
        {
            if (Modifications.Add(modification))
            {

            }
        }

        public override string ToString()
        {
            return Name;
        }

        public string GetModificationsString()
        {
            return string.Join(" | ", Modifications.Select(m => m.Name).ToArray());
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(NeuQuantSample other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (!Name.Equals(other.Name))
                return false;

            return true;
        }
    }
}

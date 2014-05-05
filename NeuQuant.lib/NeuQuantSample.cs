using System;
using System.Collections.Generic;
using System.Linq;
using CSMSL.Analysis.ExperimentalDesign;
using CSMSL.Proteomics;

namespace NeuQuant
{
    public class NeuQuantSample : IEquatable<NeuQuantSample>
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
      
        public long ID { get; internal set; }

        public ExperimentalCondition Condition { get; private set; }

        public int NumberOfModifications { get { return Condition.Count; } }

        public NeuQuantSample(string name, string description, ExperimentalCondition condition)
        {
            Name = name;
            Description = description;
            Condition = condition;
        }

        public override string ToString()
        {
            return Name;
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

using GeneticAlgorithmBot.Neat.ActivationStrategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.Genetic {
	internal class NodeGene : Gene {
		// X coordinate of a NodeGene when displaying
		public double X { get; set; }
		// Y coordinate of a NodeGene when displaying
		public double Y { get; set; }
		public IActivationFunction Activation { get; set; }
		public string ActivationName { get; set; }

		public NodeGene(int innovationNumber) : base(innovationNumber) { }

		public override bool Equals(object obj) {
			if (obj is NodeGene n) {
				return InnovationNumber.Equals(n.InnovationNumber);
			}
			return false;
		}

		// Get the object hashcode which in this case is the InnovationNumber
		public override int GetHashCode() {
			return InnovationNumber;
		}
	}
}

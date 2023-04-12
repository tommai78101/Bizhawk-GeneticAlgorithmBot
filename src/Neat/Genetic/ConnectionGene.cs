using GeneticAlgorithmBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class ConnectionGene : Gene {
		public NodeGene In { get; set; }
		public NodeGene Out { get; set; }
		public double Weight { get; set; }
		public bool Enabled { get; set; } = true;

		// Indicates if a ConnectionGene has been replaced
		public int ReplaceIndex { get; set; }

		public ConnectionGene(NodeGene inNode, NodeGene outNode) {
			this.In = inNode;
			this.Out = outNode;
		}

		public override bool Equals(object obj) {
			if (obj is ConnectionGene c) {
				return this.In.Equals(c.In) && this.Out.Equals(c.Out);
			}
			return false;
		}

		// Get the object hashcode which in this case is a computed number of In and Out InnovationNumbers
		public override int GetHashCode() {
			return In.InnovationNumber * Constants.MAX_NODES + Out.InnovationNumber;
		}
	}
}

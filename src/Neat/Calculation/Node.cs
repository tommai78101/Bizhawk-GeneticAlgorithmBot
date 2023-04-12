using GeneticAlgorithmBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class Node : IComparable<Node> {

		// X coordinate
		public double X { get; set; }
		public double Output { get; set; }
		public List<Connection> Connections { get; set; } = new List<Connection>();

		public IActivationFunction Activation { get; set; }

		public Node(double x, IActivationFunction activation) {
			this.X = x;
			this.Activation = activation;
		}

		public int CompareTo(Node other) {
			if (this.X > other.X)
				return -1;
			if (this.X < other.X)
				return 1;
			return 0;
		}

		public void Calculate() {
			double z = 0;
			foreach(Connection c in Connections) {
				if (c.Enabled) {
					z += c.Weight * c.In.Output;
				}
			}
			Output = Activation.Activate(z);
		}
	}
}

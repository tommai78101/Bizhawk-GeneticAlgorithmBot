using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.Calculation {
	internal class Connection {
		public Node In { get; set; }
		public double Weight { get; set; }
		public bool Enabled { get; set; } = true;

		public Connection(Node inNode) {
			this.In = inNode;
		}
	}
}

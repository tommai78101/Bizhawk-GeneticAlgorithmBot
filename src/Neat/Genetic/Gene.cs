using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public abstract class Gene {
		public int InnovationNumber { get; set; }

		public Gene(int innovationNumber) {
			this.InnovationNumber = innovationNumber;
		}

		public Gene() { }
	}
}

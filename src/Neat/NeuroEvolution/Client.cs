using GeneticAlgorithmBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class Client : IComparable<Client> {
		public IGenome Genome { get; set; }
		public double Score => ((NeatAlgorithm) Genome.Neat).GetCurrent().GetAttempt().Fitness;
		public Species Species { get; set; } = default!;
		public Calculator Calculator { get; set; }

		public int ClientId { get; set; } = -1;

		public Client(IGenome genome, int clientId) { 
			this.Genome = genome;
			this.ClientId = clientId;
			Calculator = new Calculator(genome);
		}

		// ====================================================================================================================================
		// NEAT functions

		public void RegenerateCalculator() => Calculator = new Calculator(Genome);
		
		public IList<double> Calculate(IList<double> inputs) => Calculator.Calculate(inputs);

		public double GetDistance(Client other) => Genome.GetDistance(other.Genome);

		public void Mutate() => Genome.Mutate();

		public int CompareTo(Client other) {
			if (Score > other.Score)
				return -1;
			if (Score < other.Score)
				return 1;
			return 0;
		}
	}
}

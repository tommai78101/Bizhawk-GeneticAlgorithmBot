using GeneticAlgorithmBot.Neat.Calculation;
using GeneticAlgorithmBot.Neat.Genetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.NeuroEvolution {
	internal class Client : IComparable<Client> {
		public IGenome Genome { get; set; }
		public double Score { get; set; }
		public Species Species { get; set; }
		public Calculator Calculator { get; set; }

		public Client(IGenome genome) { 
			this.Genome = genome;
			Calculator = new Calculator(genome);
		}

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

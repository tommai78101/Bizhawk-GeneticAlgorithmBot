using GeneticAlgorithmBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public interface IGenome {
		RandomList<ConnectionGene> Connections { get; }
		RandomList<NodeGene> Nodes { get; }
		INeat Neat { get; }

		// Manage mutation regarding their probabilities
		void Mutate();

		// Add a ConnectionGene between two existing NodeGenes.
		void MutateLink();

		// Add a NodeGene on an existing ConnectionGene.
		void MutateNode();

		// Activate or deactivate a random ConnectionGene.
		void MutateToggleLink();

		// Randomly change the weight of a random ConnectionGene
		void MutateWeightRandom();

		// Randomly shift the weight of a random ConnectionGene
		void MutateWeightShift();

		// Change the activation function of a random NodeGene
		void MutateActivationRandom();
	}
}

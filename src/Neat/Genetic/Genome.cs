using GeneticAlgorithmBot.Neat.ActivationStrategy;
using GeneticAlgorithmBot.Neat.Calculation;
using GeneticAlgorithmBot.Neat.Common;
using GeneticAlgorithmBot.Neat.NeuroEvolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.Genetic {
	internal class Genome : IGenome {
		public RandomList<ConnectionGene> Connections { get; }

		public RandomList<NodeGene> Nodes { get; }

		public INeat Neat { get; }

		public Genome(INeat neat) {
			Connections = new RandomList<ConnectionGene>();
			Nodes = new RandomList<NodeGene>();
			this.Neat = neat;
		}

		public void Mutate() {
			if (GenomeConstants.PROBABILITY_MUTATE_LINK > ThreadSafeRandom.GetRandom())
				MutateLink();
			if (GenomeConstants.PROBABILITY_MUTATE_NODE > ThreadSafeRandom.GetRandom())
				MutateNode();
			if (GenomeConstants.PROBABILITY_MUTATE_WEIGHT_RANDOM > ThreadSafeRandom.GetRandom())
				MutateWeightRandom();
			if (GenomeConstants.PROBABILITY_MUTATE_WEIGHT_SHIFT > ThreadSafeRandom.GetRandom())
				MutateWeightShift();
			if (GenomeConstants.PROBABILITY_MUTATE_TOGGLE_LINK > ThreadSafeRandom.GetRandom())
				MutateToggleLink();
			if (GenomeConstants.PROBABILITY_MUTATE_ACTIVATION_RANDOM > ThreadSafeRandom.GetRandom())
				MutateActivationRandom();
		}

		public void MutateActivationRandom() {
			NodeGene node = Nodes.GetRandomElement();
			if (node?.X > 0.1) {
				ActivationEnumeration a = ActivationEnumeration.GetRandom();
				node.Activation = a.Activation;
				node.ActivationName = a.Name;
			}
		}

		public void MutateLink() {
			for (int i = 0; i < 100; i++) {
				NodeGene a = Nodes.GetRandomElement();
				NodeGene b = Nodes.GetRandomElement();
				if (a == null || b == null || a.X.Equals(b.X))
					continue;

				ConnectionGene connection = a.X < b.X ?
					connection = new ConnectionGene(a, b) :
					connection = new ConnectionGene(b, a);
				if (Connections.Contains(connection))
					continue;

				connection = Neat.GetConnection(connection.In, connection.Out);
				connection.Weight += ThreadSafeRandom.GetNormalizedRandom(0f, 0.2f) * GenomeConstants.WEIGHT_SHIFT_STRENGTH;
				AddSorted(connection);
				return;
			}
		}

		public void MutateNode() {
			ConnectionGene connection = Connections.GetRandomElement();
			if (connection == null)
				return;

			NodeGene from = connection.In;
			NodeGene to = connection.Out;
			int replaceIndex = Neat.GetReplaceIndex(from, to);
			NodeGene middle;
			if (replaceIndex == 0) {
				ActivationEnumeration a = ActivationEnumeration.GetRandom();
				middle = Neat.CreateNode();
				middle.X = (from.X + to.X) / 2;
				middle.Y = ((from.Y + to.Y) / 2) + (ThreadSafeRandom.GetNormalizedRandom(0, 0.02f) / 2);
				middle.Activation = a.Activation;
				middle.ActivationName = a.Name;
				Neat.SetReplaceIndex(from, to, middle.InnovationNumber);
			}
			else {
				middle = Neat.GetNode(replaceIndex);
			}

			ConnectionGene connection1 = Neat.GetConnection(from, middle);
			ConnectionGene connection2 = Neat.GetConnection(middle, to);
			connection1.Weight = 1;
			connection2.Weight = connection.Weight;
			connection2.Enabled = connection.Enabled;
			connection.Enabled = false;
			Connections.Add(connection1);
			Connections.Add(connection2);
			Nodes.Add(middle);
		}

		public void MutateToggleLink() {
			ConnectionGene connection = Connections.GetRandomElement();
			if (connection != null) {
				connection.Enabled = !connection.Enabled;
			}
		}

		public void MutateWeightRandom() {
			ConnectionGene connection = Connections.GetRandomElement();
			if (connection != null) {
				connection.Weight = ThreadSafeRandom.GetNormalizedRandom(0, 0.2f) * GenomeConstants.WEIGHT_RANDOM_STRENGTH;
			}
		}

		public void MutateWeightShift() {
			ConnectionGene connection = Connections.GetRandomElement();
			if (connection != null) {
				connection.Weight += ThreadSafeRandom.GetNormalizedRandom(0, 0.2f) * GenomeConstants.WEIGHT_SHIFT_STRENGTH;
			}
		}

		// ====================================================================================================================
		// Private method

		private void AddSorted(ConnectionGene gene) {
			for (int i = 0; i < Connections.Count; i++) {
				int innovationNumber = Connections[i].InnovationNumber;
				if  (gene.InnovationNumber < innovationNumber) {
					Connections.Insert(i, gene);
					return;
				}
			}
			Connections.Add(gene);
		}
	}
}

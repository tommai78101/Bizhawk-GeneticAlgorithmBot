using GeneticAlgorithmBot.Neat.ActivationStrategy;
using GeneticAlgorithmBot.Neat.Common;
using GeneticAlgorithmBot.Neat.Genetic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.NeuroEvolution {
	internal class Neat : INeat {
		private Dictionary<ConnectionGene, ConnectionGene> AllConnections { get; set; }
		private RandomList<NodeGene> AllNodes { get; set; }
		private RandomList<Client> AllClients { get; set; }
		private RandomList<Species> AllSpecies { get; set; }

		public Neat() {
			AllConnections = new Dictionary<ConnectionGene, ConnectionGene>();
			AllNodes = new RandomList<NodeGene>();
			AllClients = new RandomList<Client>();
			AllSpecies = new RandomList<Species>();
			Reset();
		}


		public NodeGene CreateNode() {
			NodeGene node = new NodeGene(AllNodes.Count + 1);
			AllNodes.Add(node);
			return node;
		}

		public IGenome EmptyGenome() {
			IGenome g = new Genome(this);
			for (int i = 0; i < NeatConstants.InputSize + NeatConstants.OutputSize; i++) {
				g.Nodes.Add(GetNode(i + 1));
			}
			return g;
		}

		public ConnectionGene GetConnection(NodeGene from, NodeGene to) {
			ConnectionGene connection = new ConnectionGene(from, to);
			if (AllConnections.ContainsKey(connection)) {
				connection.InnovationNumber = AllConnections[connection].InnovationNumber;
			} else {
				connection.InnovationNumber = AllConnections.Count + 1;
				AllConnections.Add(connection, connection);
			}
			return connection;
		}

		public NodeGene GetNode(int id) {
			if (id <= AllNodes.Count) {
				return AllNodes[id - 1];
			}
			return CreateNode();
		}

		public int GetReplaceIndex(NodeGene node1, NodeGene node2) {
			ConnectionGene connection = new ConnectionGene(node1, node2);
			ConnectionGene data = AllConnections[connection];
			if (data == null)
				return 0;
			return data.ReplaceIndex;
		}

		public void SetReplaceIndex(NodeGene node1, NodeGene node2, int index) {
			AllConnections[new ConnectionGene(node1, node2)].ReplaceIndex = index;
		}

		public void Evolve() {
			GenerateSpecies();
			Kill();
			RemoveExtinguishedSpecies();
			Reproduce();
			Mutate();
			foreach (Client c in AllClients) {
				c.RegenerateCalculator();
			}
		}

		public void Kill() {
			foreach (Species s in AllSpecies) {
				s.Kill(1 - NeatConstants.SURVIVAL_RATE);
			}
		}

		public void RemoveExtinguishedSpecies() {
			for (int i = AllSpecies.Count - 1; i >= 0; i--) {
				if (AllSpecies[i].Count <= 1) {
					AllSpecies[i].Extinguish();
					AllSpecies.RemoveAt(i);
				}
			}
		}

		// ===========================================================================================================
		// Unit testing

		public RandomList<Client> CheckEvolutionProcess() {
			Neat neat = new Neat();

			double[] inputs = new double[NeatConstants.InputSize];
			for (int i = 0; i < NeatConstants.InputSize; i++) {
				inputs[i] = ThreadSafeRandom.GetNormalizedRandom();
			}

			for (int i = 0; i < 100; i++) {
				foreach (Client c in AllClients) {
					c.Score = c.Calculate(inputs)[0];
				}
				neat.Evolve();
				//neat.TraceClients();
				neat.TraceSpecies();
			}

			return neat.AllClients;
		}

		public void TraceClients() {
			Trace.WriteLine("-----------------------------------------");
			foreach (Client c in AllClients) {
				foreach (ConnectionGene g in c.Genome.Connections) {
					Trace.Write($"{g.InnovationNumber} ");
				}
				Trace.WriteLine("");
			}
		}

		public void TraceSpecies() {
			Trace.WriteLine("-----------------------------------------");
			foreach (Species s in AllSpecies) {
				Trace.WriteLine($"{s.GetHashCode()} {s.Score} {s.Count}");
			}
		}

		// ===========================================================================================================
		// Private methods

		private void Reset() {
			AllConnections.Clear();
			AllNodes.Clear();
			AllClients.Clear();

			for (int i = 0; i < NeatConstants.InputSize; i++) {
				NodeGene node = CreateNode();
				node.X = 0.1;
				node.Y = (i + 1) / (double) (NeatConstants.InputSize + 1);
			}

			for (int i = 0; i < NeatConstants.OutputSize; i++) {
				NodeGene node = CreateNode();
				node.X = 0.9;
				node.Y = (i + 1) / (double) (NeatConstants.OutputSize + 1);

				ActivationEnumeration a = ActivationEnumeration.GetRandom();
				node.Activation = a.Activation;
				node.ActivationName = a.Name;
			}

			for (int i = 0; i < NeatConstants.MaxClients; i++) {
				Client c = new Client(EmptyGenome());
				AllClients.Add(c);
			}
		}

		private void GenerateSpecies() {
			foreach (Species s in AllSpecies) {
				s.Reset();
			}
			foreach (Client c in AllClients) {
				if (c.Species != null)
					continue;
				bool hasFound = false;
				foreach (Species s in AllSpecies) {
					if (hasFound = s.Put(c))
						break;
				}
				if (!hasFound) {
					AllSpecies.Add(new Species(c));
				}
			}
			foreach (Species s in AllSpecies) {
				s.EvaluateScore();
			}
		}

		private void Reproduce() {
			RandomSelector<Species> selector = new RandomSelector<Species>();
			foreach (Species s in AllSpecies) {
				selector.Add(s, s.Score);
			}
			foreach (Client c in AllClients) {
				if (c.Species == null) {
					Species s = selector.GetRandom();
					c.Genome = s.Breed();
					s.ForcePut(c);
				}
			}
		}

		private void Mutate() {
			foreach (Client c in AllClients) {
				c.Mutate();
			}
		}
	}
}

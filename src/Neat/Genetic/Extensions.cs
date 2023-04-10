using GeneticAlgorithmBot.Neat.Common;
using GeneticAlgorithmBot.Neat.NeuroEvolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.Genetic {
	internal static class Extensions {
		// Clone a ConnectionGene.
		public static ConnectionGene GetConnection(this ConnectionGene connection) {
			ConnectionGene gene = new ConnectionGene(connection.In, connection.Out) {
				InnovationNumber = connection.InnovationNumber,
				Weight = connection.Weight,
				Enabled = connection.Enabled,
			};
			return gene;
		}

		// Compute the distance between two genetical informations as IGenome.
		public static double GetDistance(this IGenome genome1, IGenome genome2) {
			int highestInnovationNumber1 = 0;
			int highestInnovationNumber2 = 0;
			if (genome1.Connections.Count > 0) {
				highestInnovationNumber1 = genome1.Connections[genome1.Connections.Count - 1].InnovationNumber;
			}
			if (genome2.Connections.Count > 0) {
				highestInnovationNumber2 = genome2.Connections[genome2.Connections.Count - 1].InnovationNumber;
			}
			if (highestInnovationNumber1 < highestInnovationNumber2) {
				// Swap around.
				IGenome temp = genome1;
				genome1 = genome2;
				genome2 = temp;
			}

			int i1 = 0;
			int i2 = 0;
			int matchingCount = 0;
			int disjointCount = 0;
			double weightDifference = 0;
			while (i1 < genome1.Connections.Count && i2 < genome2.Connections.Count) {
				ConnectionGene gene1 = genome1.Connections[i1];
				ConnectionGene gene2 = genome2.Connections[i2];
				if (gene1.InnovationNumber == gene2.InnovationNumber) {
					matchingCount++;
					weightDifference += Math.Abs(gene1.Weight - gene2.Weight);
					i1++;
					i2++;
				}
				else if (gene1.InnovationNumber > gene2.InnovationNumber) {
					disjointCount++;
					i2++;
				}
				else {
					disjointCount++;
					i1++;
				}
			}

			int excessCount = genome1.Connections.Count - i1;
			double geneCountInTheLargerGenome = Math.Max(genome1.Connections.Count, genome2.Connections.Count);
			if (geneCountInTheLargerGenome < 20) {
				geneCountInTheLargerGenome = 1;
			}
			double meanWeightDifference = weightDifference / Math.Max(1, matchingCount);
			return ((Constants.C1_EXCESS_GENE * excessCount + Constants.C2_DISJOINT_GENE * disjointCount) / geneCountInTheLargerGenome) + Constants.C3_MEAN_WEIGHT_DIFFERENCE * meanWeightDifference;
		}

		// Create an offspring from two genetical informations given
		public static IGenome Crossover(this IGenome parent1, IGenome parent2) {
			INeat neat = parent1.Neat;
			IGenome offspringGenome = neat.EmptyGenome();

			int i1 = 0;
			int i2 = 0;
			while (i1 < parent1.Connections.Count && i2 < parent2.Connections.Count) {
				ConnectionGene gene1 = parent1.Connections[i1];
				ConnectionGene gene2 = parent2.Connections[i2];
				if (gene1.InnovationNumber == gene2.InnovationNumber) {
					if (ThreadSafeRandom.GetRandom() > 0.5) {
						offspringGenome.Connections.Add(gene1.GetConnection());
					}
					else {
						offspringGenome.Connections.Add(gene2.GetConnection());
					}
					i1++;
					i2++;
				}
				else if (gene1.InnovationNumber > gene2.InnovationNumber) {
					offspringGenome.Connections.Add(gene2.GetConnection());
					i2++;
				}
				else {
					offspringGenome.Connections.Add(gene1.GetConnection());
					i1++;
				}
			}
			while(i1 < parent1.Connections.Count) {
				ConnectionGene gene1 = parent1.Connections[i1];
				offspringGenome.Connections.Add(gene1.GetConnection());
				i1++;
			}
			foreach(ConnectionGene c in offspringGenome.Connections) {
				offspringGenome.Nodes.Add(c.In);
				offspringGenome.Nodes.Add(c.Out);
			}
			return offspringGenome;
		}
	}
}

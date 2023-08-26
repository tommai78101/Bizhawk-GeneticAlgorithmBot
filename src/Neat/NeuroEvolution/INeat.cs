using GeneticAlgorithmBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public interface INeat {
		// Create a NodeGene and and store it for future use
		NodeGene CreateNode();

		// Create a Genome as starter for a Client
		IGenome EmptyGenome();

		// Set a replace index as a marker between two NodeGenes.
		void SetReplaceIndex(NodeGene node1, NodeGene node2, int index);

		// Get a replace index if a NodeGene exists between two NodeGenes
		int GetReplaceIndex(NodeGene node1, NodeGene node2);

		// Find or create a ConnectionGene.
		ConnectionGene GetConnection(NodeGene from, NodeGene to);

		// Find or Create a NodeGene by id (this would be the index within the NodeGene list)
		NodeGene GetNode(int id);
	}
}

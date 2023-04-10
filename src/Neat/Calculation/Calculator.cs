using GeneticAlgorithmBot.Neat.Common;
using GeneticAlgorithmBot.Neat.Genetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneticAlgorithmBot.Neat.Calculation {
	internal class Calculator {
		readonly List<Node> InputNodes = new List<Node>();
		readonly List<Node> HiddenNodes = new List<Node>();
		readonly List<Node> OutputNodes = new List<Node>();

		public Calculator(IGenome genome) {
			RandomList<NodeGene> nodes = genome.Nodes;
			RandomList<ConnectionGene> connections = genome.Connections;
			Dictionary<int, Node> nodeDictionary = new Dictionary<int, Node>();
			
			foreach(NodeGene n in nodes) {
				Node node = new Node(n.X, n.Activation);
				nodeDictionary[n.InnovationNumber] = node;
				if (n.X <= 0.1) {
					InputNodes.Add(node);
				}
				else if (n.X >= 0.9) {
					OutputNodes.Add(node);
				} else {
					HiddenNodes.Add(node);
				}
			}
			HiddenNodes.Sort((Node n1, Node n2) => n1.CompareTo(n2));

			foreach (ConnectionGene c in connections) {
				Node from = nodeDictionary[c.In.InnovationNumber];
				Node to = nodeDictionary[c.Out.InnovationNumber];
				Connection connection = new Connection(from) {
					Enabled = c.Enabled,
					Weight = c.Weight
				};
				to.Connections.Add(connection);
			}
		}

		public IList<double> Calculate(IList<double> inputs) {
			if (inputs.Count() != InputNodes.Count) {
				throw new System.Exception("Data doesn't fit.");
			}

			for (int i = 0; i < InputNodes.Count; i++) {
				InputNodes[i].Output = inputs[i];
			}

			foreach (Node n in HiddenNodes) {
				n.Calculate();
			}

			double[] outputs = new double[InputNodes.Count];
			for (int i =0; i < OutputNodes.Count; i++) {
				OutputNodes[i].Calculate();
				outputs[i] = OutputNodes[i].Output;
			}

			return outputs;
		}
	}
}

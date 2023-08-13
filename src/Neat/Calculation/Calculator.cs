using GeneticAlgorithmBot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneticAlgorithmBot {
	public class Calculator {
		readonly List<Node> InputNodes = new List<Node>();
		readonly List<Node> HiddenNodes = new List<Node>();
		readonly List<Node> OutputNodes = new List<Node>();
		public IList<double> previousOutputs;
		private IGenome _genome;

		public Calculator(IGenome genome) {
			this._genome = genome;
			this.previousOutputs = new List<double>();
			RandomList<NodeGene> nodes = genome.Nodes;
			RandomList<ConnectionGene> connections = genome.Connections;
			Dictionary<int, Node> nodeDictionary = new Dictionary<int, Node>();

			foreach (NodeGene n in nodes) {
				Node node = new Node(n.X, n.Activation!);
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

		/// <summary>
		/// Calculation is handled by first creating an empty genome with the correct amount of input nodes as the calculator's given inputs.
		/// From here, each of the input node's output value is set to the calculator's given input value.
		/// And then finally, we begin calculating the output node's results via the hidden nodes.
		/// </summary>
		/// <param name="inputs"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public IList<double> Calculate(ExtendedColorWrapper[] inputs) {
			if (InputNodes.Count != inputs.Length) {
				throw new Exception("Data doesn't fit.");
			}

			// Insert the input values to the Calculator's input nodes.
			for (int i = 0; i < InputNodes.Count; i++) {
				Color inputColor = inputs[i].ExtendedColor.ToColor();
				InputNodes[i].Output = inputColor.GetBrightness();
			}

			// Begin calculations.
			foreach (Node n in HiddenNodes) {
				n.Calculate();
			}

			// Obtain each of the output node's value.
			double[] outputs = new double[OutputNodes.Count];
			for (int i = 0; i < OutputNodes.Count; i++) {
				OutputNodes[i].Calculate();
				outputs[i] = OutputNodes[i].Output;
			}

			return outputs;
		}

		public int GetInputSize() {
			return InputNodes.Count;
		}

		private double Step(double value) {
			double result = 0.0;
			switch (value) {
				case < 0.25:
					result = 0.0;
					break;
				case < 0.50:
					result = 0.25;
					break;
				case < 0.75:
					result = 0.5;
					break;
				case < 1.0:
					result = 0.75;
					break;
				case >= 1.0:
					result = 1.0;
					break;
				default:
					break;
			}
			return result;
		}	
	}
}

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using GeneticAlgorithmBot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class NeatAlgorithm : BotAlgorithm, INeat {
		public Dictionary<ConnectionGene, ConnectionGene> AllConnections { get; set; }
		public RandomList<NodeGene> AllNodes { get; set; }
		public RandomList<Client> AllClients { get; set; }
		public RandomList<Species> AllSpecies { get; set; }

		public List<NodeGene> InputNodes { get; set; }
		public List<NodeGene> OutputNodes { get; set; }

		public bool isFirstCalculation = false;

		public NeatAlgorithm(GeneticAlgorithmBot owner) : base(owner) {
			this.IsInitialized = false;
			AllConnections = new Dictionary<ConnectionGene, ConnectionGene>();
			AllNodes = new RandomList<NodeGene>();
			AllClients = new RandomList<Client>();
			AllSpecies = new RandomList<Species>();
			InputNodes = new List<NodeGene>();
			OutputNodes = new List<NodeGene>();
		}

		// =======================================================================================================================
		// BizHawk specific functions

		public override BotAlgorithm Initialize() {
			this.Reset();

			this.population = new InputRecording[this.bot.PopulationSize]!;
			for (int i = 0; i < this.population.Length; i++) {
				this.population[i] = new InputRecording(this)!;
				this.population[i].Reset(0);
			}

			this.SetOrigin();
			this.currentIndex = 0;
			this.Generation = 1;
			this.StartFrameNumber = this.bot._startFrame;
			this.IsInitialized = true;

			return this;
		}

		public void ClearBest() {
			this.bestRecording.Reset(0);
		}

		public bool NextRecording() {
			this.currentIndex = ++this.currentIndex % this.population.Length;
			return this.currentIndex == 0;
		}

		public bool IsCurrentAttemptBetter() {
			BotAttempt current = this.population[this.currentIndex].GetAttempt();
			BotAttempt best = this.bestRecording.GetAttempt();
			return Utils.IsAttemptBetter(this.bot, best, this.bot.comparisonAttempt, current);
		}

		public void CopyCurrentToBest(int index) {
			this.bestRecording.DeepCopy(this.population[index]);
			this.bestRecording.IsSet = true;
		}

		public void SetOrigin() {
			BotAttempt origin = this.GetBest().GetAttempt();
			origin.Fitness = 0;
			origin.Attempt = 0;
			origin.Generation = 1;
			origin.ComparisonTypeMain = this.bot.MainComparisonType;
			origin.ComparisonTypeTie1 = this.bot.Tie1ComparisonType;
			origin.ComparisonTypeTie2 = this.bot.Tie2ComparisonType;
			origin.ComparisonTypeTie3 = this.bot.Tie3ComparisonType;
			origin.Maximize = this.bot.MaximizeValue;
			origin.TieBreak1 = this.bot.TieBreaker1Value;
			origin.TieBreak2 = this.bot.TieBreaker2Value;
			origin.TieBreak3 = this.bot.TieBreaker3Value;
			origin.isReset = false;
		}

		public Client GetCurrentClient() {
			return this.AllClients.First((c) => c.ClientId == this.currentIndex);
		}

		public override long EvaluateGeneration() {
			int chosenIndex = -1;
			for (int i = 0; i < this.population.Length; i++) {
				if (Utils.IsAttemptBetter(this.bot, this.GetBest().result, this.bot.comparisonAttempt, this.population[i].result)) {
					chosenIndex = i;
				}
				// After evaluation, we can discard the input recording in the population pool.
				this.population[i].IsSet = false;
			}
			if (chosenIndex > -1) {
				CopyCurrentToBest(chosenIndex);
			}
			return ++this.Generation;
		}

		// =======================================================================================================================
		// NEAT functions

		public NodeGene CreateNode() {
			NodeGene node = new NodeGene(AllNodes.Count + 1);
			node.X = Utils.RNG.NextDouble();
			node.Y = Utils.RNG.NextDouble();
			AllNodes.Add(node);
			return node;
		}

		public void RemoveNode(string nodeName) {
			for (int i = 0; i < AllNodes.Count; i++) {
				NodeGene node = AllNodes[i];
				if (node.NodeName != null && node.NodeName.Equals(nodeName)) {
					AllNodes.Remove(node);
					if (InputNodes.Contains(node)) {
						InputNodes.Remove(node);
					}
					if (OutputNodes.Contains(node)) {
						OutputNodes.Remove(node);
					}
					break;
				}
			}
		}

		public IGenome EmptyGenome() {
			IGenome g = new Genome(this);
			int actualOutputSize = NeatConstants.OutputSize;
			if (this.bot.neatMappings.HasControls) {
				actualOutputSize = this.bot.neatMappings.GetEnabledMappings().Count;
			}
			for (int i = 0; i < NeatConstants.InputSize + actualOutputSize; i++) {
				g.Nodes.Add(GetNode(i + 1));
			}
			return g;
		}

		public ConnectionGene GetConnection(NodeGene from, NodeGene to) {
			ConnectionGene connection = new ConnectionGene(from, to);
			if (AllConnections.ContainsKey(connection)) {
				connection.InnovationNumber = AllConnections[connection].InnovationNumber;
			}
			else {
				connection.InnovationNumber = AllConnections.Count + 1;
				AllConnections.Add(connection, connection);
			}
			return connection;
		}

		public NodeGene GetNode(int id) {
			if (id <= AllNodes.Count) {
				return AllNodes[id - 1]!;
			}
			NodeGene newNode = CreateNode();
			if (newNode.X <= 0.1) {
				InputNodes.Add(newNode);
			}
			else if (newNode.X >= 0.9) {
				OutputNodes.Add(newNode);
			}
			return newNode;
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
				if (AllSpecies[i]!.Count <= 1) {
					AllSpecies[i]!.Extinguish();
					AllSpecies.RemoveAt(i);
				}
			}
		}

		public void Reset() {
			AllConnections.Clear();
			AllNodes.Clear();
			AllClients.Clear();
			InputNodes.Clear();
			OutputNodes.Clear();
			this.bot._inputSampleSize = (int) this.bot.InputSampleSize.Value;
			this.bot._inputX = GetClosest((int) this.bot.InputRegionX.Value, this.bot._inputSampleSize);
			this.bot._inputY = GetClosest((int) this.bot.InputRegionY.Value, this.bot._inputSampleSize);
			this.bot._inputWidth = GetClosest((int) this.bot.InputRegionWidth.Value, this.bot._inputSampleSize);
			this.bot._inputHeight = GetClosest((int) this.bot.InputRegionHeight.Value, this.bot._inputSampleSize);

			// Must set this up first.
			NeatConstants.InputSize = (this.bot._inputWidth * this.bot._inputHeight) / (this.bot._inputSampleSize * this.bot._inputSampleSize);
			for (int i = 0; i < NeatConstants.InputSize; i++) {
				NodeGene node = CreateNode();
				node.X = 0.1;
				node.Y = (i + 1) / (double) (NeatConstants.InputSize + 1);
				node.NodeName = null;
				InputNodes.Add(node);
			}
			this.bot._neatInputRegionData = new ExtendedColorWrapper[NeatConstants.InputSize];
			for (int i = 0; i < NeatConstants.InputSize; i++) {
				this.bot._neatInputRegionData[i] = new ExtendedColorWrapper(Color.Transparent); 
			}

			NeatConstants.OutputSize = this.bot.ControllerButtons.Count;
			int actualOutputSize = this.bot.neatMappings.HasControls ? this.bot.neatMappings.GetEnabledMappings().Count : NeatConstants.OutputSize;
			double min = 1.0 / (double) (actualOutputSize + 1);
			double max = (double) actualOutputSize / (double) (actualOutputSize + 1);
			for (int i = 0, yPos = 0; i < this.bot.ControllerButtons.Count; i++) {
				string button = this.bot.ControllerButtons[i];
				NeatMappingRow? row = this.bot.neatMappings.GetRow(button);
				if (this.bot.neatMappings.HasControls && row == null) {
					continue;
				}
				NodeGene node = CreateNode();
				node.X = 0.9;
				node.Y = Utils.Normalize(((yPos++) + 1), 1, actualOutputSize, min, max);

				ActivationEnumeration a = ActivationEnumeration.GetRandom();
				node.Activation = a.Activation;
				node.NodeName = button;

				OutputNodes.Add(node);
			}

			NeatConstants.MaxClients = this.bot.PopulationSize;
			for (int i = 0; i < NeatConstants.MaxClients; i++) {
				Client c = new Client(EmptyGenome(), i);
				AllClients.Add(c);
			}

			this.bestRecording = new InputRecording(this);
			this.isFirstCalculation = true;
		}

		// ===========================================================================================================
		// Private methods

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
			if (AllSpecies.Count < 2) {
				// They can't reproduce if there's less than 2 species.
				return;
			}
			RandomSelector<Species> selector = new RandomSelector<Species>();
			foreach (Species s in AllSpecies) {
				selector.Add(s, s.Score);
			}
			foreach (Client c in AllClients) {
				if (c.Species == null) {
					Species s = selector.GetRandom()!;
					c.Genome = s!.Breed();
					s!.ForcePut(c);
				}
			}
		}

		private void Mutate() {
			foreach (Client c in AllClients) {
				c.Mutate();
			}
		}

		private int GetClosest(int n, int x) {
			n += x / 2;
			n -= (n % x);
			return n;
		}
	}
}

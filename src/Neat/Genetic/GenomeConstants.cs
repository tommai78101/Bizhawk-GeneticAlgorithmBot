using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	internal sealed class GenomeConstants {
		// Weight shift strength
		public static readonly double WEIGHT_SHIFT_STRENGTH = 0.3;

		// Weight random strength
		public static readonly double WEIGHT_RANDOM_STRENGTH = 0.13;

		// Probability to add a connection
		public static readonly double PROBABILITY_MUTATE_LINK = 0.12;

		// Probability to add a node
		public static readonly double PROBABILITY_MUTATE_NODE = 0.05;

		// Probability to shift a weight
		public static readonly double PROBABILITY_MUTATE_WEIGHT_SHIFT = 0.12;

		// Probability to change a weight
		public static readonly double PROBABILITY_MUTATE_WEIGHT_RANDOM = 0.02;

		// Probability to activate or deactivate a connection
		public static readonly double PROBABILITY_MUTATE_TOGGLE_LINK = 0.02;

		// Probability to change the activation function of a node
		public static readonly double PROBABILITY_MUTATE_ACTIVATION_RANDOM = 0.02;
	}
}

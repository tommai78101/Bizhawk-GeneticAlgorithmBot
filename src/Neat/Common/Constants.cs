using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.Common {
	internal sealed class Constants {
		// Number of maximum nodes.
		public static readonly int MAX_NODES = (int) Math.Pow(2, 10);

		// Threshold that decides when two clients are too genetically distant under its value
		public static readonly double CP_GENETICALLY_DISTANT = 4;

		// Importance of the excess gene ratio when creating an offspring
		public static readonly double C1_EXCESS_GENE = 1;

		// Importance of the disjoint gene ratio when creating an offspring
		public static readonly double C2_DISJOINT_GENE = 1;

		// Importance of the mean weight difference ratio through genes when creating an offspring
		public static readonly double C3_MEAN_WEIGHT_DIFFERENCE = 1;
	}
}

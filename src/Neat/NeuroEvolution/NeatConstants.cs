using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.NeuroEvolution {
	internal sealed class NeatConstants {
		public static readonly int InputSize = 3;
		public static readonly int OutputSize = 2;
		public static readonly int MaxClients = 1000;
		public static readonly double SURVIVAL_RATE = 0.8;
	}
}

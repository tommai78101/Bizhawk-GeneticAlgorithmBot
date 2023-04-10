using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.ActivationStrategy {
	internal class AbsoluteFunction : IActivationFunction {
		public double Activate(double z) => Math.Abs(z);
	}
}

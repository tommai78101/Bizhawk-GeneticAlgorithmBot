using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.ActivationStrategy {
	internal class EluFunction : IActivationFunction {
		public double Activate(double z) => z > 0 ? z : Math.Exp(z) - 1;
	}
}

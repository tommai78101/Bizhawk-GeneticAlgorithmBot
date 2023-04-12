using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class SigmoidFunction : IActivationFunction {
		public double Activate(double z) => 1.0 / (1.0 + Math.Exp(-z));
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class ReluFunction : IActivationFunction {
		public string Name => ActivationNames.Relu;

		public double Activate(double z) => z > 0 ? z : 0;
	}
}

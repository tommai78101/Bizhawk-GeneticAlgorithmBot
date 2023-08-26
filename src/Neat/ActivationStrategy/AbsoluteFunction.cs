using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class AbsoluteFunction : IActivationFunction {
		public string Name => ActivationNames.Absolute;

		public double Activate(double z) => Math.Abs(z);
	}
}

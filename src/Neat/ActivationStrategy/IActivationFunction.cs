using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.ActivationStrategy {
	internal interface IActivationFunction {
		double Activate(double z);
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public interface IActivationFunction {
		double Activate(double z);
	}
}

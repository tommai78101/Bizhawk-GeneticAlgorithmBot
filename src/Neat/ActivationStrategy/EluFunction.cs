﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class EluFunction : IActivationFunction {
		public string Name { get => ActivationNames.Elu; }
		public double Activate(double z) => z > 0 ? z : Math.Exp(z) - 1;
	}
}

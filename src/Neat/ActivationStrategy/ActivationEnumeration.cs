using GeneticAlgorithmBot.Neat.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Neat.ActivationStrategy {
	internal class ActivationEnumeration : ActivationBase {
		// Abs : Absolute value, f(x) = |x|
		public static readonly ActivationEnumeration Absolute = new ActivationEnumeration(new AbsoluteFunction(), ActivationNames.Absolute);

		// ELu : Exponential linear unit, f(a,x) = {a (exp(x-1)) | x<=0 , x | x > 0}
		public static readonly ActivationEnumeration Elu = new ActivationEnumeration(new EluFunction(), ActivationNames.Elu);

		// ReLU : Rectified linear unit, f(a,x) = {0 | x<=0 , x | x > 0} = max{0 , x}
		public static readonly ActivationEnumeration Relu = new ActivationEnumeration(new ReluFunction(), ActivationNames.Relu);

		// Sigmoid : f(x) = 1 / (1 + exp(-x))
		public static readonly ActivationEnumeration Sigmoid = new ActivationEnumeration(new SigmoidFunction(), ActivationNames.Sigmoid);

		private ActivationEnumeration(IActivationFunction activation, string name) : base(activation, name) { }

		public static ActivationEnumeration GetRandom() {
			int count = GetAll<ActivationEnumeration>().Count();
			int i = (int) (ThreadSafeRandom.GetRandom() * count);
			return GetAll<ActivationEnumeration>().ToList()[i];
		}
	}
}

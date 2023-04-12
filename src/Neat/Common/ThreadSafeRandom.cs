using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	internal static class ThreadSafeRandom {
		[ThreadStatic]
		private static Random? LocalRandom;

		private static Random LocalThreadRandom {
			get {
				return LocalRandom ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));
			}
		}

		public static double GetRandom() {
			return LocalThreadRandom.NextDouble();
		}

		// Creates a random double under the normal distribution probability law (Laplace-Gauss distribution)
		public static double GetNormalizedRandom(double mean = 0, double scale = 1) {
			double random1 = 1.0 - LocalThreadRandom.NextDouble();
			double random2 = 1.0 - LocalThreadRandom.NextDouble();
			return mean + scale * Math.Sqrt(-2.0f * Math.Log(random1)) * Math.Sin(2.0f * Math.PI * random2);
		}
	}
}

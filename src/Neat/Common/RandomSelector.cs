using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class RandomSelector<T> {
		private readonly List<T> Elements = new List<T>();
		private readonly List<double> Scores = new List<double>();

		private double totalScore = 0;

		public void Add(T element, double score) {
			Elements.Add(element);
			Scores.Add(score);
			totalScore += score;
		}

		public T GetRandom() {
			double value = ThreadSafeRandom.GetRandom() * totalScore;
			double constant = 0;
			for (int i =0; i < Elements.Count; i++) {
				constant += Scores[i];
				if (constant >= value) {
					return Elements[i];
				}
			}
			if (Elements.Count == 1)
				return Elements[0];
			return default!;
		}
	}
}

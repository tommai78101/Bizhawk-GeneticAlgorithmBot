using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class GeneticAlgorithm : BotAlgorithm {
		public GeneticAlgorithmBot bot;
		public bool IsBestSet => this.bestRecording.IsSet;

		public GeneticAlgorithm(GeneticAlgorithmBot owner) {
			this.bot = owner;
			this.IsInitialized = false;
			this.Generation = 1;
			this.bestRecording = new InputRecording(owner, this);
			this.population = new InputRecording[1];
			for (int i = 0; i < this.population.Length; i++) {
				this.population[i] = new InputRecording(owner, this);
			}
		}

		public void ClearBest() {
			this.bestRecording.Reset(0);
		}

		// Returns true if the current index wraps back to zero.
		public bool NextRecording() {
			this.currentIndex = ++this.currentIndex % this.population.Length;
			return this.currentIndex == 0;
		}

		public long EvaluateGeneration() {
			int chosenIndex = -1;
			for (int i = 0; i < this.population.Length; i++) {
				if (Utils.IsGeneticBetter(this.bot, this.GetBest().result, this.bot.comparisonAttempt, this.population[i].result)) {
					chosenIndex = i;
				}
				// After evaluation, we can discard the input recording in the population pool.
				this.population[i].IsSet = false;
			}
			if (chosenIndex > -1) {
				CopyCurrentToBest(chosenIndex);
			}
			return ++this.Generation;
		}

		public bool IsCurrentAttemptBetter() {
			BotAttempt current = this.population[this.currentIndex].GetAttempt();
			BotAttempt best = this.bestRecording.GetAttempt();
			return Utils.IsGeneticBetter(this.bot, best, this.bot.comparisonAttempt, current);
		}

		public void CopyCurrentToBest(int index) {
			this.bestRecording.DeepCopy(this.population[index]);
			this.bestRecording.IsSet = true;
		}

		public void SetOrigin() {
			BotAttempt origin = this.GetBest().GetAttempt();
			origin.Fitness = 0;
			origin.Attempt = 0;
			origin.Generation = 1;
			origin.ComparisonTypeMain = this.bot.MainComparisonType;
			origin.ComparisonTypeTie1 = this.bot.Tie1ComparisonType;
			origin.ComparisonTypeTie2 = this.bot.Tie2ComparisonType;
			origin.ComparisonTypeTie3 = this.bot.Tie3ComparisonType;
			origin.Maximize = this.bot.MaximizeValue;
			origin.TieBreak1 = this.bot.TieBreaker1Value;
			origin.TieBreak2 = this.bot.TieBreaker2Value;
			origin.TieBreak3 = this.bot.TieBreaker3Value;
			origin.isReset = false;
		}

		public void Initialize() {
			this.SetOrigin();
			this.currentIndex = 0;
			this.Generation = 1;
			this.StartFrameNumber = this.bot._startFrame;
			this.population = new InputRecording[this.bot.PopulationSize];
			for (int i = 0; i < this.population.Length; i++) {
				this.population[i] = new InputRecording(this.bot, this);
				this.population[i].Reset(0);
				this.population[i].RandomizeInputRecording();
			}
			this.IsInitialized = true;
		}

		public void Reproduce() {
			for (int i = 0; i < this.population.Length; i++) {
				InputRecording child = this.population[i];
				InputRecording chosen = this.IsBestSet ? this.GetBest() : child;

				// Uniform distribution crossover.
				for (int f = 0; f < child.FrameLength; f++) {
					if (Utils.RNG.Next((int) Math.Floor(100.0 / Utils.CROSSOVER_RATE)) == 0) {
						child.recording[f].DeepCopy(chosen.recording[f]);
					}
				}

				// Uniform distribution mutation.
				for (int rate = 0; rate < child.FrameLength; rate++) {
					if (Utils.RNG.NextDouble() <= decimal.ToDouble(this.bot.MutationRate)) {
						child.RandomizeFrameInput();
					}
				}
			}
		}
	}

}

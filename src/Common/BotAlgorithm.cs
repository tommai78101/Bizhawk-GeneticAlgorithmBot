﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public abstract class BotAlgorithm {
		public int StartFrameNumber { get; set; } = 0;
		public long Generation { get; set; }
		public int currentIndex { get; set; } = 0;
		public bool IsInitialized { get; set; } = false;

		public virtual InputRecording[] population { get; set; } = default!;
		public virtual InputRecording bestRecording { get; set; } = default!;

		public GeneticAlgorithmBot bot;

		public BotAlgorithm(GeneticAlgorithmBot owner) {
			this.bot = owner;
		}

		#region Abstract Methods
		public abstract BotAlgorithm Initialize();

		public abstract long EvaluateGeneration();

		public abstract void Update(bool isFastUpdate);
		#endregion

		public InputRecording GetCurrent() {
			return this.population[this.currentIndex];
		}

		public void ClearCurrentRecordingLog() {
			this.GetCurrent().GetAttempt().Log.Clear();
		}

		public void SetCurrentRecordingLog(string log) {
			this.GetCurrent().GetAttempt().Log.Add(log);
		}

		public InputRecording GetBest() {
			return this.bestRecording;
		}
	}
}

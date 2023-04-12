using System;
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

		public InputRecording[] population { get; set; } = default!;
		public InputRecording bestRecording = default!;

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

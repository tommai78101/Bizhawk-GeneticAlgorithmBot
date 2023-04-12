using BizHawk.Client.EmuHawk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class InputRecording {
		// If result "isReset" flag is set, the input recording hasn't started its attempt. Otherwise, any attempt counts as some result (success, skipped, fail).
		public BotAttempt result;
		public FrameInput[] recording;
		public double fitness { get; set; }
		public int FrameLength { get; set; }
		public bool IsSet { get; set; }
		private GeneticAlgorithmBot bot;
		private BotAlgorithm manager;

		public InputRecording(GeneticAlgorithmBot owner, BotAlgorithm parent) {
			this.bot = owner;
			this.manager = parent;
			this.IsSet = false;
			this.FrameLength = owner.FrameLength;
			this.recording = new FrameInput[owner.FrameLength];
			for (int i = 0; i < owner.FrameLength; i++) {
				this.recording[i] = new FrameInput(i);
			}
			result = new BotAttempt();
		}

		public BotAttempt GetAttempt() {
			return this.result;
		}

		public FrameInput GetFrameInput(int frameNumber) {
			int index = frameNumber - this.manager.StartFrameNumber;
			if (index < 0 || index >= this.recording.Length) {
				index = this.recording.Length - 1;
			}
			return this.recording[index];
		}

		public void SetFrameInput(int index, FrameInput input) {
			HashSet<string> copy = new HashSet<string>();
			copy.UnionWith(input.Buttons);
			if (0 <= index && index < this.recording.Length) {
				this.recording[index].Buttons.Clear();
				this.recording[index].Buttons.UnionWith(copy);
			}
			this.IsSet = true;
		}

		public void RandomizeInputRecording() {
			float[] probabilities = bot.GetCachedInputProbabilitiesFloat();
			IList<int[]> a = Enumerable.Range(0, this.bot.FrameLength).Select(run => {
				int[] times = Enumerable.Range(0, this.bot.ControllerButtons.Count)
					.Where((buttonIndex, i) => Utils.RNG.NextDouble() < probabilities[buttonIndex])
					.ToArray();
				return times;
			}).ToArray();
			int[][] values = a.ToArray();

			int length = values.Length;
			if (values.Length != this.bot.FrameLength) {
				length = this.bot.FrameLength;
			}

			for (int i = 0; i < length; i++) {
				FrameInput input = this.GetFrameInput(this.manager.StartFrameNumber + i);
				for (int j = 0; j < values[i].Length; j++) {
					input.Pressed(this.bot.ControllerButtons[values[i][j]]);
				}
			}
			this.IsSet = true;
		}

		public void RandomizeFrameInput() {
			int frameNumber = Utils.RNG.Next(bot._startFrame, bot._startFrame + this.recording.Length);
			int index = frameNumber - bot._startFrame;
			FrameInput input = this.GetFrameInput(frameNumber);
			input.Clear();

			float[] probabilities = bot.GetCachedInputProbabilitiesFloat();
			int[] times = Enumerable.Range(0, count: this.bot.ControllerButtons.Count)
					.Where((buttonIndex, i) => Utils.RNG.NextDouble() < probabilities[buttonIndex])
					.ToArray();

			for (int i = 0; i < times.Length; i++) {
				input.Pressed(this.bot.ControllerButtons[times[i]]);
			}
			this.IsSet = true;
		}

		public FrameInput GenerateFrameInput(int frameNumber, IList<double> thresholds) {
			FrameInput frameInput = GetFrameInput(frameNumber);
			frameInput.Clear();
			int[] times = Enumerable.Range(0, count: this.bot.ControllerButtons.Count)
					.Where((buttonIndex, i) => Utils.RNG.NextDouble() < thresholds[buttonIndex])
					.ToArray();
			for (int i = 0; i < times.Length; i++) {
				frameInput.Pressed(this.bot.ControllerButtons[times[i]]);
			}
			this.IsSet = true;
			return frameInput;
		}

		public void SetResult() {
			this.result.Attempt = this.bot.Runs;
			this.result.Generation = this.bot.Generations;
			this.result.Maximize = this.bot.MaximizeValue;
			this.result.TieBreak1 = this.bot.TieBreaker1Value;
			this.result.TieBreak2 = this.bot.TieBreaker2Value;
			this.result.TieBreak3 = this.bot.TieBreaker3Value;
			this.result.ComparisonTypeMain = this.bot.MainComparisonType;
			this.result.ComparisonTypeTie1 = this.bot.Tie1ComparisonType;
			this.result.ComparisonTypeTie2 = this.bot.Tie2ComparisonType;
			this.result.ComparisonTypeTie3 = this.bot.Tie3ComparisonType;
			this.IsSet = true;
			this.bot.ClearBestButton.Enabled = true;
		}

		public void Reset(long attemptNumber) {
			this.result.Attempt = attemptNumber;
			this.result.Generation = 1;
			this.result.Maximize = 0;
			this.result.TieBreak1 = 0;
			this.result.TieBreak2 = 0;
			this.result.TieBreak3 = 0;
			this.result.Log.Clear();
			this.result.isReset = true;
			this.IsSet = false;
		}

		public void DeepCopy(InputRecording other) {
			this.result = new BotAttempt(other.result);
			this.fitness = other.fitness;
			this.FrameLength = other.FrameLength;
			this.bot = other.bot;
			this.manager = other.manager;
			this.IsSet = other.IsSet;

			this.recording = new FrameInput[other.recording.Length];
			for (int i = 0; i < other.recording.Length; i++) {
				this.recording[i] = new FrameInput(i);
				this.recording[i].DeepCopy(other.recording[i]);
			}
		}
	}
}

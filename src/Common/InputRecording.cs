using BizHawk.Client.Common.Filters;
using BizHawk.Client.EmuHawk;
using BizHawk.Emulation.Common;
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
		private BotAlgorithm manager;
		private GeneticAlgorithmBot Bot => this.manager.bot;

		public InputRecording(BotAlgorithm parent) {
			this.manager = parent;
			this.IsSet = false;
			this.FrameLength = this.Bot.FrameLength;
			this.recording = new FrameInput[this.Bot.FrameLength];
			for (int i = 0; i < this.Bot.FrameLength; i++) {
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

		public void MemorizeInputRecording() {
			int inputSize = this.Bot.Emulator.ControllerDefinition.BoolButtons.Count;
			for (int i = 0; i < this.Bot.FrameLength; i++) {
				FrameInput input = this.GetFrameInput(this.manager.StartFrameNumber + i);
				IReadOnlyDictionary<string, object> inputDictionary = this.Bot._movieApi.GetInput(this.manager.StartFrameNumber + i);
				for (int j = 0; j < inputSize; j++) {
					string button = this.Bot.Emulator.ControllerDefinition.BoolButtons[j];
					if (inputDictionary[button] is bool buttonPressed && buttonPressed) {
						input.Pressed(button);
					}
				}
			}
			this.IsSet = true;
		}

		public void RandomizeInputRecording() {
			double[] probabilities = Bot.GetNeatOutputNodesProbabilitiesDouble();
			IList<int[]> a = Enumerable.Range(0, this.Bot.FrameLength).Select(run => {
				int[] times = Enumerable.Range(0, probabilities.Length)
					.Where((buttonIndex, i) => Utils.RNG.NextDouble() < probabilities[buttonIndex])
					.ToArray();
				return times;
			}).ToArray();
			int[][] values = a.ToArray();

			int length = values.Length;
			if (values.Length != this.Bot.FrameLength) {
				length = this.Bot.FrameLength;
			}

			for (int i = 0; i < length; i++) {
				FrameInput input = this.GetFrameInput(this.manager.StartFrameNumber + i);
				for (int j = 0; j < values[i].Length; j++) {
					input.Pressed(this.Bot.ControllerButtons[values[i][j]]);
				}
			}
			this.IsSet = true;
		}

		public void RandomizeFrameInput() {
			int frameNumber = Utils.RNG.Next(Bot._startFrame, Bot._startFrame + this.recording.Length);
			int index = frameNumber - Bot._startFrame;
			FrameInput input = this.GetFrameInput(frameNumber);
			input.Clear();

			float[] probabilities = Bot.GetCachedInputProbabilitiesFloat();
			int[] times = Enumerable.Range(0, count: this.Bot.ControllerButtons.Count)
					.Where((buttonIndex, i) => Utils.RNG.NextDouble() < probabilities[buttonIndex])
					.ToArray();

			for (int i = 0; i < times.Length; i++) {
				input.Pressed(this.Bot.ControllerButtons[times[i]]);
			}
			this.IsSet = true;
		}

		public FrameInput GenerateFrameInput(int frameNumber, IList<double> thresholds) {
			FrameInput frameInput = GetFrameInput(frameNumber);
			frameInput.Clear();
			int outputIndex = 0;
			List<NeatMappingRow> rows = this.Bot.neatMappings.GetEnabledMappings();
			int[] times = Enumerable.Range(0, count: this.Bot.ControllerButtons.Count)
					.Where((buttonIndex, i) => {
						if (this.Bot.neatMappings.HasControls) {
							if (rows.FirstOrDefault(r => r.GetOutput() != null && r.GetOutput()!.Equals(this.Bot.ControllerButtons[buttonIndex])) != null) {
								return Utils.RNG.NextDouble() < thresholds[outputIndex++];
							}
							return false;
						}
						else {
							return Utils.RNG.NextDouble() < thresholds[buttonIndex];
						}
					})
					.ToArray();
			for (int i = 0; i < times.Length; i++) {
				frameInput.Pressed(this.Bot.ControllerButtons[times[i]]);
			}
			this.IsSet = true;
			return frameInput;
		}

		public void SetResult() {
			this.result.Attempt = this.Bot.Runs;
			this.result.Generation = this.Bot.Generations;
			this.result.Maximize = this.Bot.MaximizeValue;
			this.result.TieBreak1 = this.Bot.TieBreaker1Value;
			this.result.TieBreak2 = this.Bot.TieBreaker2Value;
			this.result.TieBreak3 = this.Bot.TieBreaker3Value;
			this.result.ComparisonTypeMain = this.Bot.MainComparisonType;
			this.result.ComparisonTypeTie1 = this.Bot.Tie1ComparisonType;
			this.result.ComparisonTypeTie2 = this.Bot.Tie2ComparisonType;
			this.result.ComparisonTypeTie3 = this.Bot.Tie3ComparisonType;
			this.IsSet = true;
			this.Bot.ClearBestButton.Enabled = true;
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

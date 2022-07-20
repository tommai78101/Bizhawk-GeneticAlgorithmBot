using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneticAlgorithmBot {
	[ExternalTool("Genetic Algorithm Bot")]
	public sealed partial class GeneticAlgorithmBot : ToolFormBase, IExternalToolForm, IToolFormAutoConfig {
		#region Variables
		private string _windowTitle = "Genetic Algorithm Bot";

		private string _currentFilename = "";

		private bool _previousDisplayMessage = false;

		private bool _previousInvisibleEmulation = false;

		private string _lastOpenedRom = "";

		private MemoryDomain _currentDomain;

		private bool _bigEndian;

		private int _dataSize;

		private bool _replayMode;

		private bool _isBotting;

		private int _lastFrameAdvanced;

		private bool _oldCountingSetting;

		private bool _doNotUpdateValues;

		private Dictionary<string, double> _cachedControlProbabilities;

		private ILogEntryGenerator _logGenerator;

		private readonly int POPULATION_SIZE = 4;

		public int _startFrame;

		public int _targetFrame;

		public readonly BotAttempt _beginning;

		public PopulationManager populationManager;
		#endregion

		#region Variable Getters and Setters
		private long Runs { get; set; }

		private long Frames { get; set; }

		private long Generations { get; set; }

		private IMovie CurrentMovie => MovieSession.Movie;

		private Dictionary<string, double> ControlProbabilities => ControlProbabilityPanel!.Controls.OfType<BotControlsRow>().ToDictionary(tkey => tkey.ButtonName, tvalue => tvalue.Probability);

		private string CurrentFilename {
			get => _currentFilename;
			set {
				_currentFilename = value;
				_windowTitle = !string.IsNullOrWhiteSpace(_currentFilename)
					? $"{WindowTitleStatic} - {Path.GetFileNameWithoutExtension(_currentFilename)}"
					: WindowTitleStatic;
			}
		}

		protected override string WindowTitle => _windowTitle;

		protected override string WindowTitleStatic => _windowTitle;

		[RequiredService]
		public IEmulator Emulator { get; set; }

		[RequiredService]
		public IMemoryDomains MemoryDomains { get; set; }

		[ConfigPersist]
		public GeneticAlgorithmBotSettings Settings { get; set; }

		public int FrameLength {
			get => (int) FrameLengthNumeric.Value;
			set => FrameLengthNumeric.Value = value;
		}

		public bool IsActive => throw new NotImplementedException();

		public bool IsLoaded => throw new NotImplementedException();

		public bool ContainsFocus => throw new NotImplementedException();

		public ClickyVirtualPadController Controller => InputManager.ClickyVirtualPadController;

		public IList<string> ControllerButtons => Emulator.ControllerDefinition.BoolButtons;

		public int MaximizeAddress {
			get => MaximizeAddressBox.ToRawInt() ?? 0;
			set => MaximizeAddressBox.SetFromRawInt(value);
		}

		public int MaximizeValue {
			get {
				int? addr = MaximizeAddressBox.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker1Address {
			get => TieBreaker1Box.ToRawInt() ?? 0;
			set => TieBreaker1Box.SetFromRawInt(value);
		}

		public int TieBreaker1Value {
			get {
				int? addr = TieBreaker1Box.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker2Address {
			get => TieBreaker2Box.ToRawInt() ?? 0;
			set => TieBreaker2Box.SetFromRawInt(value);
		}

		public int TieBreaker2Value {
			get {
				int? addr = TieBreaker2Box.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker3Address {
			get => TieBreaker3Box.ToRawInt() ?? 0;
			set => TieBreaker3Box.SetFromRawInt(value);
		}

		public int TieBreaker3Value {
			get {
				int? addr = TieBreaker3Box.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public byte MainComparisonType {
			get => (byte) MainOperator.SelectedIndex;
			set => MainOperator.SelectedIndex = value < 6 ? value : 0;
		}

		public byte Tie1ComparisonType {
			get => (byte) Tiebreak1Operator.SelectedIndex;
			set => Tiebreak1Operator.SelectedIndex = value < 6 ? value : 0;
		}

		public byte Tie2ComparisonType {
			get => (byte) Tiebreak2Operator.SelectedIndex;
			set => Tiebreak2Operator.SelectedIndex = value < 6 ? value : 0;
		}

		public byte Tie3ComparisonType {
			get => (byte) Tiebreak3Operator.SelectedIndex;
			set => Tiebreak3Operator.SelectedIndex = value < 6 ? value : 0;
		}

		public int GetRamValue(int addr) {
			var val = _dataSize switch {
				1 => _currentDomain.PeekByte(addr),
				2 => _currentDomain.PeekUshort(addr, _bigEndian),
				4 => (int) _currentDomain.PeekUint(addr, _bigEndian),
				_ => _currentDomain.PeekByte(addr)
			};

			return val;
		}

		public string FromSlot {
			get => StartFromSlotBox.SelectedItem != null
				? StartFromSlotBox.SelectedItem.ToString()
				: "";

			set {
				var item = StartFromSlotBox.Items
					.OfType<object>()
					.FirstOrDefault(o => o.ToString() == value);

				StartFromSlotBox.SelectedItem = item;
			}
		}

		public string SelectedSlot {
			get {
				char num = StartFromSlotBox.SelectedItem
					.ToString()
					.Last();

				return $"QuickSave{num}";
			}
		}
		#endregion

		#region Static Variables
		public static Type Resources => typeof(ToolFormBase).Assembly.GetType("BizHawk.Client.EmuHawk.Properties.Resources");
		#endregion

		#region Class Methods
		public GeneticAlgorithmBot() {
			if (OSTailoredCode.IsUnixHost) {
				this.AutoSize = false;
				this.Margin = new(0, 0, 0, 8);
			}
			this.Settings = new GeneticAlgorithmBotSettings();
			this.populationManager = new PopulationManager(this, POPULATION_SIZE);
		}
		public static T GetResourceIcons<T>(string iconName) {
			FieldInfo fi = Resources.GetField(iconName, BindingFlags.NonPublic | BindingFlags.Static);
			return (T) fi.GetValue(Resources);
		}

		public void OnBotLoad(object sender, EventArgs eventArgs) {
			if (!CurrentMovie.IsActive() && !Tools.IsLoaded<TAStudio>()) {
				DialogController.ShowMessageBox("In order to proceed to use this tool, TAStudio is required to be opened.");
				this.Close();
				DialogResult = DialogResult.Cancel;
				return;
			}

			if (Config!.OpposingDirPolicy is not OpposingDirPolicy.Allow) {
				DialogController.ShowMessageBox("In order to proceed to use this tool, please check if the U+D/L+R controller binds policy is set to 'Allow'.");
				this.Close();
				DialogResult = DialogResult.Cancel;
				return;
			}

			if (OSTailoredCode.IsUnixHost) {
				ClientSize = new(707, 587);
			}

			_previousInvisibleEmulation = InvisibleEmulationCheckBox.Checked = Settings.InvisibleEmulation;
			_previousDisplayMessage = Config.DisplayMessages;
		}

		public void UpdateValues(ToolFormUpdateType type) {
			throw new NotImplementedException();
		}

		public void FinishReplay() {
			MainForm.PauseEmulator();
			_startFrame = 0;
			_replayMode = false;
			UpdateBotStatusIcon();
			MessageLabel.Text = "Replay stopped";
		}

		public void Restart() {
			if (_currentDomain == null
				|| MemoryDomains.Contains(_currentDomain)) {
				_currentDomain = MemoryDomains.MainMemory;
				_bigEndian = _currentDomain.EndianType == MemoryDomain.Endian.Big;
				_dataSize = 1;
			}

			if (_isBotting) {
				StopBot();
			}
			else if (_replayMode) {
				FinishReplay();
			}

			if (_lastOpenedRom != MainForm.CurrentlyOpenRom) {
				_lastOpenedRom = MainForm.CurrentlyOpenRom;
				SetupControlsAndProperties();
			}
		}

		public void StartBot() {
			var message = CanStart();
			if (!string.IsNullOrWhiteSpace(message)) {
				DialogController.ShowMessageBox(message);
				return;
			}

			_isBotting = true;
			ControlsBox.Enabled = false;
			StartFromSlotBox.Enabled = false;
			RunBtn.Visible = false;
			StopBtn.Visible = true;
			GoalGroupBox.Enabled = false;
			this.populationManager.GetCurrent().Reset(Runs);

			if (MovieSession.Movie.IsRecording()) {
				_oldCountingSetting = MovieSession.Movie.IsCountingRerecords;
				MovieSession.Movie.IsCountingRerecords = false;
			}

			_logGenerator = MovieSession.Movie.LogGeneratorInstance(InputManager.ClickyVirtualPadController);
			_cachedControlProbabilities = ControlProbabilities;

			_doNotUpdateValues = true;
			PressButtons(true);
			MainForm.LoadQuickSave(SelectedSlot, true); // Triggers an UpdateValues call
			_lastFrameAdvanced = Emulator.Frame;
			_doNotUpdateValues = false;
			_startFrame = Emulator.Frame;

			_targetFrame = Emulator.Frame + (int) FrameLengthNumeric.Value;

			_previousDisplayMessage = Config.DisplayMessages;
			Config.DisplayMessages = false;

			MainForm.UnpauseEmulator();
			if (Settings.TurboWhenBotting) {
				SetMaxSpeed();
			}

			if (InvisibleEmulationCheckBox.Checked) {
				_previousInvisibleEmulation = MainForm.InvisibleEmulation;
				MainForm.InvisibleEmulation = true;
			}

			UpdateBotStatusIcon();
			MessageLabel.Text = "Running...";
		}

		public void StopBot() {
			RunBtn.Visible = true;
			StopBtn.Visible = false;
			_isBotting = false;
			_targetFrame = 0;
			ControlsBox.Enabled = true;
			StartFromSlotBox.Enabled = true;
			GoalGroupBox.Enabled = true;

			if (MovieSession.Movie.IsRecording()) {
				MovieSession.Movie.IsCountingRerecords = _oldCountingSetting;
			}

			Config!.DisplayMessages = _previousDisplayMessage;
			MainForm.InvisibleEmulation = _previousInvisibleEmulation;
			MainForm.PauseEmulator();
			SetNormalSpeed();
			UpdateBotStatusIcon();
			MessageLabel.Text = "Bot stopped";
		}

		public string CanStart() {
			if (!ControlProbabilities.Any(cp => cp.Value > 0)) {
				return "At least one control must have a probability greater than 0.";
			}

			if (!MaximizeAddressBox.ToRawInt().HasValue) {
				return "A main value address is required.";
			}

			if (FrameLengthNumeric.Value == 0) {
				return "A frame count greater than 0 is required";
			}

			return null;
		}

		public bool AskSaveChanges() {
			throw new NotImplementedException();
		}

		public bool Focus() {
			throw new NotImplementedException();
		}

		public void Show() {
			throw new NotImplementedException();
		}

		public void Close() {
			throw new NotImplementedException();
		}

		public void PressButtons(bool clear_log) {
			if (this.populationManager.GetBest() != null) {
				FrameInput inputs = this.populationManager.GetBest().GetFrameInput(Emulator.Frame);
				foreach (var button in inputs.Buttons) {
					InputManager.ClickyVirtualPadController.SetBool(button, false);
				}
				InputManager.SyncControls(Emulator, MovieSession, Config);

				if (clear_log)
					this.populationManager.ClearBestRecordingLog();
				this.populationManager.SetBestRecordingLog(_logGenerator.GenerateLogEntry());
			}
		}

		public void UpdateBestAttempt() {
			if (!this.populationManager.GetBest().GetAttempt().isReset) {
				ClearBestButton.Enabled = true;
				btnCopyBestInput.Enabled = true;
				BestAttemptNumberLabel.Text = this.populationManager.GetBest().GetAttempt().Attempt.ToString();
				BestMaximizeBox.Text = this.populationManager.GetBest().GetAttempt().Maximize.ToString();
				BestTieBreak1Box.Text = this.populationManager.GetBest().GetAttempt().TieBreak1.ToString();
				BestTieBreak2Box.Text = this.populationManager.GetBest().GetAttempt().TieBreak2.ToString();
				BestTieBreak3Box.Text = this.populationManager.GetBest().GetAttempt().TieBreak3.ToString();

				Console.WriteLine($"Logging attempt:  Log size: {this.populationManager.GetBest().GetAttempt().Log.Count}");
				var sb = new StringBuilder();
				foreach (var logEntry in this.populationManager.GetBest().GetAttempt().Log) {
					sb.AppendLine(logEntry);
				}
				BestAttemptLogLabel.Text = sb.ToString();
				PlayBestButton.Enabled = true;
				btnCopyBestInput.Enabled = true;
			}
			else {
				ClearBestButton.Enabled = false;
				BestAttemptNumberLabel.Text = "";
				BestMaximizeBox.Text = "";
				BestTieBreak1Box.Text = "";
				BestTieBreak2Box.Text = "";
				BestTieBreak3Box.Text = "";
				BestAttemptLogLabel.Text = "";
				PlayBestButton.Enabled = false;
				btnCopyBestInput.Enabled = false;
			}
		}

		public float[] GetCachedInputProbabilities() {
			float[] target = new float[Emulator.ControllerDefinition.BoolButtons.Count];
			for (int i = 0; i < Emulator.ControllerDefinition.BoolButtons.Count; i++) {
				string button = Emulator.ControllerDefinition.BoolButtons[i];
				target[i] = (float) ControlProbabilities[button] / 100.0f;
			}
			return target;
		}
		#endregion

		#region Private methods
		private void SetupControlsAndProperties() {
			MaximizeAddressBox.SetHexProperties(_currentDomain.Size);
			TieBreaker1Box.SetHexProperties(_currentDomain.Size);
			TieBreaker2Box.SetHexProperties(_currentDomain.Size);
			TieBreaker3Box.SetHexProperties(_currentDomain.Size);

			StartFromSlotBox.SelectedIndex = 0;

			const int startY = 0;
			const int lineHeight = 30;
			const int marginLeft = 15;
			int accumulatedY = 0;
			int count = 0;

			ControlProbabilityPanel.SuspendLayout();
			{
				ControlProbabilityPanel.Controls.Clear();
				foreach (var button in Emulator.ControllerDefinition.BoolButtons) {
					var control = new BotControlsRow {
						ButtonName = button,
						Probability = 0.0,
						Location = new Point(marginLeft, startY + accumulatedY),
						TabIndex = count + 1,
						ProbabilityChangedCallback = AssessRunButtonStatus
					};
					control.Scale(UIHelper.AutoScaleFactor);

					ControlProbabilityPanel.Controls.Add(control);
					accumulatedY += lineHeight;
					count++;
				}
			}
			ControlProbabilityPanel.ResumeLayout();

			if (Settings.RecentBotFiles.AutoLoad) {
				LoadFileFromRecent(Settings.RecentBotFiles.MostRecent);
			}
			UpdateBotStatusIcon();
		}

		private void AssessRunButtonStatus() {
			RunBtn.Enabled =
				FrameLength > 0
				&& !string.IsNullOrWhiteSpace(MaximizeAddressBox.Text)
				&& ControlProbabilities.Any(kvp => kvp.Value > 0);
		}

		private void LoadFileFromRecent(string path) {
			var result = LoadBotFile(path);
			if (!result) {
				Settings.RecentBotFiles.HandleLoadError(MainForm, path);
			}
		}

		private void UpdateBotStatusIcon() {
			if (_replayMode) {
				BotStatusButton.Image = GetResourceIcons<Image>("Play");
				BotStatusButton.ToolTipText = "Replaying the best result";
			}
			else if (_isBotting) {
				BotStatusButton.Image = GetResourceIcons<Image>("Record");
				BotStatusButton.ToolTipText = "Botting in progress";
			}
			else {
				BotStatusButton.Image = GetResourceIcons<Image>("Pause");
				BotStatusButton.ToolTipText = "Bot is currently not running";
			}
		}

		private bool LoadBotFile(string path) {
			var file = new FileInfo(path);
			if (!file.Exists) {
				return false;
			}

			string json = File.ReadAllText(path);
			BotData botData = (BotData) ConfigService.LoadWithType(json);

			this.populationManager.GetBest().GetAttempt().Attempt = botData.Best.Attempt;
			this.populationManager.GetBest().GetAttempt().Maximize = botData.Best.Maximize;
			this.populationManager.GetBest().GetAttempt().TieBreak1 = botData.Best.TieBreak1;
			this.populationManager.GetBest().GetAttempt().TieBreak2 = botData.Best.TieBreak2;
			this.populationManager.GetBest().GetAttempt().TieBreak3 = botData.Best.TieBreak3;

			// no references to ComparisonType parameters

			this.populationManager.GetBest().GetAttempt().Log.Clear();

			for (int i = 0; i < botData.Best.Log.Count; i++) {
				this.populationManager.GetBest().GetAttempt().Log.Add(botData.Best.Log[i]);
			}

			this.populationManager.GetBest().GetAttempt().isReset = false;

			var probabilityControls = ControlProbabilityPanel.Controls
					.OfType<BotControlsRow>()
					.ToList();

			foreach (var (button, p) in botData.ControlProbabilities) {
				var control = probabilityControls.Single(c => c.ButtonName == button);
				control.Probability = p;
			}

			MaximizeAddress = botData.Maximize;
			TieBreaker1Address = botData.TieBreaker1;
			TieBreaker2Address = botData.TieBreaker2;
			TieBreaker3Address = botData.TieBreaker3;
			try {
				MainComparisonType = botData.ComparisonTypeMain;
				Tie1ComparisonType = botData.ComparisonTypeTie1;
				Tie2ComparisonType = botData.ComparisonTypeTie2;
				Tie3ComparisonType = botData.ComparisonTypeTie3;

				MainBestRadio.Checked = botData.MainCompareToBest;
				TieBreak1BestRadio.Checked = botData.TieBreaker1CompareToBest;
				TieBreak2BestRadio.Checked = botData.TieBreaker2CompareToBest;
				TieBreak3BestRadio.Checked = botData.TieBreaker3CompareToBest;
				MainValueRadio.Checked = !botData.MainCompareToBest;
				TieBreak1ValueRadio.Checked = !botData.TieBreaker1CompareToBest;
				TieBreak2ValueRadio.Checked = !botData.TieBreaker2CompareToBest;
				TieBreak3ValueRadio.Checked = !botData.TieBreaker3CompareToBest;

				MainValueNumeric.Value = botData.MainCompareToValue;
				TieBreak1Numeric.Value = botData.TieBreaker1CompareToValue;
				TieBreak2Numeric.Value = botData.TieBreaker2CompareToValue;
				TieBreak3Numeric.Value = botData.TieBreaker3CompareToValue;
			}
			catch {
				MainComparisonType = 0;
				Tie1ComparisonType = 0;
				Tie2ComparisonType = 0;
				Tie3ComparisonType = 0;

				MainBestRadio.Checked = true;
				TieBreak1BestRadio.Checked = true;
				TieBreak2BestRadio.Checked = true;
				TieBreak3BestRadio.Checked = true;
				MainBestRadio.Checked = false;
				TieBreak1BestRadio.Checked = false;
				TieBreak2BestRadio.Checked = false;
				TieBreak3BestRadio.Checked = false;

				MainValueNumeric.Value = 0;
				TieBreak1Numeric.Value = 0;
				TieBreak2Numeric.Value = 0;
				TieBreak3Numeric.Value = 0;
			}
			FrameLength = botData.FrameLength;
			FromSlot = botData.FromSlot;
			Runs = botData.Runs;
			Frames = botData.Frames;
			Generations = botData.Generations;

#pragma warning disable CS8601 // Possible null reference assignment.
			_currentDomain = !string.IsNullOrWhiteSpace(botData.MemoryDomain) ? MemoryDomains[botData.MemoryDomain] : MemoryDomains.MainMemory;
#pragma warning restore CS8601 // Possible null reference assignment.

			_bigEndian = botData.BigEndian;
			_dataSize = botData.DataSize > 0 ? botData.DataSize : 1;

			UpdateBestAttempt();

			if (!this.populationManager.GetBest().GetAttempt().isReset) {
				PlayBestButton.Enabled = true;
			}

			CurrentFilename = path;
			Settings.RecentBotFiles.Add(CurrentFilename);
			MessageLabel.Text = $"{Path.GetFileNameWithoutExtension(path)} loaded";

			AssessRunButtonStatus();
			return true;
		}

		private void SetMaxSpeed() {
			MainForm.Unthrottle();
		}

		private void SetNormalSpeed() {
			MainForm.Throttle();
		}
		#endregion
	}

	public static class Rand {
		public static Random RNG { get; } = new Random((int) DateTime.Now.Ticks);
	}

	public class PopulationManager {
		private InputRecording _bestRecording;
		public InputRecording[] population;
		public int currentIndex = 0;
		public long Generation { get; set; }
		private GeneticAlgorithmBot bot;

		public PopulationManager(GeneticAlgorithmBot owner, int populationSize) {
			this._bestRecording = new InputRecording(owner);
			this.population = new InputRecording[populationSize];
			for (int i = 0; i < populationSize; i++) {
				this.population[i] = new InputRecording(owner);
			}
		}

		public InputRecording GetCurrent() {
			return this.population[this.currentIndex];
		}

		public void ClearBestRecordingLog() {
			this._bestRecording.GetAttempt().Log.Clear();
		}

		public void SetBestRecordingLog(string log) {
			this._bestRecording.GetAttempt().Log.Add(log);
		}

		public InputRecording GetBest() {
			return this._bestRecording;
		}

		// Returns true if the current index wraps back to zero.
		public bool NextRecording() {
			this.currentIndex = ++this.currentIndex % this.population.Length;
			return this.currentIndex == 0;
		}
	}

	public class InputRecording {
		// If result "isReset" flag is set, the input recording hasn't started its attempt. Otherwise, any attempt counts as some result (success, skipped, fail).
		public BotAttempt result;
		public FrameInput[] recording;
		public double fitness { get; set; }
		public int StartFrameNumber { get; set; }
		private GeneticAlgorithmBot bot;

		public BotAttempt SourceAttempt => bot._beginning;

		public InputRecording(GeneticAlgorithmBot owner) {
			this.bot = owner;
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
			int index = frameNumber - this.StartFrameNumber;
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
		}

		public void RandomizeInputRecording() {
			float[] probabilities = bot.GetCachedInputProbabilities();
			IList<int[]> a = Enumerable.Range(0, this.bot.FrameLength).Select(run => {
				int[] times = Enumerable.Range(0, this.bot.ControllerButtons.Count)
					.Where((buttonIndex, i) => Rand.RNG.NextDouble() < probabilities[buttonIndex])
					.ToArray();
				return times;
			}).ToArray();
			int[][] values = a.ToArray();

			int length = values.Length;
			if (values.Length != this.bot.FrameLength) {
				length = this.bot.FrameLength;
			}

			for (int i = 0; i < length; i++) {
				FrameInput input = this.GetFrameInput(this.StartFrameNumber + i);
				for (int j = 0; j < values[i].Length; j++) {
					input.Pressed(this.bot.ControllerButtons[values[i][j]]);
				}
			}
		}

		public void RandomizeFrameInput() {
			int frameNumber = Rand.RNG.Next(bot._startFrame, bot._startFrame + this.recording.Length);
			int index = frameNumber - bot._startFrame;
			FrameInput input = this.GetFrameInput(frameNumber);
			input.Clear();

			float[] probabilities = bot.GetCachedInputProbabilities();
			int[] times = Enumerable.Range(0, count: this.bot.ControllerButtons.Count)
					.Where((buttonIndex, i) => Rand.RNG.NextDouble() < probabilities[buttonIndex])
					.ToArray();

			for (int i = 0; i < times.Length; i++) {
				input.Pressed(this.bot.ControllerButtons[times[i]]);
			}
		}

		public double Evaluate() {
			if (result == null) {
				this.fitness = 0.0;
			}
			return fitness;
		}

		public void Reset(long attemptNumber) {
			this.result.Attempt = attemptNumber;
			this.result.Maximize = 0;
			this.result.TieBreak1 = 0;
			this.result.TieBreak2 = 0;
			this.result.TieBreak3 = 0;
			this.result.Log.Clear();
			this.result.isReset = true;
		}
	}

	public class FrameInput {
		public HashSet<string> Buttons { get; set; }
		public int FrameNumber { get; set; }

		public FrameInput(int frameNumber) {
			this.Buttons = new HashSet<string>();
			FrameNumber = frameNumber;
		}

		public void Clear() {
			this.Buttons.Clear();
		}

		public void Pressed(string button) {
			this.Buttons.Add(button);
		}

		public void Released(string button) {
			this.Buttons.Remove(button);
		}

		public bool IsPressed(string button) {
			return this.Buttons.Contains(button);
		}
	}

	public class BotAttempt {
		public long Attempt { get; set; }
		public long Generation { get; set; }
		public int Fitness { get; set; }
		public int Maximize { get; set; }
		public int TieBreak1 { get; set; }
		public int TieBreak2 { get; set; }
		public int TieBreak3 { get; set; }
		public byte ComparisonTypeMain { get; set; }
		public byte ComparisonTypeTie1 { get; set; }
		public byte ComparisonTypeTie2 { get; set; }
		public byte ComparisonTypeTie3 { get; set; }
		public List<string> Log { get; } = new List<string>();
		public bool isReset { get; set; } = true;
	}

	public class GeneticAlgorithmBotSettings {
		public RecentFiles RecentBotFiles { get; set; } = new RecentFiles();
		public bool TurboWhenBotting { get; set; } = true;
		public bool InvisibleEmulation { get; set; }
	}

	public class BotData {
		public BotAttempt Best { get; set; }
		public Dictionary<string, double> ControlProbabilities { get; set; }
		public int Maximize { get; set; }
		public int TieBreaker1 { get; set; }
		public int TieBreaker2 { get; set; }
		public int TieBreaker3 { get; set; }
		public byte ComparisonTypeMain { get; set; }
		public byte ComparisonTypeTie1 { get; set; }
		public byte ComparisonTypeTie2 { get; set; }
		public byte ComparisonTypeTie3 { get; set; }
		public bool MainCompareToBest { get; set; } = true;
		public bool TieBreaker1CompareToBest { get; set; } = true;
		public bool TieBreaker2CompareToBest { get; set; } = true;
		public bool TieBreaker3CompareToBest { get; set; } = true;
		public int MainCompareToValue { get; set; }
		public int TieBreaker1CompareToValue { get; set; }
		public int TieBreaker2CompareToValue { get; set; }
		public int TieBreaker3CompareToValue { get; set; }
		public int FrameLength { get; set; }
		public string FromSlot { get; set; }
		public long Runs { get; set; }
		public long Frames { get; set; }
		public long Generations { get; set; }
		public string MemoryDomain { get; set; }
		public bool BigEndian { get; set; }
		public int DataSize { get; set; }
	}
}

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;
using System.Collections;
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
		#region Static Variables
		public static Type Resources => typeof(ToolFormBase).Assembly.GetType("BizHawk.Client.EmuHawk.Properties.Resources");
		#endregion

		#region Variables
		private string _windowTitle = "Genetic Algorithm Bot";

		private string _currentFilename = "";

		private bool _previousDisplayMessage = false;

		private bool _previousInvisibleEmulation = false;

		private bool _debug_mode_skipRestart = false;

		private string _lastOpenedRom = "";

		private MemoryDomain _currentDomain = default!;

		private bool _bigEndian;

		private int _dataSize;

		private bool _replayMode;

		private bool _isBotting;

		private int _lastFrameAdvanced;

		private long _frames;

		private long _runs;

		private long _generations;

		private bool _oldCountingSetting;

		private bool _doNotUpdateValues;

		private ILogEntryGenerator _logGenerator = default!;

		/// <summary>
		/// Comparison bot attempt is a bot attempt with current best bot attempt values from Population Manager, containing values where the "best" radio buttons are selected
		/// </summary>
		public BotAttempt comparisonAttempt;

		public int _startFrame;

		public int _targetFrame;

		public GeneticAlgorithm populationManager;
		#endregion

		#region Variable Getters and Setters
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

		public long Runs {
			get => _runs;
			set {
				_runs = value;
				AttemptsLabel.Text = _runs.ToString();
			}
		}

		public long Frames {
			get => _frames;
			set {
				_frames = value;
				FramesLabel.Text = _frames.ToString();
			}
		}

		public long Generations {
			get => _generations;
			set {
				_generations = value;
				GenerationsLabel.Text = _generations.ToString();
			}
		}

		[RequiredService]
		public IEmulator Emulator { get; set; } = default!;

		[RequiredService]
		public IMemoryDomains MemoryDomains { get; set; } = default!;

		[ConfigPersist]
		public GeneticAlgorithmBotSettings Settings { get; set; }

		public int FrameLength {
			get => (int) FrameLengthNumeric.Value;
			set => FrameLengthNumeric.Value = value;
		}

		public int PopulationSize {
			get => (int) PopulationSizeNumeric.Value;
			set => PopulationSizeNumeric.Value = value;
		}

		public ClickyVirtualPadController Controller => InputManager.ClickyVirtualPadController;

		public IList<string> ControllerButtons => Emulator.ControllerDefinition.BoolButtons;

		public int MaximizeAddress {
			get => MaximizeAddressBox.ToRawInt() ?? 0;
			set => MaximizeAddressBox.SetFromRawInt(value);
		}

		public uint MaximizeValue {
			get {
				int? addr = MaximizeAddressBox.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker1Address {
			get => TieBreaker1Box.ToRawInt() ?? 0;
			set => TieBreaker1Box.SetFromRawInt(value);
		}

		public uint TieBreaker1Value {
			get {
				int? addr = TieBreaker1Box.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker2Address {
			get => TieBreaker2Box.ToRawInt() ?? 0;
			set => TieBreaker2Box.SetFromRawInt(value);
		}

		public uint TieBreaker2Value {
			get {
				int? addr = TieBreaker2Box.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker3Address {
			get => TieBreaker3Box.ToRawInt() ?? 0;
			set => TieBreaker3Box.SetFromRawInt(value);
		}

		public uint TieBreaker3Value {
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

		public uint GetRamValue(int addr) {
			uint val = _dataSize switch {
				1 => _currentDomain.PeekByte(addr),
				2 => _currentDomain.PeekUshort(addr, _bigEndian),
				4 => _currentDomain.PeekUint(addr, _bigEndian),
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

		#region Class Methods
		public GeneticAlgorithmBot() {
			InitializeComponent();
			NewMenuItem.Image = GetResourceIcons<Image>("NewFile");
			OpenMenuItem.Image = GetResourceIcons<Image>("OpenFile");
			SaveMenuItem.Image = GetResourceIcons<Image>("SaveAs");
			RecentSubMenu.Image = GetResourceIcons<Image>("Recent");
			RunBtn.Image = GetResourceIcons<Image>("Play");
			BotStatusButton.Image = GetResourceIcons<Image>("Placeholder");
			btnCopyBestInput.Image = GetResourceIcons<Image>("Duplicate");
			PlayBestButton.Image = GetResourceIcons<Image>("Play");
			ClearBestButton.Image = GetResourceIcons<Image>("Close");
			StopBtn.Image = GetResourceIcons<Image>("Stop");

			if (OSTailoredCode.IsUnixHost) {
				this.AutoSize = false;
				this.Margin = new(0, 0, 0, 8);
			}
			this.Settings = new GeneticAlgorithmBotSettings();
			this.populationManager = new GeneticAlgorithm(this);
			this.MainOperator.SelectedItem = ">=";
			this.comparisonAttempt = new BotAttempt();
		}

		public static T GetResourceIcons<T>(string iconName) {
			FieldInfo fi = Resources.GetField(iconName, BindingFlags.NonPublic | BindingFlags.Static);
			return (T) fi.GetValue(Resources);
		}

		public void FinishReplay() {
			MainForm.PauseEmulator();
			_startFrame = 0;
			_replayMode = false;
			UpdateBotStatusIcon();
			MessageLabel.Text = "Replay stopped";
		}

		public override void Restart() {
			if (_debug_mode_skipRestart) {
				return;
			}

			if (_currentDomain == null || MemoryDomains.Contains(_currentDomain)) {
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
				DialogController.ShowMessageBox(message!);
				return;
			}

			_isBotting = true;
			ControlsBox.Enabled = false;
			StartFromSlotBox.Enabled = false;
			RunBtn.Visible = false;
			StopBtn.Visible = true;
			GoalGroupBox.Enabled = false;

			if (MovieSession.Movie.IsRecording()) {
				_oldCountingSetting = MovieSession.Movie.IsCountingRerecords;
				MovieSession.Movie.IsCountingRerecords = false;
			}

			_logGenerator = MovieSession.Movie.LogGeneratorInstance(InputManager.ClickyVirtualPadController);

			_doNotUpdateValues = true;
			PressButtons(true);
			MainForm.LoadQuickSave(SelectedSlot, true); // Triggers an UpdateValues call
			_lastFrameAdvanced = Emulator.Frame;
			_doNotUpdateValues = false;
			_startFrame = Emulator.Frame;
			_targetFrame = Emulator.Frame + (int) FrameLengthNumeric.Value;

			_previousDisplayMessage = Config!.DisplayMessages;
			Config!.DisplayMessages = false;

			if (!this.populationManager.IsInitialized) {
				this.populationManager.Initialize();
			}

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

		public string? CanStart() {
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

		public void PressButtons(bool clear_log) {
			if (this.populationManager.GetCurrent() != null) {
				FrameInput inputs = this.populationManager.GetCurrent().GetFrameInput(Emulator.Frame);
				foreach (var button in inputs.Buttons) {
					InputManager.ClickyVirtualPadController.SetBool(button, false);
				}
				InputManager.SyncControls(Emulator, MovieSession, Config);

				if (clear_log)
					this.populationManager.ClearCurrentRecordingLog();
				this.populationManager.SetCurrentRecordingLog(_logGenerator.GenerateLogEntry());
			}
		}

		public void UpdateBestAttemptUI() {
			ClearBestButton.Enabled = true;
			if (this.populationManager.GetBest().IsSet) {
				btnCopyBestInput.Enabled = true;
				BotAttempt best = this.populationManager.GetBest().GetAttempt();
				BestAttemptNumberLabel.Text = best.Attempt.ToString();
				BestGenerationNumberLabel.Text = best.Generation.ToString();
				BestMaximizeBox.Text = best.Maximize.ToString();
				BestTieBreak1Box.Text = best.TieBreak1.ToString();
				BestTieBreak2Box.Text = best.TieBreak2.ToString();
				BestTieBreak3Box.Text = best.TieBreak3.ToString();

				var sb = new StringBuilder();
				foreach (var logEntry in best.Log) {
					sb.AppendLine(logEntry);
				}
				BestAttemptLogLabel.Text = sb.ToString();
				PlayBestButton.Enabled = true;
				btnCopyBestInput.Enabled = true;
			}
			else {
				BestAttemptNumberLabel.Text = "";
				BestGenerationNumberLabel.Text = "";
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

		// Controls need to be set and synced after emulation, so that everything works out properly at the start of the next frame
		// Consequently, when loading a state, input needs to be set before the load, to ensure everything works out in the correct order
		protected override void UpdateAfter() => Update(fast: false);
		protected override void FastUpdateAfter() => Update(fast: true);

		public void Update(bool fast) {
			if (_doNotUpdateValues) {
				return;
			}

			if (!HasFrameAdvanced()) {
				return;
			}

			BotAttempt best = this.populationManager.GetBest().GetAttempt();
			if (_replayMode) {
				int index = Emulator.Frame - _startFrame;
				if (index < best.Log.Count) {
					var logEntry = best.Log[index];
					var controller = MovieSession.GenerateMovieController();
					controller.SetFromMnemonic(logEntry);
					foreach (var button in controller.Definition.BoolButtons) {
						// TODO: make an input adapter specifically for the bot?
						InputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
					}

					InputManager.SyncControls(Emulator, MovieSession, Config);

					_lastFrameAdvanced = Emulator.Frame;
				}
				else {
					FinishReplay();
				}
			}
			else if (_isBotting) {
				if (Emulator.Frame >= _targetFrame) {
					Runs++;
					Frames += FrameLength;

					this.populationManager.GetCurrent().SetResult();
					if (this.populationManager.NextRecording()) {
						this.populationManager.Reproduce();
						Generations = this.populationManager.EvaluateGeneration();
						UpdateBestAttemptUI();
						UpdateComparisonBotAttempt();
					}

					_doNotUpdateValues = true;
					PressButtons(true);
					MainForm.LoadQuickSave(SelectedSlot, true);
					_lastFrameAdvanced = Emulator.Frame;
					_doNotUpdateValues = false;
					return;
				}

				// Before this would have 2 additional hits before the frame even advanced, making the amount of inputs greater than the number of frames to test.
				if (this.populationManager.GetCurrent().GetAttempt().Log.Count < FrameLength) //aka do not Add more inputs than there are Frames to test
				{
					PressButtons(false);
					_lastFrameAdvanced = Emulator.Frame;
				}
			}
		}

		public bool HasFrameAdvanced() {
			// If the emulator frame is different from the last time it tried calling
			// the function then we can continue, otherwise we need to stop.
			return _lastFrameAdvanced != Emulator.Frame;
		}
		#endregion

		#region Private methods
		/// <summary>
		/// Updates comparison bot attempt with current best bot attempt values for values where the "best" radio button is selected
		/// </summary>
		private void UpdateComparisonBotAttempt() {
			BotAttempt best = this.populationManager.GetBest().GetAttempt();
			if (best.isReset) {
				if (MainBestRadio.Checked) {
					this.comparisonAttempt.Maximize = 0;
				}

				if (TieBreak1BestRadio.Checked) {
					this.comparisonAttempt.TieBreak1 = 0;
				}

				if (TieBreak2BestRadio.Checked) {
					this.comparisonAttempt.TieBreak2 = 0;
				}

				if (TieBreak3BestRadio.Checked) {
					this.comparisonAttempt.TieBreak3 = 0;
				}
			}
			else {
				if (MainBestRadio.Checked && best.Maximize != comparisonAttempt.Maximize) {
					this.comparisonAttempt.Maximize = best.Maximize;
				}

				if (TieBreak1BestRadio.Checked && best.TieBreak1 != comparisonAttempt.TieBreak1) {
					this.comparisonAttempt.TieBreak1 = best.TieBreak1;
				}

				if (TieBreak2BestRadio.Checked && best.TieBreak2 != comparisonAttempt.TieBreak2) {
					this.comparisonAttempt.TieBreak2 = best.TieBreak2;
				}

				if (TieBreak3BestRadio.Checked && best.TieBreak3 != comparisonAttempt.TieBreak3) {
					this.comparisonAttempt.TieBreak3 = best.TieBreak3;
				}
			}
		}

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
						ProbabilityChangedCallback = this.AssessRunButtonStatus
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
			BotData botData = default!;
			try {
				// Attempts to load GeneticAlgorithmBot .BOT file save data.
				botData = (BotData) ConfigService.LoadWithType(json);
			} catch (InvalidCastException e) {
				// If exception is thrown, attempt to load BasicBot .BOT file save data instead.
				botData = Utils.BotDataReflectionCopy(ConfigService.LoadWithType(json));
			}
			this.populationManager.GetBest().GetAttempt().Attempt = botData.Best?.Attempt ?? 0;
			this.populationManager.GetBest().GetAttempt().Maximize = botData.Best?.Maximize ?? 0;
			this.populationManager.GetBest().GetAttempt().TieBreak1 = botData.Best?.TieBreak1 ?? 0;
			this.populationManager.GetBest().GetAttempt().TieBreak2 = botData.Best?.TieBreak2 ?? 0;
			this.populationManager.GetBest().GetAttempt().TieBreak3 = botData.Best?.TieBreak3 ?? 0;

			// no references to ComparisonType parameters

			this.populationManager.GetBest().GetAttempt().Log.Clear();

			for (int i = 0; i < botData.Best?.Log?.Count; i++) {
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

			_currentDomain = !string.IsNullOrWhiteSpace(botData.MemoryDomain) ? MemoryDomains[botData.MemoryDomain]! : MemoryDomains.MainMemory;

			_bigEndian = botData.BigEndian;
			_dataSize = botData.DataSize > 0 ? botData.DataSize : 1;

			UpdateBestAttemptUI();

			if (this.populationManager.GetBest().IsSet) {
				PlayBestButton.Enabled = true;
			}

			CurrentFilename = path;
			Settings.RecentBotFiles.Add(CurrentFilename);
			MessageLabel.Text = $"{Path.GetFileNameWithoutExtension(path)} loaded";

			AssessRunButtonStatus();
			return true;
		}

		private void SaveBotFile(string path) {
			BotData data = new BotData {
				Best = this.populationManager.GetBest().GetAttempt(),
				ControlProbabilities = ControlProbabilities,
				Maximize = MaximizeAddress,
				TieBreaker1 = TieBreaker1Address,
				TieBreaker2 = TieBreaker2Address,
				TieBreaker3 = TieBreaker3Address,
				ComparisonTypeMain = MainComparisonType,
				ComparisonTypeTie1 = Tie1ComparisonType,
				ComparisonTypeTie2 = Tie2ComparisonType,
				ComparisonTypeTie3 = Tie3ComparisonType,
				MainCompareToBest = MainBestRadio.Checked,
				TieBreaker1CompareToBest = TieBreak1BestRadio.Checked,
				TieBreaker2CompareToBest = TieBreak2BestRadio.Checked,
				TieBreaker3CompareToBest = TieBreak3BestRadio.Checked,
				MainCompareToValue = (int) MainValueNumeric.Value,
				TieBreaker1CompareToValue = (int) TieBreak1Numeric.Value,
				TieBreaker2CompareToValue = (int) TieBreak2Numeric.Value,
				TieBreaker3CompareToValue = (int) TieBreak3Numeric.Value,
				FromSlot = FromSlot,
				FrameLength = FrameLength,
				Runs = Runs,
				Frames = Frames,
				MemoryDomain = _currentDomain.Name,
				BigEndian = _bigEndian,
				DataSize = _dataSize
			};

			string json = ConfigService.SaveWithType(data);

			File.WriteAllText(path, json);
			CurrentFilename = path;
			Settings.RecentBotFiles.Add(CurrentFilename);
			MessageLabel.Text = $"{Path.GetFileName(CurrentFilename)} saved";
		}

		private void SetMaxSpeed() {
			MainForm.Unthrottle();
		}

		private void SetNormalSpeed() {
			MainForm.Throttle();
		}

		private void SetMemoryDomain(string name) {
			_currentDomain = MemoryDomains[name]!;
			_bigEndian = _currentDomain!.EndianType == MemoryDomain.Endian.Big;

			MaximizeAddressBox.SetHexProperties(_currentDomain.Size);
			TieBreaker1Box.SetHexProperties(_currentDomain.Size);
			TieBreaker2Box.SetHexProperties(_currentDomain.Size);
			TieBreaker3Box.SetHexProperties(_currentDomain.Size);
		}
		#endregion

		#region UI Event Handlers
		public void OnBotLoad(object sender, EventArgs eventArgs) {
			if (!CurrentMovie.IsActive() && !Tools.IsLoaded<TAStudio>()) {
				DialogController.ShowMessageBox("In order to proceed to use this tool, TAStudio is required to be opened.");
				this.Close();
				DialogResult = DialogResult.Cancel;
				return;
			}

			Type configType = Config!.GetType();
			// For BizHawk versions > 2.8. (git commit af2d8da36e50a9004d6ecfd456381956b9245d66)
			if (configType.GetProperty("OpposingDirPolicy") != null) {
				if (!configType.GetProperty("OpposingDirPolicy").GetValue(Config!).ToString().Contains("Allow")) {
					DialogController.ShowMessageBox("In order to proceed to use this tool, please check if the U+D/L+R controller binds policy is set to 'Allow'.");
					this.Close();
					DialogResult = DialogResult.Cancel;
					return;
				}
			}
			// To be made compatible with BizHawk 2.8.
			else if (configType.GetProperty("AllowUdlr") != null) {
				bool allowUldrFlag = (bool) configType.GetProperty("AllowUdlr").GetValue(Config!);
				if (!allowUldrFlag) {
					DialogController.ShowMessageBox("In order to use this tool, 'Allow U+D / L+R' must be checked in the controller menu.");
					this.Close();
					DialogResult = DialogResult.Cancel;
					return;
				}
			} 
			// Reject the tool from loading.
			else {
				DialogController.ShowMessageBox("Unsupported BizHawk version detected. Please report the issue on TASVideo Forum @ https://tasvideos.org/Forum/Topics/23453");
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

		public void FrameLengthNumeric_ValueChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
			this.populationManager.IsInitialized = false;
		}

		public void PopulationSizeNumeric_ValueChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
			this.populationManager.IsInitialized = false;
		}

		public void ClearStatsContextMenuItem_Click(object sender, EventArgs e) {
			Runs = 0;
			Frames = 0;
		}

		public void FileSubMenu_DropDownOpened(object sender, EventArgs e) {
			SaveMenuItem.Enabled = !string.IsNullOrWhiteSpace(CurrentFilename);
		}

		public void NewMenuItem_Click(object sender, EventArgs e) {
			CurrentFilename = "";
			this.populationManager.Initialize();

			foreach (var cp in ControlProbabilityPanel.Controls.OfType<BotControlsRow>()) {
				cp.Probability = 0;
			}

			FrameLength = 0;
			MaximizeAddress = 0;
			TieBreaker1Address = 0;
			TieBreaker2Address = 0;
			TieBreaker3Address = 0;
			StartFromSlotBox.SelectedIndex = 0;
			MainOperator.SelectedIndex = 0;
			Tiebreak1Operator.SelectedIndex = 0;
			Tiebreak2Operator.SelectedIndex = 0;
			Tiebreak3Operator.SelectedIndex = 0;
			MainValueNumeric.Value = 0;
			TieBreak1Numeric.Value = 0;
			TieBreak2Numeric.Value = 0;
			TieBreak3Numeric.Value = 0;
			MainBestRadio.Checked = true;
			TieBreak1BestRadio.Checked = true;
			TieBreak2BestRadio.Checked = true;
			TieBreak3BestRadio.Checked = true;

			UpdateBestAttemptUI();
		}

		public void OpenMenuItem_Click(object sender, EventArgs e) {
			var file = OpenFileDialog(
					CurrentFilename,
					Config!.PathEntries.ToolsAbsolutePath(),
					"Bot files",
					"bot");
			if (file != null) {
				LoadBotFile(file.FullName);
			}
		}

		public void SaveMenuItem_Click(object sender, EventArgs e) {
			if (!string.IsNullOrWhiteSpace(CurrentFilename)) {
				SaveBotFile(CurrentFilename);
			}
		}

		public void SaveAsMenuItem_Click(object sender, EventArgs e) {
			var fileName = CurrentFilename;
			if (string.IsNullOrWhiteSpace(fileName)) {
				fileName = Game.FilesystemSafeName();
			}

			var file = SaveFileDialog(
				fileName,
				Config!.PathEntries.ToolsAbsolutePath(),
				"Bot files",
				"bot",
				this);
			if (file != null) {
				SaveBotFile(file.FullName);
				_currentFilename = file.FullName;
			}
		}

		public void RecentSubMenu_DropDownOpened(object sender, EventArgs e) {
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Settings.RecentBotFiles.RecentMenu(MainForm, LoadFileFromRecent, "Bot Parameters"));
		}

		public void OptionsSubMenu_DropDownOpened(object sender, EventArgs e) {
			TurboWhileBottingMenuItem.Checked = Settings.TurboWhenBotting;
			BigEndianMenuItem.Checked = _bigEndian;
		}

		public void MemoryDomainsMenuItem_DropDownOpened(object sender, EventArgs e) {
			MemoryDomainsMenuItem.DropDownItems.Clear();
			MemoryDomainsMenuItem.DropDownItems.AddRange(MemoryDomains.MenuItems(SetMemoryDomain, _currentDomain.Name).ToArray());
		}

		public void DataSizeMenuItem_DropDownOpened(object sender, EventArgs e) {
			_1ByteMenuItem.Checked = _dataSize == 1;
			_2ByteMenuItem.Checked = _dataSize == 2;
			_4ByteMenuItem.Checked = _dataSize == 4;
		}

		public void OneByteMenuItem_Click(object sender, EventArgs e) {
			_dataSize = 1;
		}

		public void TwoByteMenuItem_Click(object sender, EventArgs e) {
			_dataSize = 2;
		}

		public void FourByteMenuItem_Click(object sender, EventArgs e) {
			_dataSize = 4;
		}

		public void BigEndianMenuItem_Click(object sender, EventArgs e) {
			_bigEndian ^= true;
		}

		public void TurboWhileBottingMenuItem_Click(object sender, EventArgs e) {
			Settings.TurboWhenBotting ^= true;
		}

		public void RunBtn_Click(object sender, EventArgs e) {
			StartBot();
		}

		public void StopBtn_Click(object sender, EventArgs e) {
			StopBot();
		}

		public void BtnCopyBestInput_Click(object sender, EventArgs e) {
			Clipboard.SetText(BestAttemptLogLabel.Text);
		}

		public void PlayBestButton_Click(object sender, EventArgs e) {
			StopBot();
			_replayMode = true;
			_doNotUpdateValues = true;

			// here we need to apply the initial frame's input from the best attempt
			BotAttempt bestAttempt = this.populationManager.GetBest().GetAttempt();
			var logEntry = bestAttempt.Log[0];
			var controller = MovieSession.GenerateMovieController();
			controller.SetFromMnemonic(logEntry);
			foreach (var button in controller.Definition.BoolButtons) {
				// TODO: make an input adapter specifically for the bot?
				InputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
			}

			InputManager.SyncControls(Emulator, MovieSession, Config);

			MainForm.LoadQuickSave(SelectedSlot, true); // Triggers an UpdateValues call
			_lastFrameAdvanced = Emulator.Frame;
			_doNotUpdateValues = false;
			_startFrame = Emulator.Frame;
			SetNormalSpeed();
			UpdateBotStatusIcon();
			MessageLabel.Text = "Replaying";
			MainForm.UnpauseEmulator();
		}

		public void ClearBestButton_Click(object sender, EventArgs e) {
			this.populationManager.IsInitialized = false;
			this.populationManager.GetBest().Reset(0);
			Runs = 0;
			Frames = 0;
			Generations = 1;
			UpdateBestAttemptUI();
		}

		public void MainBestRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				BotAttempt best = this.populationManager.GetBest().GetAttempt();
				MainValueNumeric.Enabled = false;
				comparisonAttempt.Maximize = best?.Maximize ?? 0;
			}
		}

		public void Tiebreak1BestRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				BotAttempt best = this.populationManager.GetBest().GetAttempt();
				TieBreak1Numeric.Enabled = false;
				comparisonAttempt.TieBreak1 = best?.TieBreak1 ?? 0;
			}
		}

		public void Tiebreak2BestRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				BotAttempt best = this.populationManager.GetBest().GetAttempt();
				TieBreak2Numeric.Enabled = false;
				comparisonAttempt.TieBreak2 = best?.TieBreak2 ?? 0;
			}
		}

		public void Tiebreak3BestRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				BotAttempt best = this.populationManager.GetBest().GetAttempt();
				TieBreak3Numeric.Enabled = false;
				comparisonAttempt.TieBreak3 = best?.TieBreak3 ?? 0;
			}
		}

		public void MainValueRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				MainValueNumeric.Enabled = true;
				comparisonAttempt.Maximize = (uint) MainValueNumeric.Value;
			}
		}

		public void TieBreak1ValueRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				TieBreak1Numeric.Enabled = true;
				comparisonAttempt.TieBreak1 = (uint) TieBreak1Numeric.Value;
			}
		}

		public void TieBreak2ValueRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				TieBreak2Numeric.Enabled = true;
				comparisonAttempt.TieBreak2 = (uint) TieBreak2Numeric.Value;
			}
		}

		public void TieBreak3ValueRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				TieBreak3Numeric.Enabled = true;
				comparisonAttempt.TieBreak3 = (uint) TieBreak3Numeric.Value;
			}
		}

		public void MainValueNumeric_ValueChanged(object sender, EventArgs e) {
			NumericUpDown numericUpDown = (NumericUpDown) sender;
			comparisonAttempt.Maximize = (uint) numericUpDown.Value;
		}

		public void TieBreak1Numeric_ValueChanged(object sender, EventArgs e) {
			NumericUpDown numericUpDown = (NumericUpDown) sender;
			comparisonAttempt.TieBreak1 = (uint) numericUpDown.Value;
		}

		public void TieBreak2Numeric_ValueChanged(object sender, EventArgs e) {
			NumericUpDown numericUpDown = (NumericUpDown) sender;
			comparisonAttempt.TieBreak2 = (uint) numericUpDown.Value;
		}

		public void TieBreak3Numeric_ValueChanged(object sender, EventArgs e) {
			NumericUpDown numericUpDown = (NumericUpDown) sender;
			comparisonAttempt.TieBreak3 = (uint) numericUpDown.Value;
		}

		public void MaximizeAddressBox_TextChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
		}

		public void TieBreaker1Box_TextChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
		}

		public void TieBreaker2Box_TextChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
		}

		public void TieBreaker3Box_TextChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
		}
		#endregion
	}

	public static class Utils {
		public static Random RNG { get; } = new Random((int) DateTime.Now.Ticks);

		public static readonly double MUTATION_RATE = 0.02;

		public static readonly double CROSSOVER_RATE = 50.0;

		public static bool IsBetter(GeneticAlgorithmBot bot, BotAttempt best, BotAttempt comparison, BotAttempt current) {
			uint max = bot.MainValueRadio.Checked ? comparison.Maximize : best.Maximize;
			if (!TestValue(bot.MainComparisonType, current.Maximize, max)) return false;
			if (current.Maximize != comparison.Maximize) return true;

			uint tie1 = bot.TieBreak1ValueRadio.Checked ? comparison.TieBreak1 : best.TieBreak1;
			if (!TestValue(bot.Tie1ComparisonType, current.TieBreak1, tie1)) return false;
			if (current.TieBreak1 != comparison.TieBreak1) return true;

			uint tie2 = bot.TieBreak2ValueRadio.Checked ? comparison.TieBreak2 : best.TieBreak2;
			if (!TestValue(bot.Tie2ComparisonType, current.TieBreak2, tie2)) return false;
			if (current.TieBreak2 != comparison.TieBreak2) return true;

			uint tie3 = bot.TieBreak3ValueRadio.Checked ? comparison.TieBreak3 : best.TieBreak3;
			if (!TestValue(bot.Tie3ComparisonType, current.TieBreak3, tie3)) return false;

			// TieBreak3 is equal, regardless of which attempt type they are.
			return true;
		}

		public static bool TestValue(byte operation, long currentValue, long bestValue)
				=> operation switch {
					0 => (currentValue > bestValue),
					1 => (currentValue >= bestValue),
					2 => (currentValue == bestValue),
					3 => (currentValue <= bestValue),
					4 => (currentValue < bestValue),
					5 => (currentValue != bestValue),
					_ => false
				};

		public static void DeepCopyAttempt(BotAttempt source, ref BotAttempt target) {
			target = new BotAttempt();
			target.Attempt = source.Attempt;
			target.Fitness = source.Fitness;
			target.Generation = source.Generation;
			target.Maximize = source.Maximize;
			target.TieBreak1 = source.TieBreak1;
			target.TieBreak2 = source.TieBreak2;
			target.TieBreak3 = source.TieBreak3;
			target.ComparisonTypeMain = source.ComparisonTypeMain;
			target.ComparisonTypeTie1 = source.ComparisonTypeTie1;
			target.ComparisonTypeTie2 = source.ComparisonTypeTie2;
			target.ComparisonTypeTie3 = source.ComparisonTypeTie3;
			target.isReset = source.isReset;

			target.Log.Clear();
			target.Log.AddRange(source.Log);
		}

		public static BotData BotDataReflectionCopy(object source) {
			BotData target = (BotData) Activator.CreateInstance(typeof(BotData));
			foreach (PropertyInfo p in source.GetType().GetProperties()) {
				if (p.Name.Equals("Best")) {
					BotAttempt attempt = Utils.BotAttemptReflectionCopy(p.GetValue(source));
					target.Best = new BotAttempt(attempt);
				}
				else if (p.Name.Equals("ControlProbabilities")) {
					Dictionary<string, double> sourceDict = (Dictionary<string, double>) p.GetValue(source);
					target.ControlProbabilities = new Dictionary<string, double>(sourceDict);
				}
				else if (p.Name.Equals("Attempts")) {
					target.GetType().GetProperty("Runs")!.SetValue(target, p.GetValue(source));
				}
				else {
					target.GetType().GetProperty(p.Name)!.SetValue(target, p.GetValue(source));
				}
			}
			return target;
		}

		public static BotAttempt BotAttemptReflectionCopy(object source) {
			BotAttempt target = (BotAttempt) Activator.CreateInstance(typeof(BotAttempt));
			foreach (PropertyInfo p in source.GetType().GetProperties()) {
				object value = p.GetValue(source);
				PropertyInfo targetInfo = typeof(BotAttempt).GetProperty(p.Name);
				if (value.GetType() == typeof(int)) {
					targetInfo!.SetValue(target, Convert.ToUInt32(value));
				}
				else if (p.Name.Equals("Log")) {
					List<string> logs = (List<string>) targetInfo.GetValue(target);
					logs.Clear();
					logs.AddRange((List<string>) p.GetValue(source));
				}
				else if (p.Name.Equals("is_Reset")) {
					typeof(BotAttempt).GetProperty("isReset")!.SetValue(target, p.GetValue(source));
				}
				else {
					typeof(BotAttempt).GetProperty(p.Name)!.SetValue(target, p.GetValue(source));
				}
			}
			return target;
		}
	}

	public class GeneticAlgorithm {
		private InputRecording _bestRecording;
		public InputRecording[] population;
		public int currentIndex = 0;
		public long Generation { get; set; }
		public int StartFrameNumber { get; set; } = 0;
		private GeneticAlgorithmBot bot;
		public bool IsInitialized { get; set; }
		public bool IsBestSet => this._bestRecording.IsSet;

		public GeneticAlgorithm(GeneticAlgorithmBot owner) {
			this.bot = owner;
			this.IsInitialized = false;
			this.Generation = 1;
			this._bestRecording = new InputRecording(owner, this);
			this.population = new InputRecording[1];
			for (int i = 0; i < this.population.Length; i++) {
				this.population[i] = new InputRecording(owner, this);
			}
		}

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
			return this._bestRecording;
		}

		public void ClearBest() {
			this._bestRecording.Reset(0);
		}

		// Returns true if the current index wraps back to zero.
		public bool NextRecording() {
			this.currentIndex = ++this.currentIndex % this.population.Length;
			return this.currentIndex == 0;
		}

		public long EvaluateGeneration() {
			int chosenIndex = -1;
			for (int i = 0; i < this.population.Length; i++) {
				if (Utils.IsBetter(this.bot, this.GetBest().result, this.bot.comparisonAttempt, this.population[i].result)) {
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
			BotAttempt best = this._bestRecording.GetAttempt();
			return Utils.IsBetter(this.bot, best, this.bot.comparisonAttempt, current);
		}

		public void CopyCurrentToBest(int index) {
			this._bestRecording.DeepCopy(this.population[index]);
			this._bestRecording.IsSet = true;
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

		/*
		 Proper Genetic Algorithm.
		 */

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
					if (Utils.RNG.NextDouble() <= Utils.MUTATION_RATE) {
						child.RandomizeFrameInput();
					}
				}
			}
		}
	}

	public class InputRecording {
		// If result "isReset" flag is set, the input recording hasn't started its attempt. Otherwise, any attempt counts as some result (success, skipped, fail).
		public BotAttempt result;
		public FrameInput[] recording;
		public double fitness { get; set; }
		public int FrameLength { get; set; }
		public bool IsSet { get; set; }
		private GeneticAlgorithmBot bot;
		private GeneticAlgorithm manager;

		public InputRecording(GeneticAlgorithmBot owner, GeneticAlgorithm parent) {
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
			float[] probabilities = bot.GetCachedInputProbabilities();
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

			float[] probabilities = bot.GetCachedInputProbabilities();
			int[] times = Enumerable.Range(0, count: this.bot.ControllerButtons.Count)
					.Where((buttonIndex, i) => Utils.RNG.NextDouble() < probabilities[buttonIndex])
					.ToArray();

			for (int i = 0; i < times.Length; i++) {
				input.Pressed(this.bot.ControllerButtons[times[i]]);
			}
			this.IsSet = true;
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

		public void DeepCopy(FrameInput other) {
			this.Buttons.UnionWith(other.Buttons);
			this.FrameNumber = other.FrameNumber;
		}
	}

	public class BotAttempt {
		public long Attempt { get; set; }
		public long Generation { get; set; }
		public int Fitness { get; set; }
		public uint Maximize { get; set; }
		public uint TieBreak1 { get; set; }
		public uint TieBreak2 { get; set; }
		public uint TieBreak3 { get; set; }
		public byte ComparisonTypeMain { get; set; }
		public byte ComparisonTypeTie1 { get; set; }
		public byte ComparisonTypeTie2 { get; set; }
		public byte ComparisonTypeTie3 { get; set; }
		public List<string> Log { get; } = new List<string>();
		public bool isReset { get; set; } = true;

		public BotAttempt() { }

		public BotAttempt(BotAttempt source) {
			this.Attempt = source.Attempt;
			this.Fitness = source.Fitness;
			this.Generation = source.Generation;
			this.Maximize = source.Maximize;
			this.TieBreak1 = source.TieBreak1;
			this.TieBreak2 = source.TieBreak2;
			this.TieBreak3 = source.TieBreak3;
			this.ComparisonTypeMain = source.ComparisonTypeMain;
			this.ComparisonTypeTie1 = source.ComparisonTypeTie1;
			this.ComparisonTypeTie2 = source.ComparisonTypeTie2;
			this.ComparisonTypeTie3 = source.ComparisonTypeTie3;
			this.isReset = source.isReset;

			this.Log = new List<string>();
			this.Log.AddRange(source.Log);
		}
	}

	public class GeneticAlgorithmBotSettings {
		public RecentFiles RecentBotFiles { get; set; } = new RecentFiles();
		public bool TurboWhenBotting { get; set; } = true;
		public bool InvisibleEmulation { get; set; }
	}

	public struct BotData {
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
		public bool MainCompareToBest { get; set; }
		public bool TieBreaker1CompareToBest { get; set; }
		public bool TieBreaker2CompareToBest { get; set; }
		public bool TieBreaker3CompareToBest { get; set; }
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

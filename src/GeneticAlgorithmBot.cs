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

		private string _lastOpenedRom = "";

		private MemoryDomain _currentDomain;

		private bool _bigEndian;

		private int _dataSize;

		private bool _replayMode;

		private bool _isBotting;

		private int _lastFrameAdvanced;

		private bool _oldCountingSetting;

		private bool _doNotUpdateValues;

		private ILogEntryGenerator _logGenerator;

		private readonly int POPULATION_SIZE = 4;

		public int _startFrame;

		public int _targetFrame;

		public GeneticAlgorithm populationManager;
		#endregion

		#region Variable Getters and Setters
		private long Generations {
			get {
				return this.populationManager.Generation;
			}
			set {
				this.populationManager.Generation = value;
			}
		}

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

		public long Runs { get; set; }

		public long Frames { get; set; }

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
			this.populationManager = new GeneticAlgorithm(this, POPULATION_SIZE);
			this.MainOperator.SelectedItem = ">=";
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
			this.populationManager.SetOrigin();
			this.populationManager.GetCurrent().Reset(Runs);

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
			if (this.populationManager.GetBest() != null) {
				FrameInput inputs = this.populationManager.GetBest().GetFrameInput(Emulator.Frame);
				foreach (var button in inputs.Buttons) {
					InputManager.ClickyVirtualPadController.SetBool(button, false);
				}
				InputManager.SyncControls(Emulator, MovieSession, Config);

				if (clear_log)
					this.populationManager.ClearCurrentRecordingLog();
				this.populationManager.SetCurrentRecordingLog(_logGenerator.GenerateLogEntry());
			}
		}

		public void UpdateBestAttempt() {
			if (!this.populationManager.GetBest().GetAttempt().isReset) {
				ClearBestButton.Enabled = true;
				btnCopyBestInput.Enabled = true;
				BotAttempt best = this.populationManager.GetBest().GetAttempt();
				BestAttemptNumberLabel.Text = best.Attempt.ToString();
				BestMaximizeBox.Text = best.Maximize.ToString();
				BestTieBreak1Box.Text = best.TieBreak1.ToString();
				BestTieBreak2Box.Text = best.TieBreak2.ToString();
				BestTieBreak3Box.Text = best.TieBreak3.ToString();

				Console.WriteLine($"Logging attempt:  Log size: {best.Log.Count}");
				var sb = new StringBuilder();
				foreach (var logEntry in best.Log) {
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
					PlayBestButton.Enabled = true;

					if (best.isReset || this.populationManager.IsCurrentAttemptBetter()) {
						this.populationManager.CopyCurrentToBest();
						UpdateBestAttempt();
					}

					if (this.populationManager.NextRecording()) {
						this.populationManager.Reproduce();
						Generations = this.populationManager.NextGeneration();
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

			_currentDomain = !string.IsNullOrWhiteSpace(botData.MemoryDomain) ? MemoryDomains[botData.MemoryDomain]! : MemoryDomains.MainMemory;

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

		public void FrameLengthNumeric_ValueChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
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

			UpdateBestAttempt();
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
			BotAttempt best = this.populationManager.GetBest().GetAttempt();
			best.isReset = true;
			Runs = 0;
			Frames = 0;
			Generations = 0;
			UpdateBestAttempt();
		}
		#endregion
	}

	public static class Utils {
		public static Random RNG { get; } = new Random((int) DateTime.Now.Ticks);

		public static bool IsBetter(GeneticAlgorithmBot bot, BotAttempt comparison, BotAttempt current) {
			if (!TestValue(bot.MainComparisonType, current.Maximize, comparison.Maximize)) return false;
			if (current.Maximize != comparison.Maximize) return true;

			if (!TestValue(bot.Tie1ComparisonType, current.TieBreak1, comparison.TieBreak1)) return false;
			if (current.TieBreak1 != comparison.TieBreak1) return true;

			if (!TestValue(bot.Tie2ComparisonType, current.TieBreak2, comparison.TieBreak2)) return false;
			if (current.TieBreak2 != comparison.TieBreak2) return true;

			if (!TestValue(bot.Tie3ComparisonType, current.TieBreak3, comparison.TieBreak3)) return false;
			/*if (current.TieBreak3 != comparison.TieBreak3)*/
			return true;
		}

		public static bool TestValue(byte operation, int currentValue, int bestValue)
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
	}

	public class GeneticAlgorithm : IComparer {
		private BotAttempt _beginning;
		private InputRecording _bestRecording;
		public InputRecording[] population;
		public int currentIndex = 0;
		public long Generation { get; set; }
		private GeneticAlgorithmBot bot;

		public GeneticAlgorithm(GeneticAlgorithmBot owner, int populationSize) {
			this.bot = owner;
			this._beginning = new BotAttempt();
			this._bestRecording = new InputRecording(owner, this);
			this.population = new InputRecording[populationSize];
			for (int i = 0; i < populationSize; i++) {
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

		// Returns true if the current index wraps back to zero.
		public bool NextRecording() {
			this.currentIndex = ++this.currentIndex % this.population.Length;
			return this.currentIndex == 0;
		}

		public long NextGeneration() {
			return ++this.Generation;
		}

		public bool IsCurrentAttemptBetter() {
			BotAttempt current = this.population[this.currentIndex].GetAttempt();
			BotAttempt best = this._bestRecording.GetAttempt();
			return Utils.IsBetter(this.bot, best, current);
		}

		public void CopyCurrentToBest() {
			BotAttempt current = this.population[this.currentIndex].GetAttempt();
			BotAttempt best = this._bestRecording.GetAttempt();
			best.Attempt = current.Attempt;
			best.Maximize = current.Maximize;
			best.TieBreak1 = current.TieBreak1;
			best.TieBreak2 = current.TieBreak2;
			best.TieBreak3 = current.TieBreak3;

			best.Log.Clear();
			for (int i = 0; i < current.Log.Count; i++) {
				best.Log.Add(current.Log[i]);
			}
			best.isReset = false;
		}

		public void SetOrigin() {
			this._beginning.Fitness = 0;
			this._beginning.Attempt = 0;
			this._beginning.Generation = 0;
			this._beginning.ComparisonTypeMain = this.bot.MainComparisonType;
			this._beginning.ComparisonTypeTie1 = this.bot.Tie1ComparisonType;
			this._beginning.ComparisonTypeTie2 = this.bot.Tie2ComparisonType;
			this._beginning.ComparisonTypeTie3 = this.bot.Tie3ComparisonType;
			this._beginning.Maximize = this.bot.MaximizeValue;
			this._beginning.TieBreak1 = this.bot.TieBreaker1Value;
			this._beginning.TieBreak2 = this.bot.TieBreaker2Value;
			this._beginning.TieBreak3 = this.bot.TieBreaker3Value;
			this._beginning.isReset = false;
		}

		/*
		 Proper Genetic Algorithm.
		 */

		public void Initialize() {
			this._bestRecording.Reset(0);
			this.currentIndex = 0;
			for (int i = 0; i < this.population.Length; i++) {
				InputRecording rec = this.population[i];
				rec.Reset(0);
				rec.RandomizeInputRecording();
			}
		}

		public void Reproduce() {
			for (int i = 0; i< this.population.Length; i++) {
				InputRecording rec = new InputRecording(this.bot, this);

				// Uniform distribution crossover.
				for (int f = 0; f < rec.FrameLength; f++) {
					rec.SetFrameInput(f, (Utils.RNG.Next(2) == 0 ? this._bestRecording.GetFrameInput(f) : this.population[i].GetFrameInput(f)));
				}

				//Uniform distribution mutation.
				rec.RandomizeFrameInput();

				this.population[i] = rec;
			}
		}

		// Comparer
		/// <summary>
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>
		/// Less than zero (This instance precedes other in the sort order.)<br/>
		/// Zero (This instance occurs in the same position in the sort order as other.)<br/>
		/// Greater than zero (This instance follows other in the sort order.)
		/// </returns>
		public int Compare(object x, object y) {
			if (x == null || y == null)
				return 1;
			BotAttempt xAttempt = ((InputRecording) x).GetAttempt();
			BotAttempt yAttempt = ((InputRecording) y).GetAttempt();
			if (Utils.IsBetter(this.bot, xAttempt, yAttempt))
				return -1;
			return 1;
			// No zero. It can never be zero.
		}
	}

	public class InputRecording {
		// If result "isReset" flag is set, the input recording hasn't started its attempt. Otherwise, any attempt counts as some result (success, skipped, fail).
		public BotAttempt result;
		public FrameInput[] recording;
		public double fitness { get; set; }
		public int StartFrameNumber { get; set; }
		public int FrameLength { get; set; }
		private GeneticAlgorithmBot bot;
		private GeneticAlgorithm manager;

		public InputRecording(GeneticAlgorithmBot owner, GeneticAlgorithm parent) {
			this.bot = owner;
			this.manager = parent;
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
				FrameInput input = this.GetFrameInput(this.StartFrameNumber + i);
				for (int j = 0; j < values[i].Length; j++) {
					input.Pressed(this.bot.ControllerButtons[values[i][j]]);
				}
			}
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
		}

		public void SetResult() {
			this.result.Attempt = this.bot.Runs;
			this.result.Maximize = this.bot.MaximizeValue;
			this.result.TieBreak1 = this.bot.TieBreaker1Value;
			this.result.TieBreak2 = this.bot.TieBreaker2Value;
			this.result.TieBreak3 = this.bot.TieBreaker3Value;
			this.result.ComparisonTypeMain = this.bot.MainComparisonType;
			this.result.ComparisonTypeTie1 = this.bot.Tie1ComparisonType;
			this.result.ComparisonTypeTie2 = this.bot.Tie2ComparisonType;
			this.result.ComparisonTypeTie3 = this.bot.Tie3ComparisonType;
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

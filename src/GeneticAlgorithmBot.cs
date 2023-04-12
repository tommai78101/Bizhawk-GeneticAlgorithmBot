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
using GeneticAlgorithmBot;
using BizHawk.Bizware.BizwareGL;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace GeneticAlgorithmBot {
	[ExternalTool("Genetic Algorithm Bot")]
	public sealed partial class GeneticAlgorithmBot : ToolFormBase, IExternalToolForm, IToolFormAutoConfig {
		#region Static Variables
		public static Type Resources => typeof(ToolFormBase).Assembly.GetType("BizHawk.Client.EmuHawk.Properties.Resources");

		private static readonly FilesystemFilterSet BotFilesFSFilterSet = new(new FilesystemFilter("Bot files", new[] { "bot" }));
		#endregion

		#region Variables
		private string _windowTitle = "Genetic Algorithm Bot";

		private string _currentFilename = "";

		private bool _previousDisplayMessage = false;

		private bool _previousInvisibleEmulation = false;

		private bool _useNeat = false;

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

		public GeneticAlgorithm genetics;

		public NeatAlgorithm neat;

		public BotAlgorithm algorithm { get; set; } = default!;
		#endregion

		#region Settings
		[ConfigPersist]
		public RecentFiles recentFiles {
			get => this.Settings.RecentBotFiles;
			set => this.Settings.RecentBotFiles = value;
		}

		[ConfigPersist]
		public bool TurboWhenBotting {
			get => this.Settings.TurboWhenBotting;
			set => this.Settings.TurboWhenBotting = value;
		}

		[ConfigPersist]
		public bool InvisibleEmulation {
			get => this.Settings.InvisibleEmulation;
			set => this.Settings.InvisibleEmulation = value;
		}

		public GeneticAlgorithmBotSettings Settings { get; set; }
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
		private IStatable StatableCore { get; set; } = default!;

		[RequiredService]
		public IMemoryDomains MemoryDomains { get; set; } = default!;

		[RequiredService]
		public IVideoProvider _currentVideoProvider { get; set; } = default!;

		public int FrameLength {
			get => (int) FrameLengthNumeric.Value;
			set => FrameLengthNumeric.Value = value;
		}

		public int PopulationSize {
			get => (int) PopulationSizeNumeric.Value;
			set => PopulationSizeNumeric.Value = value;
		}

		public decimal MutationRate {
			get => MutationRateNumeric.Value;
			set => MutationRateNumeric.Value = (decimal) value;
		}

		public ClickyVirtualPadController Controller => InputManager.ClickyVirtualPadController;

		public IList<string> ControllerButtons => Emulator.ControllerDefinition.BoolButtons;

		public ulong? MaximizeAddress {
			get => MaximizeAddressBox.ToU64();
			set => MaximizeAddressBox.SetFromU64(value);
		}

		public int MaximizeValue => GetRamValue(MaximizeAddress);

		public ulong? TieBreaker1Address {
			get => TieBreaker1Box.ToU64();
			set => TieBreaker1Box.SetFromU64(value);
		}

		public int TieBreaker1Value => GetRamValue(TieBreaker1Address);

		public ulong? TieBreaker2Address {
			get => TieBreaker2Box.ToU64();
			set => TieBreaker2Box.SetFromU64(value);
		}

		public int TieBreaker2Value => GetRamValue(TieBreaker2Address);

		public ulong? TieBreaker3Address {
			get => TieBreaker3Box.ToU64();
			set => TieBreaker3Box.SetFromU64(value);
		}

		public int TieBreaker3Value => GetRamValue(TieBreaker3Address);

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

		public int GetRamValue(ulong? address) {
			if (address is null)
				return 0;
			var addr = checked((long) address);
			int val = _dataSize switch {
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

		public int SelectedSlot => 1 + StartFromSlotBox.SelectedIndex;
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
			this.genetics = new GeneticAlgorithm(this);
			this.neat = new NeatAlgorithm(this);
			this.MainOperator.SelectedItem = ">=";
			this.comparisonAttempt = new BotAttempt();
			RunBtn.Enabled = false;
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
			// This has to do with loading and saving save states, which is something the bot needs to function.
			_ = StatableCore!;

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

			this.algorithm = _useNeat ? this.neat : this.genetics;
			if (_useNeat && !this.neat.IsInitialized) {
				NeatConstants.InputSize = ControllerButtons.Count;
				NeatConstants.OutputSize = ControllerButtons.Count;
				NeatConstants.MaxClients = PopulationSize;
				neat.Reset();
				neat.Initialize();
			}
			else if (!this.genetics.IsInitialized) {
				this.genetics.Initialize();
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
			if (!_useNeat)
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

			_previousInvisibleEmulation = MainForm.InvisibleEmulation;
			MainForm.InvisibleEmulation = InvisibleEmulationCheckBox.Checked;

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
			if (!_useNeat && !ControlProbabilities.Any(cp => cp.Value > 0)) {
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
			if (_useNeat) {
				Client client = neat.GetCurrentClient();
				FrameInput inputs = this.neat.GetCurrent().GenerateFrameInput(Emulator.Frame, client.Calculate(GetCachedInputProbabilitiesDouble()));
				foreach (var button in inputs.Buttons) {
					InputManager.ClickyVirtualPadController.SetBool(button, false);
				}
				InputManager.SyncControls(Emulator, MovieSession, Config);

				if (clear_log)
					this.neat.ClearCurrentRecordingLog();
				this.neat.SetCurrentRecordingLog(_logGenerator.GenerateLogEntry());
			}
			else {
				if (this.genetics.GetCurrent() != null) {
					FrameInput inputs = this.genetics.GetCurrent().GetFrameInput(Emulator.Frame);
					foreach (var button in inputs.Buttons) {
						InputManager.ClickyVirtualPadController.SetBool(button, false);
					}
					InputManager.SyncControls(Emulator, MovieSession, Config);

					if (clear_log)
						this.genetics.ClearCurrentRecordingLog();
					this.genetics.SetCurrentRecordingLog(_logGenerator.GenerateLogEntry());
				}
			}
		}

		public void UpdateBestAttemptUI() {
			ClearBestButton.Enabled = true;
			if (this.algorithm.GetBest().IsSet) {
				btnCopyBestInput.Enabled = true;
				BotAttempt best = this.algorithm.GetBest().GetAttempt();
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

		public float[] GetCachedInputProbabilitiesFloat() {
			float[] target = new float[Emulator.ControllerDefinition.BoolButtons.Count];
			for (int i = 0; i < Emulator.ControllerDefinition.BoolButtons.Count; i++) {
				string button = Emulator.ControllerDefinition.BoolButtons[i];
				target[i] = (float) ControlProbabilities[button] / 100.0f;
			}
			return target;
		}

		public double[] GetCachedInputProbabilitiesDouble() {
			double[] target = new double[Emulator.ControllerDefinition.BoolButtons.Count];
			for (int i = 0; i < Emulator.ControllerDefinition.BoolButtons.Count; i++) {
				string button = Emulator.ControllerDefinition.BoolButtons[i];
				target[i] = ControlProbabilities[button] / 100.0;
			}
			return target;
		}

		public double[] GetNonZeroCachedInputProbabilitiesDouble() {
			List<double> target = new List<double>();
			for (int i = 0; i < Emulator.ControllerDefinition.BoolButtons.Count; i++) {
				string button = Emulator.ControllerDefinition.BoolButtons[i];
				double value = ControlProbabilities[button] / 100.0;
				if (value > 0.0) {
					target.Add(value);
				}
			}
			return target.ToArray();
		}

		// Controls need to be set and synced after emulation, so that everything works out properly at the start of the next frame
		// Consequently, when loading a state, input needs to be set before the load, to ensure everything works out in the correct order
		protected override void UpdateAfter() => Update(fast: false);
		protected override void FastUpdateAfter() => Update(fast: true);

		public void Update(bool fast) {
			if (_useNeat) {
				UpdateNeat(fast);
			}
			else {
				UpdateGeneticAlgorithm(fast);
			}
		}

		public void UpdateNeat(bool fast) {
			if (_doNotUpdateValues) {
				return;
			}

			if (!HasFrameAdvanced()) {
				return;
			}

			BotAttempt best = neat.GetBest().GetAttempt();
			if (_replayMode) {
				int index = Emulator.Frame - _startFrame;
				if (index < best.Log.Count) {
					var logEntry = best.Log[index];
					var controller = MovieSession.GenerateMovieController();
					controller.SetFromMnemonic(logEntry);
					foreach (var button in controller.Definition.BoolButtons) {
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
					Frames += FrameLength;

					// TODO(Thompson): Finish and complete "setting the results" to the InputRecording class object in NEAT.
					neat.GetCurrent().SetResult();
					if (neat.NextRecording()) {
						// Evolve the NEAT when all clients have attempted their input recordings.
						neat.Evolve();
						Runs++;

						// Replace these methods with the NEAT equivalent.
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
				if (neat.GetCurrent().GetAttempt().Log.Count < FrameLength) //aka do not Add more inputs than there are Frames to test
				{
					// We need to use the display as the inputs for NEAT.
					IDisposable disposableOutput = null!;
					BitmapBuffer bitmapBufferIn = null!;
					Bitmap bitmapInput = null!;
					Bitmap bitmapOutput = null!;
					if (_currentVideoProvider.BufferWidth > 0 && _currentVideoProvider.BufferHeight > 0) {
						try {
							byte[] data = GetScreenshotRegionData(0, 0, 1, 1);


							bitmapOutput = new Bitmap(width: _currentVideoProvider.BufferWidth, height: _currentVideoProvider.BufferHeight, PixelFormat.Format32bppArgb);
							using (var g = Graphics.FromImage(bitmapOutput)) {
								g.InterpolationMode = InterpolationMode.NearestNeighbor;
								g.PixelOffsetMode = PixelOffsetMode.Half;
								g.DrawImage(bitmapInput, new Rectangle(0, 0, bitmapOutput.Width, bitmapOutput.Height));
							}
							IVideoProvider output = new BmpVideoProvider(bitmapOutput, _currentVideoProvider.VsyncNumerator, _currentVideoProvider.VsyncDenominator);
							disposableOutput = (IDisposable) output;
						}
						finally {
							bitmapBufferIn?.Dispose();
							bitmapOutput?.Dispose();
						}
					}


					PressButtons(false);
					_lastFrameAdvanced = Emulator.Frame;
				}
			}

		}

		public void UpdateGeneticAlgorithm(bool fast) {
			if (_doNotUpdateValues) {
				return;
			}

			if (!HasFrameAdvanced()) {
				return;
			}

			BotAttempt best = this.genetics.GetBest().GetAttempt();
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
				// Using standard Genetic Algorithm
				if (Emulator.Frame >= _targetFrame) {
					Runs++;
					Frames += FrameLength;

					this.genetics.GetCurrent().SetResult();
					if (this.genetics.NextRecording()) {
						this.genetics.Reproduce();
						Generations = this.genetics.EvaluateGeneration();
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
				if (this.genetics.GetCurrent().GetAttempt().Log.Count < FrameLength) //aka do not Add more inputs than there are Frames to test
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

		public byte[] GetScreenshotRegionData(int x, int y, int width, int height) {
			BitmapBuffer screenshot = GetScreenshotImage();
			Bitmap screenshotImage = screenshot.ToSysdrawingBitmap();
			BitmapData data = screenshotImage.LockBits(new Rectangle(x, y, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			int length = data.Stride * data.Height;
			byte[] bytes = new byte[length];
			Marshal.Copy(data.Scan0, bytes, 0, length);
			screenshotImage.UnlockBits(data);
			return bytes;
		}
		#endregion

		#region Private methods
		/// <summary>
		/// Updates comparison bot attempt with current best bot attempt values for values where the "best" radio button is selected
		/// </summary>
		private void UpdateComparisonBotAttempt() {
			BotAttempt best = this.algorithm.GetBest().GetAttempt();
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
				&& ((!_useNeat && ControlProbabilities.Any(kvp => kvp.Value > 0)) || (_useNeat));
		}

		private void LoadFileFromRecent(string path) {
			var result = LoadBotFile(path);
			if (!result && !File.Exists(path)) {
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
				try {
					// Attempts to load GeneticAlgorithmBot .BOT file save data.
					botData = (BotData) ConfigService.LoadWithType(json);
				}
				catch (InvalidCastException) {
					// If exception is thrown, attempt to load BasicBot .BOT file save data instead.
					botData = Utils.BotDataReflectionCopy(ConfigService.LoadWithType(json));
				}
			}
			catch (InvalidCastException e) {
				using ExceptionBox dialog = new(e);
				this.ShowDialogAsChild(dialog);
				return false;
			}

			// Additional checks to make sure the BOT file is targeted at the loaded ROM game.
			if (botData.SysID != Emulator.SystemId) {
				this.ModalMessageBox(text: $"This file was made for a different system ({botData.SysID}).");
				if (!string.IsNullOrEmpty(botData.SysID)) {
					// there's little chance the file would load without throwing, and if it did, it wouldn't be useful
					// Otherwise grandfathered (made with old version, sysID unknowable), user has been warned.
					return false;
				}
			}
			bool isHawkVersionMatches = VersionInfo.DeveloperBuild || botData.HawkVersion == VersionInfo.GetEmuVersion();
			bool isCoreNameMatches = botData.CoreName == Emulator.Attributes().CoreName;
			bool isGameNameMatches = botData.GameName == Game.Name;
			if (!(isHawkVersionMatches && isCoreNameMatches && isGameNameMatches)) {
				// Inconsistent data found. Warn the user.
				string msg = isHawkVersionMatches
					? isCoreNameMatches
						? string.Empty
						: $" with a different core ({botData.CoreName ?? "unknown"})"
					: isCoreNameMatches
						? " with a different version of EmuHawk"
						: $" with a different core ({botData.CoreName ?? "unknown"}) on a different version of EmuHawk";
				if (!isGameNameMatches)
					msg = $"for a different game ({botData.GameName ?? "unknown"}){msg}";
				if (!this.ModalMessageBox2(
						text: $"This file was made {msg}. Load it anyway?",
						caption: "Confirm file load",
						icon: EMsgBoxIcon.Question)) {
					return false;
				}
			}

			try {
				LoadBotFileInner(botData, path);
				return true;
			}
			catch (Exception e) {
				using ExceptionBox dialog = new(e);
				this.ShowDialogAsChild(dialog);
				return false;
			}
		}

		private void LoadBotFileInner(BotData botData, string path) {
			// At this point, BotData is guaranteed to be valid.
			this.genetics.GetBest().GetAttempt().Attempt = botData.Best?.Attempt ?? 0;
			this.genetics.GetBest().GetAttempt().Maximize = botData.Best?.Maximize ?? 0;
			this.genetics.GetBest().GetAttempt().TieBreak1 = botData.Best?.TieBreak1 ?? 0;
			this.genetics.GetBest().GetAttempt().TieBreak2 = botData.Best?.TieBreak2 ?? 0;
			this.genetics.GetBest().GetAttempt().TieBreak3 = botData.Best?.TieBreak3 ?? 0;

			// no references to ComparisonType parameters

			this.genetics.GetBest().GetAttempt().Log.Clear();

			for (int i = 0; i < botData.Best?.Log?.Count; i++) {
				this.genetics.GetBest().GetAttempt().Log.Add(botData.Best.Log[i]);
			}

			this.genetics.GetBest().GetAttempt().isReset = false;

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

			if (this.genetics.GetBest().IsSet) {
				PlayBestButton.Enabled = true;
			}

			CurrentFilename = path;
			Settings.RecentBotFiles.Add(CurrentFilename);
			MessageLabel.Text = $"{Path.GetFileNameWithoutExtension(path)} loaded";

			AssessRunButtonStatus();
		}

		private void SaveBotFile(string path) {
			BotData data = new BotData {
				Best = this.genetics.GetBest().GetAttempt(),
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
				DataSize = _dataSize,
				HawkVersion = VersionInfo.GetEmuVersion(),
				SysID = Emulator.SystemId,
				CoreName = Emulator.Attributes().CoreName,
				GameName = Game.Name
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

		private BitmapBuffer GetScreenshotImage() {
			BitmapBuffer result = new BitmapBuffer(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight, _currentVideoProvider.GetVideoBuffer());
			result.DiscardAlpha();
			return result;
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
			this.genetics.IsInitialized = false;
			this.neat.IsInitialized = false;
		}

		public void PopulationSizeNumeric_ValueChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
			this.genetics.IsInitialized = false;
			this.neat.IsInitialized = false;
		}

		public void MutationRateNumeric_ValueChanged(object sender, EventArgs e) {
			AssessRunButtonStatus();
			this.genetics.IsInitialized = false;
			this.neat.IsInitialized = false;
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
			this.genetics.Initialize();

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
					currentFile: CurrentFilename,
					path: Config!.PathEntries.ToolsAbsolutePath(),
					BotFilesFSFilterSet);
			if (file != null) {
				// Discarding the returned value.
				_ = LoadBotFile(file.FullName);
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
				currentFile: fileName,
				path: Config!.PathEntries.ToolsAbsolutePath(),
				BotFilesFSFilterSet,
				this);
			if (file != null) {
				SaveBotFile(file.FullName);
				_currentFilename = file.FullName;
			}
		}

		public void RecentSubMenu_DropDownOpened(object sender, EventArgs e) {
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Settings.RecentBotFiles.RecentMenu(this, LoadFileFromRecent, "Bot Parameters"));
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
			BotAttempt bestAttempt = this.genetics.GetBest().GetAttempt();
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
			this.genetics.IsInitialized = false;
			this.genetics.GetBest().Reset(0);
			Runs = 0;
			Frames = 0;
			Generations = 1;
			UpdateBestAttemptUI();
			this.ClearBestButton.Enabled = false;
		}

		public void MainBestRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				BotAttempt best = this.genetics.GetBest().GetAttempt();
				MainValueNumeric.Enabled = false;
				comparisonAttempt.Maximize = best?.Maximize ?? 0;
			}
		}

		public void Tiebreak1BestRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				BotAttempt best = this.genetics.GetBest().GetAttempt();
				TieBreak1Numeric.Enabled = false;
				comparisonAttempt.TieBreak1 = best?.TieBreak1 ?? 0;
			}
		}

		public void Tiebreak2BestRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				BotAttempt best = this.genetics.GetBest().GetAttempt();
				TieBreak2Numeric.Enabled = false;
				comparisonAttempt.TieBreak2 = best?.TieBreak2 ?? 0;
			}
		}

		public void Tiebreak3BestRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				BotAttempt best = this.genetics.GetBest().GetAttempt();
				TieBreak3Numeric.Enabled = false;
				comparisonAttempt.TieBreak3 = best?.TieBreak3 ?? 0;
			}
		}

		public void MainValueRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				MainValueNumeric.Enabled = true;
				comparisonAttempt.Maximize = (int) MainValueNumeric.Value;
			}
		}

		public void TieBreak1ValueRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				TieBreak1Numeric.Enabled = true;
				comparisonAttempt.TieBreak1 = (int) TieBreak1Numeric.Value;
			}
		}

		public void TieBreak2ValueRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				TieBreak2Numeric.Enabled = true;
				comparisonAttempt.TieBreak2 = (int) TieBreak2Numeric.Value;
			}
		}

		public void TieBreak3ValueRadio_CheckedChanged(object sender, EventArgs e) {
			if (sender is RadioButton radioButton && radioButton.Checked) {
				TieBreak3Numeric.Enabled = true;
				comparisonAttempt.TieBreak3 = (int) TieBreak3Numeric.Value;
			}
		}

		public void MainValueNumeric_ValueChanged(object sender, EventArgs e) {
			NumericUpDown numericUpDown = (NumericUpDown) sender;
			comparisonAttempt.Maximize = (int) numericUpDown.Value;
		}

		public void TieBreak1Numeric_ValueChanged(object sender, EventArgs e) {
			NumericUpDown numericUpDown = (NumericUpDown) sender;
			comparisonAttempt.TieBreak1 = (int) numericUpDown.Value;
		}

		public void TieBreak2Numeric_ValueChanged(object sender, EventArgs e) {
			NumericUpDown numericUpDown = (NumericUpDown) sender;
			comparisonAttempt.TieBreak2 = (int) numericUpDown.Value;
		}

		public void TieBreak3Numeric_ValueChanged(object sender, EventArgs e) {
			NumericUpDown numericUpDown = (NumericUpDown) sender;
			comparisonAttempt.TieBreak3 = (int) numericUpDown.Value;
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

		public void UseNeatCheckBox_CheckedChanged(object sender, EventArgs e) {
			this._useNeat = UseNeatCheckBox.Checked;
			AssessRunButtonStatus();
		}
		#endregion
	}
}

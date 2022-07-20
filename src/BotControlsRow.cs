using System;
using System.Windows.Forms;

namespace GeneticAlgorithmBot {
	public partial class BotControlsRow : UserControl
	{
		private bool _programmaticallyChangingValues;

		public BotControlsRow()
		{
			InitializeComponent();

			Action? nullable = default;
			this.ProbabilityChangedCallback = nullable!;
		}

		public Action ProbabilityChangedCallback { get; set; }

		public string ButtonName
		{
			get => ButtonNameLabel.Text;
			set => ButtonNameLabel.Text = value;
		}

		public double Probability
		{
			get => (double)ProbabilityUpDown.Value;
			set => ProbabilityUpDown.Value = (decimal)value;
		}

		private void ProbabilityUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_programmaticallyChangingValues = true;
				ProbabilitySlider.Value = (int)ProbabilityUpDown.Value;
				ProbabilityChangedCallback?.Invoke();
				_programmaticallyChangingValues = false;
			}
		}

		private void ProbabilitySlider_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_programmaticallyChangingValues = true;
				ProbabilityUpDown.Value = ProbabilitySlider.Value;
				ProbabilityChangedCallback?.Invoke();
				_programmaticallyChangingValues = false;
			}
		}
	}
}

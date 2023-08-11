using GeneticAlgorithmBot.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneticAlgorithmBot {
	public partial class NeatMappingRow : UserControl {
		internal static int uniqueId = 0;
		internal static HashSet<string> usedMappings = new HashSet<string>();

		private int id;
		private Panel parent;

		public bool Exists => this.GetOutput() != null;

		public NeatMappingRow(Panel parent, IEnumerable<string> buttonOutputs) {
			InitializeComponent();
			this.Name = buttonOutputs.Except(usedMappings).FirstOrDefault();
			this.parent = parent;
			this.inputLabel.Text = this.Name;
			this.outputSelectionBox.Items.Add("-- Disabled --");
			this.outputSelectionBox.Items.AddRange(buttonOutputs.ToArray());
			this.outputSelectionBox.SelectedIndex = 0;
			usedMappings.Add(this.Name);
		}

		public NeatMappingRow Pop() {
			usedMappings.Remove(this.Name);
			return this;
		}

		public string GetInput() {
			return this.Name;
		}

		public string? GetOutput() {
			if (this.outputSelectionBox.SelectedIndex != 0) {
				return this.outputSelectionBox.SelectedItem.ToString();
			}
			return null;
		}
	}
}

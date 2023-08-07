using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneticAlgorithmBot.Common {
	public class NeatInputMappings {
		private const int MAX_MAPPINGS_SIZE = 64;
		private GeneticAlgorithmBot owner;
		private bool isScrollingEnabled = false;

		public Panel Parent => this.owner.NeatMappingPanel;
		public Control.ControlCollection Controls => Parent.Controls;

		public NeatInputMappings(GeneticAlgorithmBot owner) {
			this.owner = owner;
			this.isScrollingEnabled = false;

			Parent.AutoScroll = false;
			Parent.HorizontalScroll.Enabled = false;
			Parent.HorizontalScroll.Visible = false;
			Parent.HorizontalScroll.Maximum = 0;
			Parent.AutoScroll = true;
		}

		public void Push(NeatMappingRow mapping) {
			if (Controls.Count >= MAX_MAPPINGS_SIZE || mapping == null) {
				return;
			}
			Controls.Add(mapping);

			Point location = mapping.Location;
			location.Y += (Controls.Count - 1) * mapping.Height;
			mapping.Location = location;
		}

		public void Pop() {
			if (Controls.Count <= 0 || Controls[Controls.Count - 1] == null) {
				return;
			}
			NeatMappingRow row = (NeatMappingRow) Controls[Controls.Count - 1];
			Controls.Remove(row.Pop());
		}
	}
}

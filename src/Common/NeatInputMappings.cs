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
		public bool HasControls => Controls.Count > 0;

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

		public List<NeatMappingRow> GetDisabledMappings() {
			List<NeatMappingRow> rows = new List<NeatMappingRow>();
			for (int i = 0; i < Controls.Count; i++) {
				NeatMappingRow row = (NeatMappingRow) Controls[i];
				if (!row.Exists) {
					rows.Add(row);
				}
			}
			return rows;
		}

		public List<NeatMappingRow> GetEnabledMappings() {
			List<NeatMappingRow> rows = new List<NeatMappingRow>();
			if (Controls.Count > 0) {
				for (int i = 0; i < Controls.Count; i++) {
					NeatMappingRow row = (NeatMappingRow) Controls[i];
					if (row.Exists) {
						rows.Add(row);
					}
				}
			} else {

			}
			return rows;
		}

		public NeatMappingRow? GetRow(string button) {
			for (int i = 0; i < Controls.Count; i++) {
				NeatMappingRow row = (NeatMappingRow) Controls[i];
				if (row.Exists && row.GetOutput()!.Equals(button))
					return row;
			}
			return null;
		}
	}
}

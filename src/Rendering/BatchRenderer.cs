using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.MSX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot.Rendering {
	public class BatchRenderer {
		private GeneticAlgorithmBot owner;
		private IGuiApi gui = null!;
		private readonly Color outline = Color.Black;
		private readonly Color fill = Color.White;

		public List<NodeGene> Nodes => this.owner.neat.AllNodes.GetData();
		public List<ConnectionGene> Connections => this.owner.neat.AllConnections.Keys.ToList();
		public bool IsBotting => this.owner._isBotting;

		public BatchRenderer(GeneticAlgorithmBot bot) {
			this.owner = bot;
		}

		public void Initialize() {
			this.gui = this.owner._guiApi;
		}

		public void RenderGraph(Rectangle drawRegion) {
			if (!this.owner.UseNeat || this.gui == null) {
				return;
			}
			foreach (ConnectionGene c in this.Connections) {
				if (!c.Enabled) {
					continue;
				}
				// Connections X and Y positions are already normalized.
				int xIn = (int) Math.Floor(c.In.X * drawRegion.Width) + drawRegion.X;
				int yIn = (int) Math.Floor(c.In.Y * drawRegion.Height) + drawRegion.Y;
				int xOut = (int) Math.Floor(c.Out.X * drawRegion.Width) + drawRegion.X;
				int yOut = (int) Math.Floor(c.Out.Y * drawRegion.Height) + drawRegion.Y;
				this.gui.DrawLine(xIn, yIn, xOut, yOut, GetColorByWeight(c), DisplaySurfaceID.EmuCore);
			}
			foreach (NodeGene n in this.Nodes) {
				// Node X and Y positions are already normalized.
				int x = (int) Math.Floor(n.X * drawRegion.Width) + drawRegion.X;
				int y = (int) Math.Floor(n.Y * drawRegion.Height) + drawRegion.Y;
				// 3x3 box
				this.gui.DrawBox(x - 1, y - 1, x + 1, y + 1, outline, fill, DisplaySurfaceID.EmuCore);
			}
		}

		public void RenderInputRegion() {
			if (!this.owner.UseNeat || this.gui == null) {
				return;
			}
			int x = IsBotting ? this.owner._inputX : (int) this.owner.InputRegionX.Value;
			int y = IsBotting ? this.owner._inputY : (int) this.owner.InputRegionY.Value;
			int width = IsBotting ? this.owner._inputWidth : (int) this.owner.InputRegionWidth.Value;
			int height = IsBotting ? this.owner._inputHeight : (int) this.owner.InputRegionHeight.Value;
			this.gui.DrawBox(x, y, x + width, y + height, null, Color.FromArgb(72, Color.LightGray), DisplaySurfaceID.EmuCore);
		}

		// ================================================================================================
		// Private methods

		private Color GetColorByWeight(ConnectionGene c) {
			double[] green = Utils.RgbToHsv(Color.Green);
			double[] red = Utils.RgbToHsv(Color.DarkRed);
			double hue = Utils.Normalize(c.Weight, -1.0, 1.0, red[0], green[0]);
			double value = Utils.Normalize(c.Weight, -1.0, 1.0, red[2], green[2]);
			return (Color) Utils.HsvToRgb(hue, 1.0, value)!;
		}
	}
}

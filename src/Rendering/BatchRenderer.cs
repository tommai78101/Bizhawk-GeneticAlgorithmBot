using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.MSX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BizHawk.Common.XlibImports;

namespace GeneticAlgorithmBot.Rendering {
	public class BatchRenderer {
		private GeneticAlgorithmBot owner;
		private IGuiApi gui = null!;
		private readonly Color outline = Color.Black;
		private readonly Color fill = Color.White;

		public List<NodeGene> Nodes => this.owner.neat.AllNodes.GetData();
		public List<NodeGene> InputNodes => this.owner.neat.InputNodes;
		public List<NodeGene> OutputNodes => this.owner.neat.OutputNodes;
		public List<ConnectionGene> Connections => this.owner.neat.AllConnections.Keys.ToList();
		public bool IsBotting => this.owner._isBotting;

		public BatchRenderer(GeneticAlgorithmBot bot) {
			this.owner = bot;
		}

		public void Initialize() {
			this.gui = this.owner._guiApi;
			this.owner._inputX = (int) this.owner.InputRegionX.Value;
			this.owner._inputY = (int) this.owner.InputRegionY.Value;
			this.owner._inputWidth = (int) this.owner.InputRegionWidth.Value;
			this.owner._inputHeight = (int) this.owner.InputRegionHeight.Value;
		}

		public void RenderGraph() {
			if (!this.owner.UseNeat || this.gui == null) {
				return;
			}

			// Input region border
			int screenWidth = this.owner._clientApi.BufferWidth();
			int screenHeight = this.owner._clientApi.BufferHeight();
			const int sideSize = 128;
			const int paddingRightX = 10;
			const int paddingTopY = 10;
			Color inputBorderColor = Color.FromArgb(32, Color.Yellow);
			Color inputRegionColor = Color.FromArgb(32, Color.DarkGray);
			Color inputColor = Color.FromArgb(32, Color.AliceBlue);
			int radius = this.owner._inputSampleSize / 2;
			int zoom = screenWidth / this.owner._inputWidth;

			// Get the draw regions
			Rectangle drawRegion = new Rectangle(screenWidth - paddingRightX - sideSize, paddingTopY, sideSize, sideSize);
			this.gui.DrawBox(drawRegion.Left, drawRegion.Top, drawRegion.Right, drawRegion.Bottom, inputBorderColor, null, DisplaySurfaceID.EmuCore);

			// Input region to input nodes lines
			for (int i = 0; i < this.owner._neatInputRegionData.Length; i++) {
				// From
				ExtendedColorWrapper w = this.owner._neatInputRegionData[i];
				int x1 = (w.X * this.owner._inputSampleSize) + radius + this.owner._inputX;
				int y1 = (w.Y * this.owner._inputSampleSize) + radius + this.owner._inputY;
				this.gui.DrawBox(x1 - radius, y1 - radius, x1 + radius, y1 + radius, null, inputRegionColor, DisplaySurfaceID.EmuCore);
				// To
				NodeGene n = InputNodes[i];
				int x2 = (int) Math.Floor(n.X * drawRegion.Width) + drawRegion.X;
				int y2 = (int) Math.Floor(n.Y * drawRegion.Height) + drawRegion.Y;
				this.gui.DrawLine(x1, y1, x2, y2, inputColor, DisplaySurfaceID.EmuCore);
			}

			// Connection lines
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

			// Nodes in the graph
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
			Color inputOutlineColor = Color.FromArgb(72, Color.Yellow);
			Color inputRegionColor = Color.FromArgb(32, Color.Yellow);
			int x = (int) this.owner.InputRegionX.Value;
			int y = (int) this.owner.InputRegionY.Value;
			int width = (int) this.owner.InputRegionWidth.Value;
			int height = (int) this.owner.InputRegionHeight.Value;
			this.gui.DrawBox(x, y, x + width, y + height, inputOutlineColor, inputRegionColor, DisplaySurfaceID.EmuCore);

			if (this.IsBotting) {
				Color blockOutlineColor = Color.FromArgb(48, Color.AliceBlue);
				int radius = this.owner._inputSampleSize / 2;
				foreach (ExtendedColorWrapper block in this.owner._neatInputRegionData) {
					Color blockColor = block.ExtendedColor.ToColor();
					int x1 = (block.X * this.owner._inputSampleSize) + radius + this.owner._inputX;
					int y1 = (block.Y * this.owner._inputSampleSize) + radius + this.owner._inputY;
					this.gui.DrawBox(x1 - radius, y1 - radius, x1 + radius, y1 + radius, blockOutlineColor, blockColor, DisplaySurfaceID.EmuCore);
				}
			}
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

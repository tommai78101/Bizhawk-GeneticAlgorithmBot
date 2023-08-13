using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	[StructLayout(LayoutKind.Explicit)]
	public struct ExtendedColor {
		[FieldOffset(0)]
		public byte R;
		[FieldOffset(1)]
		public byte G;
		[FieldOffset(2)]
		public byte B;

		[FieldOffset(0)]
		private double _Double;

		// Helper constant
		private const int N_MINUS_1 = 256 * 256 * 256 - 1;

		// Precomputed raw range length
		private static readonly double RAW_RANGE_LENGTH = double.Epsilon * N_MINUS_1;

		public double Double {
			get {
				return this._Double / RAW_RANGE_LENGTH;
			}
			set {
				this._Double = value * RAW_RANGE_LENGTH;
			}
		}

		public ExtendedColor(Color color) {
			this._Double = 0.0;
			this.R = color.R;
			this.G = color.G;
			this.B = color.B;
		}

		public void SetColor(Color color) {
			this._Double = 0.0;
			this.B = color.B;
			this.G = color.G;
			this.R = color.R;
		}

		public Color ToColor() {
			return Color.FromArgb(R, G, B);
		}
	}

	public class ExtendedColorWrapper {
		private ExtendedColor _color = new ExtendedColor(Color.Transparent);
		public int X { get; set; } = 0;
		public int Y { get; set; } = 0;
		public int Radius { get; set; } = 1;

		public ExtendedColor ExtendedColor => this._color;

		public ExtendedColorWrapper(ExtendedColor color) {
			this._color = color;
			this.Radius = 0;
		}

		public ExtendedColorWrapper(Color color) {
			this._color.SetColor(color);
			this.Radius = 0;
		}

		public static ExtendedColorWrapper[] Initialize(int xOffset, int yOffset, int desiredWidth, int desiredHeight) {
			ExtendedColorWrapper[] output = new ExtendedColorWrapper[desiredWidth * desiredHeight];
			for (int i = 0; i < output.Length; i++) {
				int x = (i + xOffset) % desiredWidth;
				int y = (i + yOffset) / desiredWidth;
				output[i] = new ExtendedColorWrapper(Color.Transparent);
				output[i].X = x;
				output[i].Y = y;
				output[i].Radius = 0;
			}
			return output;
		}
	}
}

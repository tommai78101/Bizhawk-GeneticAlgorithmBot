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
			this.R = color.R;
			this.G = color.G;
			this.B = color.B;
			this._Double = 0.0;
		}

		public ExtendedColor FromInt32Argb(int value) {
			return FromColor(Color.FromArgb(value));
		}

		public ExtendedColor FromColor(Color color) {
			int pixelColor = color.ToArgb();
			ExtendedColor extendedColor = new ExtendedColor();
			extendedColor.R = (byte) ((pixelColor << 8) >> 24);
			extendedColor.G = (byte) ((pixelColor << 16) >> 24);
			extendedColor.B = (byte) ((pixelColor << 24) >> 24);
			return extendedColor;
		}

		public Color ToColor() {
			return Color.FromArgb(R, G, B);
		}

		public int ToPixel() {
			return (0xFF << 24) | (R << 16) | (G << 8) | B;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public static class Utils {
		public static Random RNG { get; } = new Random((int) DateTime.Now.Ticks);

		public static readonly double CROSSOVER_RATE = 50.0;

		public static bool IsAttemptBetter(GeneticAlgorithmBot bot, BotAttempt best, BotAttempt comparison, BotAttempt current) {
			int max = bot.MainValueRadio.Checked ? comparison.Maximize : best.Maximize;
			if (!TestValue(bot.MainComparisonType, current.Maximize, max)) return false;
			if (current.Maximize != comparison.Maximize) return true;

			int tie1 = bot.TieBreak1ValueRadio.Checked ? comparison.TieBreak1 : best.TieBreak1;
			if (!TestValue(bot.Tie1ComparisonType, current.TieBreak1, tie1)) return false;
			if (current.TieBreak1 != comparison.TieBreak1) return true;

			int tie2 = bot.TieBreak2ValueRadio.Checked ? comparison.TieBreak2 : best.TieBreak2;
			if (!TestValue(bot.Tie2ComparisonType, current.TieBreak2, tie2)) return false;
			if (current.TieBreak2 != comparison.TieBreak2) return true;

			int tie3 = bot.TieBreak3ValueRadio.Checked ? comparison.TieBreak3 : best.TieBreak3;
			if (!TestValue(bot.Tie3ComparisonType, current.TieBreak3, tie3)) return false;

			// TieBreak3 is equal, regardless of which attempt type they are.
			return true;
		}

		public static bool TestValue(byte operation, long currentValue, long bestValue)
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

		public static BotData BotDataReflectionCopy(object source) {
			BotData target = (BotData) Activator.CreateInstance(typeof(BotData));
			foreach (PropertyInfo p in source.GetType().GetProperties()) {
				if (p.Name.Equals("Best")) {
					BotAttempt attempt = Utils.BotAttemptReflectionCopy(p.GetValue(source));
					target.Best = new BotAttempt(attempt);
				}
				else if (p.Name.Equals("ControlProbabilities")) {
					Dictionary<string, double> sourceDict = (Dictionary<string, double>) p.GetValue(source);
					target.ControlProbabilities = new Dictionary<string, double>(sourceDict);
				}
				else if (p.Name.Equals("Attempts")) {
					target.GetType().GetProperty("Runs")!.SetValue(target, p.GetValue(source));
				}
				else {
					target.GetType().GetProperty(p.Name)!.SetValue(target, p.GetValue(source));
				}
			}
			return target;
		}

		public static BotAttempt BotAttemptReflectionCopy(object source) {
			BotAttempt target = (BotAttempt) Activator.CreateInstance(typeof(BotAttempt));
			foreach (PropertyInfo p in source.GetType().GetProperties()) {
				object value = p.GetValue(source);
				PropertyInfo targetInfo = typeof(BotAttempt).GetProperty(p.Name);
				if (value.GetType() == typeof(int)) {
					targetInfo!.SetValue(target, Convert.ToInt32(value));
				}
				else if (p.Name.Equals("Log")) {
					List<string> logs = (List<string>) targetInfo.GetValue(target);
					logs.Clear();
					logs.AddRange((List<string>) p.GetValue(source));
				}
				else if (p.Name.Equals("is_Reset")) {
					typeof(BotAttempt).GetProperty("isReset")!.SetValue(target, p.GetValue(source));
				}
				else {
					typeof(BotAttempt).GetProperty(p.Name)!.SetValue(target, p.GetValue(source));
				}
			}
			return target;
		}

		/// <summary>
		/// <para>Takes in a color and converts it into HSV values. </para>
		/// <a href="http://www.easyrgb.com/en/math.php">Source</a>.
		/// </summary>
		/// <param name="color" />
		/// <returns>An array of doubles containing the hue, saturation, and value, respectively.</returns>
		public static double[] RgbToHsv(Color color) {
			double R = color.R / 255.0;
			double G = color.G / 255.0;
			double B = color.B / 255.0;
			double max = Math.Max(R, Math.Max(G, B));
			double min = Math.Min(R, Math.Min(G, B));
			double delta = max - min;

			double hue = color.GetHue(), saturation, value = max;
			if (Math.Abs(delta) < Double.Epsilon) {
				hue = 0.0;
				saturation = 0.0;
			}
			else {
				double deltaR = (((max - R) / 6) + (max / 2)) / max;
				double deltaG = (((max - G) / 6) + (max / 2)) / max;
				double deltaB = (((max - B) / 6) + (max / 2)) / max;
				if (Math.Abs(R - max) < Double.Epsilon) {
					hue = deltaB - deltaG;
				}
				else if (Math.Abs(G - max) < Double.Epsilon) {
					hue = (1.0 / 3.0) + deltaR - deltaB;
				}
				else if (Math.Abs(B - max) < Double.Epsilon) {
					hue = (2.0 / 3.0) + deltaG - deltaR;
				}
				if (hue < 0.0) {
					hue += 1.0;
				}
				if (hue > 1.0) {
					hue -= 1.0;
				}
				saturation = delta / max;
			}
			return new double[3] { hue, saturation, value };
		}

		/// <summary>
		/// <para>Takes in the HSV values and converts it into a color. </para>
		/// <a href="http://www.easyrgb.com/en/math.php">Source</a>.
		/// </summary>
		/// <param name="hue" />
		/// <param name="saturation" />
		/// <param name="value" />
		/// <returns>A color of type System.Drawing.Color.</returns>
		public static Color HsvToRgb(double hue, double saturation, double value) {
			int R, G, B;
			if (saturation < Double.Epsilon) {
				R = (int) (value * 255.0);
				G = (int) (value * 255.0);
				B = (int) (value * 255.0);
			}
			else {
				hue *= 6.0;
				if (Math.Abs(hue - 6) < Double.Epsilon) {
					// Hue must be less than 1.0.
					hue = 0.0;
				}
				double hueInt = (int) hue;
				double x = value * (1.0 - saturation);
				double y = value * (1.0 - saturation * (hue - hueInt));
				double z = value * (1.0 - saturation * (1.0 - (hue - hueInt)));

				// HSV matrix
#pragma warning disable format // @formatter:off
				double r, g, b;
				if			(hueInt == 0)	{ r = value	; g = z		; b = x		; }
				else if		(hueInt == 1)	{ r = y		; g = value	; b = x		; }
				else if		(hueInt == 2)	{ r = x		; g = value	; b = z		; }
				else if		(hueInt == 3)	{ r = x		; g = y		; b = value	; }
				else if		(hueInt == 4)	{ r = z		; g = x		; b = value	; }
				else						{ r = value	; g = x		; b = y		; }
#pragma warning restore format // @formatter:on

				R = (int) (r * 255.0);
				G = (int) (g * 255.0);
				B = (int) (b * 255.0);
			}
			return Color.FromArgb(R, G, B);
		}

		public static Color? HsvToRgb(double[] hsv) {
			if (hsv == null || hsv.Length != 3) {
				// Programming error color.
				return null;
			}
			return HsvToRgb(hsv[0], hsv[1], hsv[2]);
		}

		public static double Normalize(double value, double minValue, double maxValue, double min, double max) {
			return (((value - minValue) / (maxValue - minValue)) * (max - min)) + min;
		}
	}
}

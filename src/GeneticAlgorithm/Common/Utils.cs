using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public static class Utils {
		public static Random RNG { get; } = new Random((int) DateTime.Now.Ticks);

		public static readonly double CROSSOVER_RATE = 50.0;

		public static bool IsGeneticBetter(GeneticAlgorithmBot bot, BotAttempt best, BotAttempt comparison, BotAttempt current) {
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
					targetInfo!.SetValue(target, Convert.ToUInt32(value));
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
	}
}

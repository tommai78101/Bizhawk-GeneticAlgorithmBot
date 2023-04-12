using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class BotAttempt {
		public long Attempt { get; set; }
		public long Generation { get; set; }
		public int Fitness { get; set; }
		public int Maximize { get; set; }
		public int TieBreak1 { get; set; }
		public int TieBreak2 { get; set; }
		public int TieBreak3 { get; set; }
		public byte ComparisonTypeMain { get; set; }
		public byte ComparisonTypeTie1 { get; set; }
		public byte ComparisonTypeTie2 { get; set; }
		public byte ComparisonTypeTie3 { get; set; }
		public List<string> Log { get; } = new List<string>();
		public bool isReset { get; set; } = true;

		public BotAttempt() { }

		public BotAttempt(BotAttempt source) {
			this.Attempt = source.Attempt;
			this.Fitness = source.Fitness;
			this.Generation = source.Generation;
			this.Maximize = source.Maximize;
			this.TieBreak1 = source.TieBreak1;
			this.TieBreak2 = source.TieBreak2;
			this.TieBreak3 = source.TieBreak3;
			this.ComparisonTypeMain = source.ComparisonTypeMain;
			this.ComparisonTypeTie1 = source.ComparisonTypeTie1;
			this.ComparisonTypeTie2 = source.ComparisonTypeTie2;
			this.ComparisonTypeTie3 = source.ComparisonTypeTie3;
			this.isReset = source.isReset;
			this.Log = new List<string>();
			this.Log.AddRange(source.Log);
		}
	}
}

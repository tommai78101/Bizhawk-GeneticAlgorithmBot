using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public struct BotData {
		public BotAttempt Best { get; set; }
		public Dictionary<string, double> ControlProbabilities { get; set; }
		public ulong? Maximize { get; set; }
		public ulong? TieBreaker1 { get; set; }
		public ulong? TieBreaker2 { get; set; }
		public ulong? TieBreaker3 { get; set; }
		public byte ComparisonTypeMain { get; set; }
		public byte ComparisonTypeTie1 { get; set; }
		public byte ComparisonTypeTie2 { get; set; }
		public byte ComparisonTypeTie3 { get; set; }
		public bool MainCompareToBest { get; set; }
		public bool TieBreaker1CompareToBest { get; set; }
		public bool TieBreaker2CompareToBest { get; set; }
		public bool TieBreaker3CompareToBest { get; set; }
		public int MainCompareToValue { get; set; }
		public int TieBreaker1CompareToValue { get; set; }
		public int TieBreaker2CompareToValue { get; set; }
		public int TieBreaker3CompareToValue { get; set; }
		public int FrameLength { get; set; }
		public string FromSlot { get; set; }
		public long Runs { get; set; }
		public long Frames { get; set; }
		public long Generations { get; set; }
		public string MemoryDomain { get; set; }
		public bool BigEndian { get; set; }
		public int DataSize { get; set; }
		public string HawkVersion { get; set; }
		public string SysID { get; set; }
		public string CoreName { get; set; }
		public string GameName { get; set; }
	}
}

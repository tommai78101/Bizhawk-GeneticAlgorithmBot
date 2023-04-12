using BizHawk.Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	// Temporary class to store all of the settings (class member properties) marked with [ConfigPersist] attribute.
	public class GeneticAlgorithmBotSettings {
		public RecentFiles RecentBotFiles { get; set; } = new RecentFiles();
		public bool TurboWhenBotting { get; set; } = true;
		public bool InvisibleEmulation { get; set; } = true;
	}
}

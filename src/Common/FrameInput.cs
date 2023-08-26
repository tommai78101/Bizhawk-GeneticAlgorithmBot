using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class FrameInput {
		public HashSet<string> Buttons { get; set; }
		public int FrameNumber { get; set; }

		public FrameInput(int frameNumber) {
			this.Buttons = new HashSet<string>();
			FrameNumber = frameNumber;
		}

		public void Clear() {
			this.Buttons.Clear();
		}

		public void Pressed(string button) {
			this.Buttons.Add(button);
		}

		public void Released(string button) {
			this.Buttons.Remove(button);
		}

		public bool IsPressed(string button) {
			return this.Buttons.Contains(button);
		}

		public void DeepCopy(FrameInput other) {
			this.Buttons.UnionWith(other.Buttons);
			this.FrameNumber = other.FrameNumber;
		}
	}
}

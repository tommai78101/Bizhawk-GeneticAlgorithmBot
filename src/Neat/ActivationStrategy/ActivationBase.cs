using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public abstract class ActivationBase {
		public IActivationFunction Activation { get; set; }
		public string Name { get; set; }

		public ActivationBase(IActivationFunction activation, string name) {
			this.Activation = activation;
			this.Name = name;
		}

		public static IEnumerable<T> GetAll<T>() where T : ActivationBase {
			var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
			return fields.Select(f => f.GetValue(null)).Cast<T>();
		}
	}
}

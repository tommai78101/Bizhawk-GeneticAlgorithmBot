using GeneticAlgorithmBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class Species : IComparable<Species> {
		public RandomList<Client> Clients { get; }
		public Client Representative { get; private set; }
		public double Score { get; private set; }
		public int Count => Clients.Count;

		public Species(Client representative) {
			Clients = new RandomList<Client>();
			this.Representative = representative;
			this.Representative.Species = this;
			Clients.Add(representative);
		}

		// Affect a Client to a Species regarding its genetical distance
		public bool Put(Client client) {
			if (client.GetDistance(Representative!) < Constants.CP_GENETICALLY_DISTANT) {
				client.Species = this;
				Clients.Add(client);
				return true;
			}
			return false;
		}

		public void ForcePut(Client client) {
			client.Species = this;
			Clients.Add(client);
		}

		public void Extinguish() {
			foreach(Client c in Clients) {
				c.Species = null!;
			}
		}

		public void EvaluateScore() {
			// Default scoring method:
			// Score = Clients.Sum(d => d.Score) / Clients.Count;
			Score = Clients.Sum(c => c.Score);
		}

		public void Reset() {
			Representative = Clients.GetRandomElement()!;
			foreach (Client c in Clients) {
				c.Species = null!;
			}
			Clients.Clear();

			Clients.Add(Representative!);
			Representative!.Species = this;
			Score = 0;
		}

		public void Kill(double percentage) {
			Clients.Sort((Client c1, Client c2) => c1.CompareTo(c2));
			double amount = percentage * Clients.Count;
			for (int i = 0; i < amount; i++) {
				Client c = Clients[Clients.Count - 1]!;
				c.Species = null!;
				Clients.Remove(c);
			}
		}

		public IGenome Breed() {
			Client c1 = Clients.GetRandomElement()!;
			Client c2 = Clients.GetRandomElement()!;
			if (c1!.Score > c2!.Score) {
				return c1.Genome.Crossover(c2.Genome);
			}
			return c2.Genome.Crossover(c1.Genome);
		}

		public int CompareTo(Species other) {
			if (Score > other.Score)
				return -1;
			if (Score < other.Score) 
				return 1;
			return 0;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmBot {
	public class RandomList<T> : IEnumerable<T> {
		private List<T> Data { get; }
		private static readonly Random random = new Random();

		public int Count => Data.Count;

		public bool Contains(T obj) => Data.Contains(obj);

		public RandomList() {
			Data = new List<T>();
		}

		public List<T> GetData() {
			return this.Data;
		}

		public T GetRandomElement() {
			if (Data.Any()) {
				return Data[ThreadSafeRandom.GetNext(Data.Count)];
			}
			return default!;
		}

		public void Add(T obj) {
			if (!Data.Contains(obj)) {
				Data.Add(obj);
			}
		}

		public void Insert(int index, T obj) {
			Data.Insert(index, obj);
		}

		public void Clear() {
			Data.Clear();
		}

		public void RemoveAt(int i) => Data.RemoveAt(i);

		public void Remove(T obj) => Data.Remove(obj);

		public IEnumerator<T> GetEnumerator() {
			return ((IEnumerable<T>) Data).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable) Data).GetEnumerator();
		}

		public void Sort(Comparison<T> comparison) {
			Data.Sort(comparison);
		}

		public T this[int index] {
			get {
				if (index >= 0 && index < Count)
					return Data[index];
				return default!;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core {
    internal static class EnumerableExtensions {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source) {
            return !source?.Any() ?? true;
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) {
            return source.MaxBy(selector, null);
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
            return source.MostBy(selector, comparer, true);
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) {
            return source.MinBy(selector, null);
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
            return source.MostBy(selector, comparer, false);
        }

        private static TSource MostBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer, bool max) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (selector == null) {
                throw new ArgumentNullException("selector");
            }
            comparer ??= Comparer<TKey>.Default;
            var       factor         = max ? -1 : 1;
            using var sourceIterator = source.GetEnumerator();
            if (!sourceIterator.MoveNext()) {
                throw new InvalidOperationException("Sequence contains no elements");
            }
            var most    = sourceIterator.Current;
            var mostKey = selector(most);
            while (sourceIterator.MoveNext()) {
                var candidate          = sourceIterator.Current;
                var candidateProjected = selector(candidate);
                if (comparer.Compare(candidateProjected, mostKey) * factor >= 0) continue;
                most    = candidate;
                mostKey = candidateProjected;
            }
            var result = most;
            return result;
        }

        public static void Move<T>(this IList<T> list, int oldIndex, int newIndex) {
            if (oldIndex == newIndex || 0 > oldIndex || oldIndex >= list.Count || 0 > newIndex ||
                newIndex >= list.Count) {
                return;
            }

            int i;
            var tmp = list[oldIndex];
            if (oldIndex < newIndex) {
                // Nove element down and shift other elements up.
                for (i = oldIndex; i < newIndex; i++) {
                    list[i] = list[i + 1];
                }
            } else {
                // Move element up and shift other elements down.
                for (i = oldIndex; i > newIndex; i--) {
                    list[i] = list[i - 1];
                }
            }
            // Put element from position 1 to destination.
            list[newIndex] = tmp;
        }
    }
}

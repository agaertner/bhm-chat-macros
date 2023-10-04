using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core {
    internal static class FastenshteinUtil {
        /// <summary>
        /// Finds the closest matching string in the list.
        /// </summary>
        /// <param name="needle">the string to find a closest match for.</param>
        /// <param name="list">the list to search in.</param>
        /// <returns>Closest matching string.</returns>
        public static string FindClosestMatch(string needle, params string[] list) {
            return FindClosestMatchBy(needle, list, str => new List<string>{str});
        }

        /// <summary>
        /// Finds the closest matching object using an expression.
        /// </summary>
        /// <param name="needle">the string to find a closest match for.</param>
        /// <param name="list">the list of objects to compare.</param>
        /// <returns>Closest matching object.</returns>
        public static T FindClosestMatchBy<T>(string needle, IEnumerable<T> list, Func<T, IEnumerable<string>> expression) {
            if (string.IsNullOrWhiteSpace(needle) || needle.Length == 0) {
                return default;
            }

            T result = default;

            //The value here is a purely nonsensical high value and serves no other purpose
            int minScore = 20000;

            var lev = new Fastenshtein.Levenshtein(needle);

            foreach (var item in list) {
                var property = expression(item)?.ToList();

                if (!property?.Any() ?? true) {
                    continue;
                }

                foreach (var str in property) {
                    int score = lev.DistanceFrom(str);
                    if (score < minScore) {
                        minScore = score;
                        result   = item;
                    }
                }
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core {
    internal static class FastenshteinUtil {

        public static IEnumerable<T> FindMatchesBy<T>(string needle, IEnumerable<T> list, Func<T, string> expression) {
            return FindMatchesBy(needle, list, item => new List<string> {expression(item)});
        }

        public static IEnumerable<T> FindMatchesBy<T>(string needle, IEnumerable<T> list, Func<T, IEnumerable<string>> expression) {
            if (string.IsNullOrWhiteSpace(needle) || list == null) {
                return Enumerable.Empty<T>();
            }
            return list.Where(o => expression(o)?.Any(str => str?.ToLowerInvariant().Contains(needle.ToLowerInvariant()) ?? false) ?? false);
        }

        /// <summary>
        /// Finds the closest matching string in the list.
        /// </summary>
        /// <param name="needle">the string to find a closest match for.</param>
        /// <param name="ignoreCase">If case should be ignored.</param>
        /// <param name="list">the list to search in.</param>
        /// <returns>Closest matching string.</returns>
        public static string FindClosestMatch(string needle, bool ignoreCase = true, params string[] list) {
            return FindClosestMatchBy(needle, list, str => new List<string>{str}, ignoreCase);
        }

        public static T FindClosestMatchBy<T>(string needle, IEnumerable<T> list, Func<T, string> expression, bool ignoreCase = true) {
            return FindClosestMatchBy(needle, list, item => new List<string> { expression(item) }, ignoreCase);
        }

        /// <summary>
        /// Finds the closest matching object using an expression.
        /// </summary>
        /// <param name="needle">the string to find a closest match for.</param>
        /// <param name="list">the list of objects to compare.</param>
        /// <param name="expression">Expression that takes an object from the list and returns a string.</param>
        /// <param name="ignoreCase">If case should be ignored.</param>
        /// <returns>Closest matching object.</returns>
        public static T FindClosestMatchBy<T>(string needle, IEnumerable<T> list, Func<T, IEnumerable<string>> expression, bool ignoreCase = true) {
            if (string.IsNullOrWhiteSpace(needle) || needle.Length == 0) {
                return default;
            }

            list = FindMatchesBy(needle, list, expression);

            needle = ignoreCase ? needle.ToLowerInvariant() : needle;

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
                    var testStr = ignoreCase ? str.ToLowerInvariant() : str;
                    if (IsCloser(lev, testStr, ref minScore)) {
                        result = item;
                    }
                }
            }

            return result;
        }

        private static bool IsCloser(Fastenshtein.Levenshtein lev, string test, ref int minScore) {
            int score = lev.DistanceFrom(test);
            if (score < minScore) {
                minScore = score;
                return true;
            }
            return false;
        }
    }
}

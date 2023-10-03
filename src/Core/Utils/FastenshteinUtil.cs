using System.Linq;

namespace Nekres.ChatMacros.Core {
    internal static class FastenshteinUtil {
        /// <summary>
        /// Finds the closest matching string in the list.
        /// </summary>
        /// <param name="needle">the string to find a closest match for.</param>
        /// <param name="list">the list to search in.</param>
        /// <returns></returns>
        public static string FindClosestMatch(string needle, params string[] list) {
            if (string.IsNullOrWhiteSpace(needle) || needle.Length == 0) {
                return string.Empty;
            }

            if (!list?.Any() ?? true) {
                return string.Empty;
            }

            var result = string.Empty;

            //The value here is a purely nonsensical high value and serves no other purpose
            int minScore = 20000;

            var lev = new Fastenshtein.Levenshtein(needle);

            foreach (var element in list) {
                int score = lev.DistanceFrom(element);
                if (score < minScore) {
                    minScore = score;
                    result   = element;
                }
            }
            return result;
        }
    }
}

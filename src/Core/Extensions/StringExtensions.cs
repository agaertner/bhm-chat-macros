using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Nekres.ChatMacros.Core {
    internal static class StringExtensions {
        public static string SplitCamelCase(this string input) {
            return Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }

        public static IEnumerable<string> Split(this string input, string delimiter) {
            return input.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static IEnumerable<string> ReadLines(this string input) {
            using var sr = new StringReader(input);
            while (sr.ReadLine() is { } line) {
                yield return line;
            }
        }

        public static string GetTextBetweenTags(this string input, string tagName) {
            var match = Regex.Match(input, $"<{tagName}>(.*?)</{tagName}>");
            return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : string.Empty;
        }

        public static bool IsWebLink(this string uri) {
            return Uri.TryCreate(uri, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static bool IsPathFullyQualified(this string path) {
            var root = Path.GetPathRoot(path);
            return root.StartsWith(@"\\") || root.EndsWith(@"\") && root != @"\";
        }

        public static string TrimStart(this string input, int count) {
            if (string.IsNullOrEmpty(input)) {
                return input;
            }
            while (count > 0 && input.Length > 0 && input.StartsWith(" ")) {
                input = input.Substring(1);
                --count;
            }
            return input;
        }

        public static string Replace(this string text, string search, string replace, int count, bool ignoreCase = true) {
            if (string.IsNullOrEmpty(text)) {
                return text;
            }

            var comparison = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            var pos = text.IndexOf(search, comparison);

            while (pos > -1 && count > 0)
            {
                text = text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
                pos  = text.IndexOf(search, comparison);
                --count;
            }
            return text;
        }
    }
}

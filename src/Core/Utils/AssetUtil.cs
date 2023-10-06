using Blish_HUD;
using Blish_HUD.Extended;
using Gw2Sharp.WebApi;
using MonoGame.Extended.BitmapFonts;
using System.IO;

namespace Nekres.ChatMacros {
    public static class AssetUtil {

        private const char ELLIPSIS      = '\u2026';
        private const char BRACKET_LEFT  = '[';
        private const char BRACKET_RIGHT = ']';
        private const string WIKI_SEARCH = "https://wiki-{0}.guildwars2.com?search={1}";

        public static int GetId(string assetUri) {
            return int.Parse(Path.GetFileNameWithoutExtension(assetUri));
        }

        public static string GetItemDisplayName(string name, int quantity, bool brackets = true) {
            if (quantity == 1) {
                return brackets ? $"{BRACKET_LEFT}{name}{BRACKET_RIGHT}" : name;
            }
            return brackets ? $"{BRACKET_LEFT}{quantity} {name}{BRACKET_RIGHT}" : $"{quantity} {name}";
        }

        public static string Truncate(string text, int maxWidth, BitmapFont font) {
            if (string.IsNullOrEmpty(text)) {
                return text;
            }
            var result = text;
            var width  = (int)font.MeasureString(result).Width;
            while (width > maxWidth) {
                result = result.Substring(0, result.Length - 1);
                width  = (int)font.MeasureString(result).Width;
            }
            return result.Length < text.Length ? result.TrimEnd() + ELLIPSIS : result;
        }

        public static string GetWikiLink(string wikiPage) {
            switch (GameService.Overlay.UserLocale.Value) {
                case Locale.English:
                case Locale.Spanish:
                case Locale.German:
                case Locale.French:
                    return string.Format(WIKI_SEARCH, GameService.Overlay.UserLocale.Value.Code(), wikiPage);
                case Locale.Korean:
                case Locale.Chinese:
                default: return string.Format(WIKI_SEARCH, Locale.English.Code(), wikiPage);
            }
        }
    }
}

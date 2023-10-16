using Gw2Sharp.WebApi;
using System.Globalization;

namespace Nekres.ChatMacros.Core {
    internal static class LocaleExtensions {
        public static CultureInfo GetCulture(this Locale locale) {
            switch (locale) {
                case Locale.English:
                    return CultureInfo.GetCultureInfo(9);
                case Locale.Spanish:
                    return CultureInfo.GetCultureInfo(10);
                case Locale.German:
                    return CultureInfo.GetCultureInfo(7);
                case Locale.French:
                    return CultureInfo.GetCultureInfo(12);
                case Locale.Korean:
                    return CultureInfo.GetCultureInfo(18);
                case Locale.Chinese:
                    return CultureInfo.GetCultureInfo(30724);
                default:
                    return CultureInfo.GetCultureInfo(9);
            }
        }
    }
}

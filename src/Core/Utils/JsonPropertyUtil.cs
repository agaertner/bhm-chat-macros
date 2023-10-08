using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Nekres.ChatMacros.Core {
    internal static class JsonPropertyUtil {
        public static string GetPropertyFromJson(string json, string propertyPath) {
            try {
                var token = JToken.Parse(json);
                string[] pathSegments = propertyPath.Split('.');

                foreach (string segment in pathSegments) {

                    // Handle JArray
                    if (int.TryParse(segment, out int index) && token is JArray array) {
                        if (index >= 0 && index < array.Count) {
                            token = array[index];
                            continue;
                        }
                        return string.Empty; // Index out of range
                    }

                    // Handle JObject
                    if (token is JObject obj) {
                        if (obj.TryGetValue(segment, out token)) {
                            continue;
                        }
                    }

                    // Segment doesn't exist.
                    return string.Empty;
                }
                if (token is JValue jValue) {
                    return jValue.ToString(CultureInfo.InvariantCulture);
                }
                return string.Empty;
            } catch (JsonReaderException) {
                // Invalid json<y
                return string.Empty;
            }
        }
    }
}

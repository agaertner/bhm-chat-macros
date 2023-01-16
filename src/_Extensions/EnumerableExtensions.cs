using System.Collections.Generic;
using System.Linq;

namespace Nekres.Chat_Shorts {
    internal static class EnumerableExtensions {
        public static bool TryGet<T>(this IEnumerable<T> enumerable, int index, out T item) {
            item = default;

            if (index < 0) {
                return false;
            }

            T[] arr = enumerable.ToArray();
            int len = arr.Length;

            if (len == 0 || index >= len) {
                return false;
            }

            item = arr[index];

            return true;
        }
    }
}

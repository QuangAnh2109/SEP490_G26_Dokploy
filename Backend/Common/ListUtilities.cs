using System;
using System.Collections.Generic;

namespace Backend.Common
{
    public static class ListUtilities
    {
        // Xáo trộn danh sách phần tử
        public static void Shuffle<T>(this List<T> list, int? seed = null)
        {
            if (list == null || list.Count <= 1) return;

            Random rng = seed.HasValue ? new Random(seed.Value) : new Random();

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);

                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
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

            int currentIndex = list.Count;
            while (currentIndex > 1)
            {
                currentIndex--;
                int randomIndex = rng.Next(currentIndex + 1);

                T value = list[randomIndex];
                list[randomIndex] = list[currentIndex];
                list[currentIndex] = value;
            }
        }
    }
}
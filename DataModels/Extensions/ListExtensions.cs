using System;
using System.Collections.Generic;
using System.Linq;

namespace DataModels.Extensions
{
    /// <summary>
    /// Расширения для перечислений. 
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Разделяет список на указанное количество списков. 
        /// </summary>
        /// <typeparam name="T">Элемент коллекции.
        /// </typeparam><param name="listToSplit">Список для разделения на списки.</param>
        /// <param name="splitCount">Количество списков.</param>
        /// <returns>
        /// Возвращает список списков.
        /// </returns>
        public static List<List<T>> SplitListByCount<T>(this IList<T> listToSplit, int splitCount)
        {
            int splitLength = (int)Math.Ceiling(listToSplit.Count / (double)splitCount);
            return SplitList(listToSplit, splitCount, splitLength);
        }

        /// <summary>
        /// Разделяет список на указанное количество списков. 
        /// </summary>
        /// <typeparam name="T">Элемент коллекции.</typeparam>
        /// <param name="listToSplit">Список для разделения на списки.</param>
        /// <param name="splitLength">Количество элементов в списке.</param>
        /// <returns>
        /// Возвращает список списков.
        /// </returns>
        public static List<List<T>> SplitListByLength<T>(this IList<T> listToSplit, int splitLength)
        {
            int splitCount = (int)Math.Ceiling(listToSplit.Count / (double)splitLength);
            return listToSplit.SplitList(splitCount, splitLength);
        }

        /// <summary>
        /// Разделяет список на указанное количество списков. 
        /// </summary>
        /// <typeparam name="T">Элемент коллекции.</typeparam>
        /// <param name="listToSplit">Список для разделения на списки.</param>
        /// <param name="splitCount">Количество списков.</param>
        /// <param name="splitLength">Количество элементов в списке.</param>
        /// <returns>
        /// Возвращает список списков.
        /// </returns>
        public static List<List<T>> SplitList<T>(this IEnumerable<T> listToSplit, int splitCount, int splitLength)
        {
            return Enumerable.Range(0, splitCount).Select(i => listToSplit.Skip(i * splitLength).Take(splitLength).ToList()).Where(i => (uint)i.Count > 0U).ToList();
        }
    }
}

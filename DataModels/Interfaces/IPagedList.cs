using System.Collections.Generic;

namespace DataModels.Interfaces
{
    /// <summary>
    /// Интерфейс постраничного списка.
    /// </summary>
    /// <typeparam name="TModel">Тип элементов.</typeparam>
    public interface IPagedList<out TModel>
    {
        /// <summary>
        /// Перечисление элементов.
        /// </summary>
        IEnumerable<TModel> Items { get; }

        /// <summary>
        /// Общее количество элементов.
        /// </summary>
        int TotalItemsCount { get; }

        /// <summary>
        /// Количество элементов на страницу.
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Номер текущей страницы.
        /// </summary>
        int Page { get; }

        /// <summary>
        /// Количество страниц.
        /// </summary>
        int PagesCount { get; }

        /// <summary>
        /// Признак наличия следующей страницы.
        /// </summary>
        bool HasNext { get; }

        /// <summary>
        /// Признак наличия предыдущей страницы.
        /// </summary>
        bool HasPrevious { get; }
    }
}

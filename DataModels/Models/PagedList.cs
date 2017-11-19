using System;
using System.Collections.Generic;
using System.Linq;
using DataModels.Interfaces;

namespace DataModels.Models
{
    /// <summary>
    /// Класс представляет собой постраничный список элементов заданного типа.
    /// </summary>
    /// <typeparam name="TModel">Тип элементов.</typeparam>
    public sealed class PagedList<TModel> : IPagedList<TModel>
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PagedList{TModel}"/>.
        /// </summary>
        /// <param name="allItems">Все элементы перечисления.</param>
        /// <param name="page">Номер текущей страницы.</param>
        /// <param name="pageSize">Количество элементов на страницу.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public PagedList(IEnumerable<TModel> allItems, int page, int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Номер страницы должен быть больше нуля");
            }
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Количество элементов на страницу должно быть больше нуля");
            }

            this.PageSize = pageSize;
            this.Page = page;

            var arrayItems = allItems as TModel[] ?? allItems.ToArray();
            if (arrayItems.Length == 0)
            {
                this.Items = new TModel[0];
                this.PagesCount = 0;
                this.TotalItemsCount = 0;
                this.HasNext = false;
                this.HasPrevious = false;
                return;
            }

            this.Items = arrayItems.Skip((page - 1) * pageSize).Take(pageSize);
            this.TotalItemsCount = arrayItems.Length;
            this.PagesCount = this.TotalItemsCount/this.PageSize + 1;
            this.HasNext = this.PagesCount > this.Page;
            this.HasPrevious = this.Page > 1;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PagedList{TModel}"/> в одностраничном формате.
        /// </summary>
        /// <param name="allItems">Все элементы перечисления.</param>
        public PagedList(IEnumerable<TModel> allItems)
        {
            var arrayItems = allItems as TModel[] ?? allItems.ToArray();
            this.Page = 1;
            this.PagesCount = 1;
            this.HasNext = false;
            this.HasPrevious = false;
            this.PageSize = arrayItems.Length;
            if (arrayItems.Length == 0)
            {
                this.Items = new TModel[0];
                this.TotalItemsCount = 0;
                return;
            }

            this.Items = arrayItems;
            this.TotalItemsCount = arrayItems.Length;
        }

        /// <summary>
        /// Перечисление элементов.
        /// </summary>
        public IEnumerable<TModel> Items { get; }

        /// <summary>
        /// Общее количество элементов.
        /// </summary>
        public int TotalItemsCount { get; }

        /// <summary>
        /// Количество элементов на страницу.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Номер страницы.
        /// </summary>
        public int Page { get; }

        /// <summary>
        /// Количество страниц.
        /// </summary>
        public int PagesCount { get; }

        /// <summary>
        /// Признак наличия следующей страницы.
        /// </summary>
        public bool HasNext { get; }

        /// <summary>
        /// Признак наличия предыдущей страницы.
        /// </summary>
        public bool HasPrevious { get; }
    }
}

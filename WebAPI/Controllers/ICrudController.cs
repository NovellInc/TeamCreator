using System.Web.Http;
using DataModels.Interfaces;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Интерфейс CRUD модели контроллера.
    /// </summary>
    /// <typeparam name="TModel">Тип объектов.</typeparam>
    public interface ICrudController<in TModel>
    {
        /// <summary>
        /// Получает объекты согласно фильтру.
        /// </summary>
        /// <param name="model">Фильтр.</param>
        /// <returns></returns>
        IHttpActionResult Get([FromUri] TModel model);

        /// <summary>
        /// Получает объект по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор.</param>
        /// <returns></returns>
        IHttpActionResult Get([FromUri] string id);

        /// <summary>
        /// Создает объект.
        /// </summary>
        /// <param name="model">Объект.</param>
        /// <returns></returns>
        IHttpActionResult Create([FromBody] TModel model);

        /// <summary>
        /// Обновляет объект.
        /// </summary>
        /// <param name="model">Объект.</param>
        /// <returns></returns>
        IHttpActionResult Update([FromBody] TModel model);

        /// <summary>
        /// Удаляет объект по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="ownerId">Идентификатор создателя объекта.</param>
        /// <returns></returns>
        IHttpActionResult Delete(string id, [FromBody] string ownerId);
    }
}

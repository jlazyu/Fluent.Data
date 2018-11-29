using System.Reflection;

namespace Fluent.Data.MySql.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Convenience method that will load the <see cref="MySqlSession"/> type into the current domain.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static object UseFluentMySql(this object app)
        {
            Assembly.LoadFrom(typeof(MySqlSession).Assembly.Location);

            return app;
        }
    }
}

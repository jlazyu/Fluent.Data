using System.Reflection;

namespace Fluent.Data.SqlServer.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Convenience method that will load the <see cref="SqlServerSession"/> type into the current domain.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static object UseFluentSqlServer(this object app)
        {
            Assembly.LoadFrom(typeof(SqlServerSession).Assembly.Location);

            return app;
        }
    }
}

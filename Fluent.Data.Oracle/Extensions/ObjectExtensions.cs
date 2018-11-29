using System.Reflection;

namespace Fluent.Data.Oracle.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Convenience method that will load the <see cref="OracleSession"/> type into the current domain.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static object UseFluentOracle(this object app)
        {
            Assembly.LoadFrom(typeof(OracleSession).Assembly.Location);

            return app;
        }
    }
}

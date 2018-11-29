using Microsoft.Extensions.Configuration;

namespace Fluent.Data.Configuration.Core
{
    public static class ConnectionStringSettingsExtensions
    {
        /// <summary>
        /// Returns the <see cref="ConnectionStringSettings"/> with the name <paramref name="name"/>.  This is 
        /// retrieved using the given <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="name"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public static ConnectionStringSettings ConnectionString(this IConfiguration configuration, string name,
            string section = "ConnectionStrings")
        {
            var connectionStringCollection =
                configuration.GetSection(section)
                    .Get<ConnectionStringSettingsCollection>();

            if (connectionStringCollection == null ||
                !connectionStringCollection.TryGetValue(name, out var connectionStringSettings))
            {
                return null;
            }

            return connectionStringSettings;
        }
    }
}

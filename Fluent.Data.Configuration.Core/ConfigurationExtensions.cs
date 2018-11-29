using System;
using Microsoft.Extensions.Configuration;

namespace Fluent.Data.Configuration.Core
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Convenience method that will create a <see cref="FluentConnectionInformation"/> using the 
        /// given <paramref name="decryptString"/>.  The given <paramref name="configuration"/> func will
        /// be used to decrypt information on the connection string such as the password.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="connectionName"></param>
        /// <param name="decryptString">
        /// </param>
        /// <returns></returns>
        /// <exception cref="FluentConnectionInformation"></exception>
        public static FluentConnectionInformation GetConnectionInformation(this IConfiguration configuration,
            string connectionName, Func<string, string> decryptString)
        {
            if (configuration == null)
            {
                return null;
            }

            var connectionStringSettings = configuration.ConnectionString(connectionName) ??
                                           throw new FluentDatabaseSessionException(
                                               $"Unable to locate connection string settings with name {connectionName}.");

            return new FluentConnectionInformation
            {
                Name = connectionName,
                ConnectionString = connectionStringSettings.ConnectionString,
                ProviderName = connectionStringSettings.ProviderName,
                DecryptString = decryptString
            };
        }
    }
}

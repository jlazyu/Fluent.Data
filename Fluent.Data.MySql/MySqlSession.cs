using System.Data;
using Fluent.Data.Configuration;
using MySql.Data.MySqlClient;

namespace Fluent.Data.MySql
{
    [FluentDataProvider("MySql.Data.MySqlClient")]
    public class MySqlSession : DatabaseSession
    {
        protected override string GetConnectionString(FluentConnectionInformation connectionInformation)
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionInformation.ConnectionString);
            connectionStringBuilder.Password = connectionInformation.DecryptString(connectionStringBuilder.Password);

            return connectionStringBuilder.ConnectionString;
        }

        protected override string ParameterPrefix => "@";

        protected override void DecorateCommand(IDbCommand command)
        {

        }
    }
}
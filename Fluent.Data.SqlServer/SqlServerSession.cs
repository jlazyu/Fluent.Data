using System.Data;
using System.Data.SqlClient;
using Fluent.Data.Configuration;

namespace Fluent.Data.SqlServer
{
    [FluentDataProvider("System.Data.SqlClient")]
    public class SqlServerSession : DatabaseSession
    {
        protected override string GetConnectionString(FluentConnectionInformation connectionInformation)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionInformation.ConnectionString);
            connectionStringBuilder.Password = connectionInformation.DecryptString(connectionStringBuilder.Password);
            return connectionStringBuilder.ConnectionString;
        }

        protected override string ParameterPrefix => "@";

        protected override void DecorateCommand(IDbCommand command)
        {
            
        }
    }
}

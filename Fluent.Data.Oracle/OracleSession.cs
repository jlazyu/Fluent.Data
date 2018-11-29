using System;
using System.Data;
using System.Data.Common;
using Fluent.Data.Configuration;
using Orcl = Oracle.ManagedDataAccess.Client;

namespace Fluent.Data.Oracle
{
    [FluentDataProvider("Oracle.ManagedDataAccess.Client")]
    public class OracleSession : DatabaseSession
    {
        protected override string GetConnectionString(FluentConnectionInformation connectionInformation)
        {
            var connectionStringBuilder =
                new Orcl.OracleConnectionStringBuilder(connectionInformation.ConnectionString);
            connectionStringBuilder.Password = connectionInformation.DecryptString(connectionStringBuilder.Password);
            return connectionStringBuilder.ConnectionString;
        }

        protected override string ParameterPrefix => ":";

        protected override void DecorateCommand(IDbCommand command) => ((Orcl.OracleCommand) command).BindByName = true;

        protected override void SetProviderSpecificParameterType(DbParameter parameter, DbType parameterType)
        {
            ((Orcl.OracleParameter) parameter).OracleDbType = DbTypeToOracleDbType(parameterType);
        }

        private static Orcl.OracleDbType DbTypeToOracleDbType(DbType dbType)
        {

            switch (dbType)
            {
                case DbType.AnsiString:
                case DbType.String:
                    return Orcl.OracleDbType.Varchar2;
                case DbType.AnsiStringFixedLength:
                case DbType.StringFixedLength:
                    return Orcl.OracleDbType.Char;
                case DbType.Byte:
                case DbType.Int16:
                case DbType.SByte:
                case DbType.UInt16:
                case DbType.Int32:
                    return Orcl.OracleDbType.Int32;
                case DbType.Single:
                    return Orcl.OracleDbType.Single;
                case DbType.Double:
                    return Orcl.OracleDbType.Double;
                case DbType.Date:
                    return Orcl.OracleDbType.Date;
                case DbType.DateTime:
                    return Orcl.OracleDbType.TimeStamp;
                case DbType.Time:
                    return Orcl.OracleDbType.IntervalDS;
                case DbType.Binary:
                    return Orcl.OracleDbType.Blob;
                case DbType.Int64:
                case DbType.UInt64:
                case DbType.VarNumeric:
                case DbType.Decimal:
                case DbType.Currency:
                    return Orcl.OracleDbType.Decimal;
                case DbType.Guid:
                    return Orcl.OracleDbType.Raw;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

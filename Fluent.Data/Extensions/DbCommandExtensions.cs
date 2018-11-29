using System.Data;
using System.Data.Common;
using System.Linq;

namespace Fluent.Data.Extensions
{
    public static class DbCommandExtensions
    {
        /// <summary>
        /// Renders the given <paramref name="dbCommand"/> to a sql statement by substituting each parameter in the 
        /// <see cref="DbParameter"/>
        /// in the <see cref="DbCommand.CommandText"/> with the corresponding <see cref="DbParameter"/> in the 
        /// <see cref="DbCommand.Parameters"/> collection.
        /// </summary>
        /// <param name="dbCommand"></param>
        /// <returns></returns>
        public static string ToCommandString(this IDbCommand dbCommand)
        {
            if (dbCommand == null)
            {
                return null;
            }

            var quotedParameterTypes = new[]
            {
                DbType.AnsiString,
                DbType.Date,
                DbType.DateTime,
                DbType.Guid,
                DbType.String,
                DbType.AnsiStringFixedLength,
                DbType.StringFixedLength
            };

            var query = dbCommand.CommandText;
            var parameters = dbCommand
                .Parameters
                .Cast<DbParameter>();

            foreach (var parameter in parameters.OrderByDescending(p => p.ParameterName.Length))
            {
                var value = parameter.Value?.ToString() ?? "NULL";

                if (quotedParameterTypes.Contains(parameter.DbType))
                {
                    value = "'" + value + "'";
                }

                query = query.Replace(parameter.ParameterName, value);
            }

            return query;
        }
    }
}

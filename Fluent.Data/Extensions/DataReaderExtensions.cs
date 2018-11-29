using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace Fluent.Data.Extensions
{
    public static class DataReaderExtensions
    {
        public static T GetFieldValue<T>(this IDataReader reader, string columnName)
        {
            return (T)reader.GetValue(reader.GetOrdinal(columnName));
        }

        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            using (var dataReader = reader)
            {
                while (dataReader.Read())
                {
                    yield return dataReader;
                }
            }
        }

        //Randy - Messing around with Expressions.  This will allow for refactoring ease since
        //strings don't have to be passed.  Not sure about performance.
        public static T GetValue<T, TEntity>(this IDataRecord dataRecord, Expression<Func<TEntity, object>> expression)
        {
            if (!(expression.Body is MemberExpression member))
            {
                // The property access might be getting converted to object to match the func
                // If so, get the operand and see if that's a member expression
                member = (expression.Body as UnaryExpression)?.Operand as MemberExpression;
            }

            if (member == null)
            {
                throw new ArgumentException("Action must be a member expression.");
            }

            return dataRecord.GetValue<T>(member.Member.Name);
        }

        public static T GetValue<T>(this IDataRecord dataRecord, string columnName)
        {
            object value;

            try
            {
                value = dataRecord.GetValue(dataRecord.GetOrdinal(columnName));
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new IndexOutOfRangeException($"Column: {columnName}", ex);
            }

            if (value == DBNull.Value)
            {
                return default(T);
            }

            try
            {
                return (T) value;
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidCastException($"Column: {columnName} - Type: {value.GetType().Name}", ex);
            }
        }
    }
}
